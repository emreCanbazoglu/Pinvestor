using System.Collections.Generic;
using UnityEngine;

namespace Pinvestor.CardSystem.Authoring
{
    public class CompanySelectionPileWrapper : MonoBehaviour
    {
        public CompanySelectionPile Pile { get; private set; }

        public void WrapPile(
            CompanySelectionPile pile)
        {
            Pile = pile;

            Pile.OnSlotsReset += OnSlotsReset;
            Pile.OnSlotsFilled += OnSlotsFilled;
        }

        private void OnDestroy()
        {
            Pile.OnSlotsReset -= OnSlotsReset;
            Pile.OnSlotsFilled -= OnSlotsFilled;
        }

        private void OnSlotsReset()
        {
            // Implement me
        }

        private void OnSlotsFilled()
        {
            
        }

        private IEnumerator<float> CreatePileCards()
        {
            yield break;
            
        }
    }
}
