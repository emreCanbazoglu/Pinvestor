using UnityEngine;

namespace Pinvestor.CardSystem
{
    [System.Serializable]
    public class DeckPileData
    {
        [field: SerializeField] public EDeckPile DeckPileType { get; private set; }
        [field: SerializeField] public int SlotCapacity { get; private set; }
        
        public DeckPileData()
        {
        }
        
        public DeckPileData(
            EDeckPile deckPileType,
            int slotCapacity)
        {
            DeckPileType = deckPileType;
            SlotCapacity = slotCapacity;
        }
    }
}