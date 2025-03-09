using System;
using System.Collections.Generic;
using AttributeSystem.Authoring;
using Cysharp.Threading.Tasks;
using ResetableSystem;
using UnityEngine;

namespace AttributeSystem.Components
{
    /// <summary>
    /// Manages the attributes for a game character
    /// </summary>
    public class AttributeSystemComponent : MonoBehaviour,
        IAttributeValueProvider,
        IComponentProvider<AttributeSystemComponent>,
        IResetable
    {
        [SerializeField] private AbstractAttributeEventHandler[] _attributeSystemEvents
            = Array.Empty<AbstractAttributeEventHandler>();

        /// <summary>
        /// Attribute sets assigned to the game character
        /// </summary>
        [SerializeField] private AttributeSetScriptableObject _attributeSet = null;
        
        private List<AttributeDefinition> _attributeDefinitions 
            = new List<AttributeDefinition>();

        [SerializeField] private List<AttributeValue> _attributeValues 
            = new List<AttributeValue>();
        
        [SerializeField] private bool _initializeOnAwake = true;

        private bool _attributeDictStale;
        public Dictionary<AttributeScriptableObject, int> AttributeIndexCache { get; private set; } 
            = new Dictionary<AttributeScriptableObject, int>();
        
        private bool _isInitialized = false;

        /// <summary>
        /// Marks attribute cache dirty, so it can be recreated next time it is required
        /// </summary>
        public void MarkAttributesDirty()
        {
            _attributeDictStale = true;
        }
        
        /// <summary>
        /// Gets the value of an attribute.  Note that the returned value is a copy of the struct, so modifying it
        /// does not modify the original attribute
        /// </summary>
        /// <param name="attribute">Attribute to get value for</param>
        /// <param name="modifierObject">Custom data to modify attribute value (previously used as level)</param>
        /// <param name="value">Returned attribute</param>
        /// <returns>True if attribute was found, false otherwise.</returns>
        public bool TryGetAttributeValue(AttributeScriptableObject attribute, out AttributeValue value, object modifierObject = null)
        {
            // If dictionary is stale, rebuild it
            var attributeCache = GetAttributeCache();

            // We use a cache to store the index of the attribute in the list, so we don't
            // have to iterate through it every time
            if (attributeCache.TryGetValue(attribute, out var index))
            {
                if (modifierObject == null)
                {
                    value = _attributeValues[index];

                    return true;
                }
                else
                {
                    _attributeSet.TryGetAttributeValue(attribute, out value, modifierObject);
                    value.Modifier = _attributeValues[index].Modifier;
                    
                    value
                        = value.Attribute
                            .CalculateCurrentAttributeValue(
                                this,
                                value, 
                                _attributeValues);
                    
                    return true;
                }
            }

            // No matching attribute found
            value = new AttributeValue();
            return false;
        }
        
        

        public void SetAttributeBaseValue(
            AttributeScriptableObject attribute, float value)
        {
            // If dictionary is stale, rebuild it
            var attributeCache = GetAttributeCache();
            if (!attributeCache.TryGetValue(attribute, out var index)) return;
            var attributeValue = _attributeValues[index];
            attributeValue.BaseValue = value;
            _attributeValues[index] = attributeValue;
            
            UpdateAttributeCurrentValue(attribute);
        }

        public bool ModifyBaseValue(
            AttributeScriptableObject attribute,
            AttributeModifier modifier, 
            out AttributeValue value)
        {
            // If dictionary is stale, rebuild it
            var attributeCache = GetAttributeCache();
            if (!attributeCache.TryGetValue(attribute, out var index))
            {
                value = new AttributeValue();
                return false;
            }
            
            var attributeValue = _attributeValues[index];
            
            var newBaseValue = (attributeValue.BaseValue + modifier.Add) * (modifier.Multiply + 1);

            SetAttributeBaseValue(attribute, newBaseValue);

            value = _attributeValues[index];
            
            return true;
        }

        /// <summary>
        /// Sets value of an attribute.  Note that the out value is a copy of the struct, so modifying it
        /// does not modify the original attribute
        /// </summary>
        /// <param name="attribute">Attribute to set</param>
        /// <param name="modifierType">How to modify the attribute</param>
        /// <param name="value">Copy of newly modified attribute</param>
        /// <returns>True, if attribute was found.</returns>
        public bool UpdateAttributeModifiers(AttributeScriptableObject attribute, AttributeModifier modifier, out AttributeValue value)
        {
            // If dictionary is stale, rebuild it
            var attributeCache = GetAttributeCache();

            // We use a cache to store the index of the attribute in the list, so we don't
            // have to iterate through it every time
            if (attributeCache.TryGetValue(attribute, out var index))
            {
                // Get a copy of the attribute value struct
                value = _attributeValues[index];
                value.Modifier = value.Modifier.Combine(modifier);

                // Structs are copied by value, so the modified attribute needs to be reassigned to the array
                _attributeValues[index] = value;
                return true;
            }

            // No matching attribute found
            value = new AttributeValue();
            return false;
        }

        /// <summary>
        /// Add attributes to this attribute system.  Duplicates are ignored.
        /// </summary>
        /// <param name="attributeDefinitions">Attributes to add</param>
        public void AddAttributes(params AttributeDefinition[] attributeDefinitions)
        {
            // If this attribute already exists, we don't need to add it.  For that, we need to make sure the cache is up to date.
            var attributeCache = GetAttributeCache();

            for (var i = 0; i < attributeDefinitions.Length; i++)
            {
                if (attributeCache.ContainsKey(attributeDefinitions[i].Attribute))
                    continue;

                _attributeDefinitions.Add(attributeDefinitions[i]);
                attributeCache.Add(attributeDefinitions[i].Attribute, _attributeDefinitions.Count - 1);
            }
        }

        /// <summary>
        /// Remove attributes from this attribute system.
        /// </summary>
        /// <param name="attributes">Attributes to remove</param>
        public void RemoveAttributes(params AttributeScriptableObject[] attributes)
        {
            foreach (var attribute in attributes)
            {
                if (!AttributeIndexCache.TryGetValue(attribute, out var index))
                    continue;

                _attributeDefinitions.RemoveAt(index);
                _attributeValues.RemoveAt(index);
                MarkAttributesDirty();
            }
            
            // Update attribute cache
            GetAttributeCache();
        }

        public void ResetAll()
        {
            for (var i = 0; i < _attributeValues.Count; i++)
            {
                var defaultAttribute = new AttributeValue();
                defaultAttribute.Attribute = _attributeValues[i].Attribute;
                _attributeValues[i] = defaultAttribute;
            }
        }

        public void ResetAttributeModifiers()
        {
            for (var i = 0; i < _attributeValues.Count; i++)
            {
                var attributeValue = _attributeValues[i];
                attributeValue.Modifier = default;
                _attributeValues[i] = attributeValue;
            }
        }

        private void InitializeAttributeDefinitions()
        {
            _attributeDefinitions = new List<AttributeDefinition>();
            
            for (var i = 0; i < _attributeSet.AttributeDefinitions.Length; i++)
                _attributeDefinitions.Add(_attributeSet.AttributeDefinitions[i]);
        }

        private void InitializeAttributeValues()
        {
            _attributeValues = new List<AttributeValue>();
            for (var i = 0; i < _attributeSet.AttributeDefinitions.Length; i++)
            {
                var attributeDefinition = _attributeSet.AttributeDefinitions[i];
                object modifierObject = attributeDefinition.ModifierObjectProvider != null
                    ? attributeDefinition.ModifierObjectProvider.GetObject(
                        this,
                        _attributeSet.AttributeDefinitions[i].Attribute)
                    : null;
                
                _attributeValues.Add(new AttributeValue()
                {
                    Attribute = attributeDefinition.Attribute,
                    BaseValue =  InitializeBaseValue(_attributeSet.AttributeDefinitions[i], modifierObject),
                    Modifier = new AttributeModifier()
                    {
                        Add = 0f,
                        Multiply = 0f,
                        Override = 0f
                    }
                });
                
                UpdateAttributeCurrentValue(i);
                _attributeDictStale = true;
            }
            
        }
        
        public async UniTask ReinitializeAttributeValuesAsync()
        {
            await WaitUntilInitializeAsync();
            
            for (var i = 0; i < _attributeValues.Count; i++)
            {
                var attributeDefinition = _attributeSet.AttributeDefinitions[i];
                object modifierObject = attributeDefinition.ModifierObjectProvider != null
                    ? attributeDefinition.ModifierObjectProvider.GetObject(
                        this,
                        _attributeSet.AttributeDefinitions[i].Attribute)
                    : null;
                
                _attributeValues[i] = new AttributeValue
                {
                    Attribute = attributeDefinition.Attribute,
                    BaseValue =  InitializeBaseValue(_attributeSet.AttributeDefinitions[i], modifierObject),
                    Modifier = _attributeValues[i].Modifier
                };
                
                UpdateAttributeCurrentValue(i);
                _attributeDictStale = true;
            }
        }

        private float InitializeBaseValue(
            AttributeDefinition attributeDefinition,
            object modifierObject)
        {
            float value = attributeDefinition.BaseValueModifier
                .CalculateBaseValue(this, modifierObject);
            
            value *= attributeDefinition.Multiplier;

            return value;
        }

        private List<AttributeValue> _prevAttributeValues = new List<AttributeValue>();
        public void UpdateAttributeCurrentValues()
        {
            _prevAttributeValues.Clear();
            
            for (var i = 0; i < _attributeValues.Count; i++)
            {
                var attributeValue = _attributeValues[i];
                _prevAttributeValues.Add(attributeValue);
                _attributeValues[i] 
                    = attributeValue.Attribute
                        .CalculateCurrentAttributeValue(
                            this,
                            attributeValue, 
                            _attributeValues);
            }
            
            for (var i = 0; i < _attributeSystemEvents.Length; i++)
            {
                _attributeSystemEvents[i].PreAttributeChange(
                    this, 
                    _prevAttributeValues, 
                    ref _attributeValues);
            }
        }

        public void UpdateAttributeCurrentValue(AttributeScriptableObject attribute)
        {
            List<AttributeValue> prevAttributeValues
                = new List<AttributeValue>();

            AttributeValue attributeValue = default;

            for (var i = 0; i < _attributeValues.Count; i++)
            {
                prevAttributeValues.Add(attributeValue);

                if (attribute != _attributeValues[i].Attribute)
                    continue;
                
                attributeValue = _attributeValues[i];
                _attributeValues[i] 
                    = attributeValue.Attribute
                        .CalculateCurrentAttributeValue(
                            this,
                            attributeValue, 
                            _attributeValues);
                
                break;
            }
            
            for (var i = 0; i < _attributeSystemEvents.Length; i++)
            {
                _attributeSystemEvents[i].PreAttributeChange(
                    this, 
                    _prevAttributeValues, 
                    ref _attributeValues);
            }
        }

        private void UpdateAttributeCurrentValue(int index)
        {
            if(index < 0 || index >= _attributeValues.Count)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range.  " +
                    $"AttributeValues has {_attributeValues.Count} elements.");
            
            AttributeValue attributeValue = _attributeValues[index];
            
            _attributeValues[index] 
                = attributeValue.Attribute
                    .CalculateCurrentAttributeValue(
                        this,
                        attributeValue, 
                        _attributeValues);
            
            for (var i = 0; i < _attributeSystemEvents.Length; i++)
            {
                _attributeSystemEvents[i].PreAttributeChange(
                    this, 
                    _prevAttributeValues, 
                    ref _attributeValues);
            }
        }

