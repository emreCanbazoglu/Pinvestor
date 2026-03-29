using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pinvestor.CardSystem
{
    [CreateAssetMenu(
        menuName = "Pinvestor/Deck System/Card Container",
        fileName = "CardContainer")]
    public class CardContainerScriptableObject : ScriptableObject
    {
        [SerializeField] private CardDataScriptableObject[] _cards
            = Array.Empty<CardDataScriptableObject>();
        
        public bool TryGetCardData(
            string referenceCardId, 
            out CardDataScriptableObject cardData)
        {
            cardData = _cards.FirstOrDefault(
                val => val.UniqueId == referenceCardId);

            return cardData != null;
        }
        
        public List<TCardDataScriptableObject> GetCardDataOfType<TCardDataScriptableObject>()
            where TCardDataScriptableObject : CardDataScriptableObject
        {
            return _cards.OfType<TCardDataScriptableObject>().ToList();
        }
    }
}