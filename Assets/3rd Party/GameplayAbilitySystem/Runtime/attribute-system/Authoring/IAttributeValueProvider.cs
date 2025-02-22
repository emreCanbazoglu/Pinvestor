using AttributeSystem.Components;

namespace AttributeSystem.Authoring
{
    public interface IAttributeValueProvider
    {
        bool TryGetAttributeValue(
            AttributeScriptableObject attribute,
            out AttributeValue value,
            object modifierObject = null);
    }
    
    public class CompareAttributeRequest
    {
        public IAttributeValueProvider OtherProvider { get; set; }
        public AttributeScriptableObject Attribute { get; set; }
        public object SelfModifierObject { get; set; }
        public object OtherModifierObject { get; set; }
        
        public CompareAttributeRequest(
            IAttributeValueProvider otherProvider,
            AttributeScriptableObject attribute,
            object selfModifierObject = null,
            object otherModifierObject = null)
        {
            OtherProvider = otherProvider;
            Attribute = attribute;
            SelfModifierObject = selfModifierObject;
            OtherModifierObject = otherModifierObject;
        }
    }
    
    public class CompareAttributeResponse
    {
        public float SelfValue { get; set; }
        public float OtherValue { get; set; }
        
        public float PercentageDifference => (OtherValue - SelfValue) / SelfValue * 100;
            
    }
    
    public static class IAttributeValueProviderExtensions
    {
        public static bool TryCompareAttribute(
            this IAttributeValueProvider provider,
            CompareAttributeRequest request,
            out CompareAttributeResponse response)
        {
            response = default;
            
            if (!provider.TryGetAttributeValue(
                    request.Attribute,
                    out var selfAttributeValue,
                    request.SelfModifierObject))
                return false;

            if (!request.OtherProvider.TryGetAttributeValue(
                    request.Attribute,
                    out var otherAttributeValue,
                    request.OtherModifierObject))
                return false;

            response = new CompareAttributeResponse
            {
                SelfValue = selfAttributeValue.CurrentValue,
                OtherValue = otherAttributeValue.CurrentValue
            };
            
            return true;
        }
    }
}