using AttributeSystem.Components;
using UnityEngine;

namespace AttributeSystem.Authoring
{
    [CreateAssetMenu(
        menuName = "Gameplay Ability System/Attribute System/Attribute Modifier Object Provider/Constant Level Provider", 
        fileName = "ConstantAttributeLevelProvider")]
    public class ConstantAttributeLevelProviderScriptableObject : AttributeModifierObjectProviderScriptableObject
    {
        [SerializeField] private int _level = 1;
        
        public override object GetObject(
            AttributeSystemComponent asc,
            AttributeScriptableObject attribute)
        {
            return _level;
        }
    }
}