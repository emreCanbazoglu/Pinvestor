using UnityEngine;

namespace AttributeSystem.Components
{
    public class AttributeSystemComponentReference : MonoBehaviour,
        IComponentProvider<AttributeSystemComponent>
    {
        [field: SerializeField] public AttributeSystemComponent AttributeSystemComponent { get; private set; } = null;

        public AttributeSystemComponent GetComponent()
        {
            return AttributeSystemComponent;
        }
    }
}
