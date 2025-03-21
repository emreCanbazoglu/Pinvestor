using Pinvestor.AbilitySystem.Abilities;

namespace Pinvestor.CardSystem
{
    public abstract class CardBase
    {
        public CardPlayer Owner { get; private set; }
        public CardData CardData { get; private set; }
        public abstract CardDataScriptableObject CardDataScriptableObject { get; }
        
        private PlayCardAbilitySpec _playCardAbilitySpec;
        
        protected CardBase(
            CardPlayer owner,
            CardData cardData)
        {
            Owner = owner;
            CardData = cardData;
        }
        
        protected void SetPlayCardAbilitySpec(
            PlayCardAbilitySpec playCardAbilitySpec)
        {
            _playCardAbilitySpec = playCardAbilitySpec;
        }
        
        public bool CanPlayCard(
            CardPlayer target = null)
        {
            if(target == null)
                target = Owner;
            
            SetTargetCardPlayer(target);
            
            return _playCardAbilitySpec.CanActivateAbility();
        }
        
        private void SetTargetCardPlayer(
            CardPlayer target)
        {
            _playCardAbilitySpec.TargetCardPlayer = target;
        }
        
        public void SetCardRemovedFromSlot()
        {
            SetSlotIndex(Slot.EMPTY_SLOT_INDEX);
            
            SetCardRemovedFromSlotCore();
        }
        
        protected virtual void SetCardRemovedFromSlotCore()
        {
            
        }
        
        public void SetCardAddedToSlot(
            int slotIndex, 
            object additionalData = null)
        {
            SetSlotIndex(slotIndex);
            
            SetCardAddedToSlotCore();
        }
        
        protected virtual void SetCardAddedToSlotCore(
            object additionalData = null)
        {
            
        }
        
        private void SetSlotIndex(int slotIndex)
        {
            CardData.SetSlotIndex(slotIndex);
        }
        
        public void SetDeckPile (
            EDeckPile deckPile)
        {
            CardData.SetDeckPile(deckPile);
        }
    }
    
    public abstract class CardBase<TCardDataScriptableObject> : CardBase
        where TCardDataScriptableObject : CardDataScriptableObject
    {
        public TCardDataScriptableObject CastedCardDataSo { get; private set; }
        
        public sealed override CardDataScriptableObject CardDataScriptableObject => CastedCardDataSo;
        
        protected CardBase(
            CardPlayer owner,
            CardData cardData,
            TCardDataScriptableObject cardDataSo) : base(owner, cardData)
        {
            CastedCardDataSo = cardDataSo;
            
            if(CastedCardDataSo.CardAbilityScriptableObject != null)
                SetPlayCardAbilitySpec(
                    CastedCardDataSo.CardAbilityScriptableObject
                        .CreateSpec(
                        owner.AbilitySystemCharacter) as PlayCardAbilitySpec);
        }
    }
}