        private Dictionary<AttributeScriptableObject, int> GetAttributeCache()
        {
            if (_attributeDictStale)
            {
                AttributeIndexCache.Clear();
                for (var i = 0; i < _attributeValues.Count; i++)
                {
                    AttributeIndexCache.Add(_attributeValues[i].Attribute, i);
                }
                this._attributeDictStale = false;
            }
            return AttributeIndexCache;
        }

        private void Awake()
        {
            if (_initializeOnAwake)
                Initialize();
        }

        public void Initialize(
            AttributeSetScriptableObject attributeSet)
        {
            _attributeSet = attributeSet;
            Initialize();
        }

        public void Initialize()
        {
            InitializeAttributeDefinitions();
            InitializeAttributeValues();
            MarkAttributesDirty();
            GetAttributeCache();
            
            UpdateAttributeCurrentValues();

            _isInitialized = true;
        }

        public async UniTask WaitUntilInitializeAsync()
        {
            while (!_isInitialized)
                await UniTask.Yield();
        }

        private void LateUpdate()
        {

            UpdateAttributeCurrentValues();
        }

        public void ResetResetable()
        {
            _isInitialized = false;
            
            Initialize();
        }

        public AttributeSystemComponent GetComponent()
        {
            return this;
        }

        #region Attribute Value Comparision

