namespace Pinvestor.CardSystem
{
    public class Slot
    {
        public int Index { get; private set; }
        public DeckPile DeckPile { get; private set; }
        public CardBase Card { get; private set; }
        
        public bool IsEmpty => Card == null;
        
        public const int EMPTY_SLOT_INDEX = -1;
        
        public Slot(
            int index,
            DeckPile deckPile)
        {
            Index = index;
            DeckPile = deckPile;
        }
        
        public void SetCard(
            CardBase card,
            object additionalData = null)
        {
            Card = card;
            Card.SetCardAddedToSlot(Index, additionalData);
        }

        public bool TryRemoveCard()
        {
            if (Card == null)
                return false;

            Card.SetCardRemovedFromSlot();
            Card = null;

            return true;
        }
    }
}