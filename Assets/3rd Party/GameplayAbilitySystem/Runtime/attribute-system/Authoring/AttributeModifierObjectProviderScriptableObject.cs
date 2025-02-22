using AttributeSystem.Components;
using UnityEngine;

namespace AttributeSystem.Authoring
{
    public abstract class AttributeModifierObjectProviderScriptableObject : ScriptableObject
    {
        public abstract object GetObject(
            AttributeSystemComponent asc, 
            AttributeScriptableObject attribute);

    }
}