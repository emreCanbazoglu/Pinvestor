using System;
using System.Linq;
using TypeReferences;
using UnityEngine;

namespace Pinvestor.CardSystem
{
    public class DeckPileFactory : Singleton<DeckPileFactory>
    {
        [System.Serializable]
        public class PileMap
        {
            public EDeckPile DeckPileType;
            
            [ClassImplements(typeof(IDeckPile))]
            public ClassTypeReference DeckPileTypeReference = typeof(DeckPile);
        }

        [SerializeField] private PileMap[] _pileMappings = Array.Empty<PileMap>();
        
        public DeckPile CreateDeckPile(
            Deck deck,
            DeckPileData deckPileData)
        {
            if(!TryGetPileMap(
                   deckPileData.DeckPileType, 
                   out var pileMap))
            {
                return new DeckPile(
                    deck,
                    deckPileData);
            }
            
            var deckPileType = pileMap.DeckPileTypeReference.Type;
            
            return (DeckPile)Activator.CreateInstance(
                deckPileType, deck, deckPileData);
        }
        
        private bool TryGetPileMap(
            EDeckPile deckPileType,
            out PileMap pileMap)
        {
            pileMap = _pileMappings.FirstOrDefault(
                val => val.DeckPileType == deckPileType);
            
            return pileMap != null;
        }
    }
}