        public struct AttributeValueDifference
        {
            public AttributeScriptableObject Attribute;
            public float PreviousValue;
            public float CurrentValue;
        }
        
        public struct AttributeDifferenceRequest_ValueBased
        {
            public AttributeValue PreviousValue;
            public int Level;
        }
        
        public struct AttributeDifferenceRequest_LevelBased
        {
            public AttributeScriptableObject Attribute;
            public int CurLevel;
            public int TargetLevel;
        }
        
        public List<AttributeValueDifference> CalculateAttributeDifferences(
            List<AttributeDifferenceRequest_ValueBased> requests)
        {
            List<AttributeValueDifference> differences = new List<AttributeValueDifference>();
            
            for (var i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                var attribute = request.PreviousValue.Attribute;
                var previousValue = request.PreviousValue;
                
                if(!_attributeSet.TryGetAttributeDefinition(
                       attribute, out var attributeDefinition))
                    continue;
                
                var newBaseValue 
                    = attributeDefinition.BaseValueModifier
                    .CalculateBaseValue(this, request.Level);
                newBaseValue *= attributeDefinition.Multiplier;
                
                var currentValue = new AttributeValue()
                {
                    Attribute = attribute,
                    BaseValue = newBaseValue,
                    Modifier = new AttributeModifier()
                    {
                        Add = previousValue.Modifier.Add,
                        Multiply = previousValue.Modifier.Multiply,
                        Override = previousValue.Modifier.Override
                    }
                };
                
                currentValue = attribute.CalculateCurrentAttributeValue(
                    this, currentValue, _attributeValues);
                
                differences.Add(new AttributeValueDifference()
                {
                    Attribute = attribute,
                    PreviousValue = previousValue.CurrentValue,
                    CurrentValue = currentValue.CurrentValue
                });
            }

            return differences;
        }
        
        public static List<AttributeValueDifference> CalculateAttributeDifferencesStatic(
            AttributeSetScriptableObject attributeSet,
            List<AttributeDifferenceRequest_LevelBased> requests)
        {
            List<AttributeValueDifference> differences = new List<AttributeValueDifference>();
            
            for (var i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                var attribute = request.Attribute;
                
                if(!attributeSet.TryGetAttributeDefinition(
                       attribute, out var attributeDefinition))
                    continue;

                var currentBaseValue
                    = attributeDefinition.BaseValueModifier
                        .CalculateBaseValue(attributeSet, request.CurLevel);
                currentBaseValue *= attributeDefinition.Multiplier;
                
                var targetBaseValue
                    = attributeDefinition.BaseValueModifier
                        .CalculateBaseValue(attributeSet, request.TargetLevel);
                targetBaseValue *= attributeDefinition.Multiplier;
                
                differences.Add(new AttributeValueDifference()
                {
                    Attribute = attribute,
                    PreviousValue = currentBaseValue,
                    CurrentValue = targetBaseValue
                });
            }

            return differences;
        }

        #endregion 
    }
}
