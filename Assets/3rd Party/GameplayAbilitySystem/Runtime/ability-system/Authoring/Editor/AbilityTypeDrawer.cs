using UnityEditor;
using UnityEngine;

namespace AbilitySystem.Authoring
{
    [CustomPropertyDrawer(typeof(AbilityType))]
    public class AbilityTypeDrawer : PropertyDrawer
    {
        private static AbilityTypeContainer _container;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                
                System.Type declaringType = property.serializedObject.targetObject.GetType();

                if (declaringType == typeof(AbilityTypeContainer))
                {
                    EditorGUI.PropertyField(position, property, label);
                    return;
                }
                
                EditorGUI.BeginChangeCheck();
                AbilityType selectedType = (AbilityType)property.objectReferenceValue;

                if(_container == null)
                    _container
                        = AssetDatabase.LoadAssetAtPath<AbilityTypeContainer>("Assets/ScriptableObjects/Abilities/AbilityTypeContainer.asset");
            
                if(_container == null)
                    return;
            
                AbilityType[] abilityTypes = _container.AbilityTypes;

                // Create a dropdown list to select the gameplay tag
                int selectedTagIndex = -1;
                string[] tagNames = new string[abilityTypes.Length];
                for (int i = 0; i < abilityTypes.Length; i++)
                {
                    tagNames[i] = abilityTypes[i].name;
                    if (abilityTypes[i] == selectedType)
                    {
                        selectedTagIndex = i;
                    }
                }

                selectedTagIndex = EditorGUI.Popup(position, label.text, selectedTagIndex, tagNames);

                if (selectedTagIndex >= 0 && selectedTagIndex < abilityTypes.Length)
                {
                    selectedType = abilityTypes[selectedTagIndex];
                }

                if (EditorGUI.EndChangeCheck())
                {
                    property.objectReferenceValue = selectedType;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Generic)
            {
                // Array or List of GameplayTag
                EditorGUI.PropertyField(position, property, label);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}