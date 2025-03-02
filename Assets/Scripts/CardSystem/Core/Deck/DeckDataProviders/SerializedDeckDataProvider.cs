using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pinvestor.CardSystem
{
    public class SerializedDeckDataProvider : MonoBehaviour,
        IDeckDataProvider
    {
        [System.Serializable]
        public class SerializedCardData
        {
            [field: SerializeField] public CardDataScriptableObject CardDataScriptableObject { get; private set; }
            [field: SerializeField] public EDeckPile DeckPile { get; private set; }
            [field: SerializeField] public int SlotIndex { get; private set; }            
        }

        [SerializeField] private SerializedCardData[] _cardDatas
            = Array.Empty<SerializedCardData>();
        
        [SerializeField] private DeckPileData[] _pileDatas
            = Array.Empty<DeckPileData>();
        
        
        public UniTask WaitUntilInitialized()
        {
            return UniTask.CompletedTask;
        }

        public IReadOnlyList<CardData> GetCardData()
        {
            var cardData = new CardData[_cardDatas.Length];
            
            for (var i = 0; i < _cardDatas.Length; i++)
            {
                var serializedCardData = _cardDatas[i];
                
                cardData[i] = new CardData(
                    serializedCardData.CardDataScriptableObject.UniqueId,
                    serializedCardData.DeckPile,
                    serializedCardData.SlotIndex);
            }
            
            return cardData;
        }

        public IReadOnlyList<DeckPileData> GetPileData()
        {
            return _pileDatas;
        }
    }
}
