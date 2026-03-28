using System;
using MMFramework.MMUI;
using Pinvestor.Game.Offer;
using Pinvestor.GameConfigSystem;
using UnityEngine;
using UnityWeld.Binding;

namespace Pinvestor.UI.Offer
{
    /// <summary>
    /// View model for the Company Offer Panel.
    /// Receives the OfferPhaseContext via ShowCompanyOfferPanelEvent,
    /// manages single-card selection across 3 CompanyOfferCardWidgets,
    /// and exposes ConfirmSelection() which resolves the context's SelectionTask.
    ///
    /// Confirm button is disabled until exactly one card is selected.
    /// </summary>
    [Binding]
    public class CompanyOfferPanelVM : VMBase
    {
        [SerializeField] private CompanyOfferCardWidget _cardWidget0 = null;
        [SerializeField] private CompanyOfferCardWidget _cardWidget1 = null;
        [SerializeField] private CompanyOfferCardWidget _cardWidget2 = null;
        [SerializeField] private ButtonWidget _confirmButton = null;

        private OfferPhaseContext _context;
        private int _selectedIndex = -1;

        private EventBinding<ShowCompanyOfferPanelEvent> _showEventBinding;
        private EventBinding<HideCompanyOfferPanelEvent> _hideEventBinding;

        // --- Confirm button enabled state ---

        private bool _isConfirmEnabled;
        [Binding]
        public bool IsConfirmEnabled
        {
            get => _isConfirmEnabled;
            private set
            {
                _isConfirmEnabled = value;
                OnPropertyChanged(nameof(IsConfirmEnabled));
            }
        }

        // --- VMBase lifecycle ---

        protected override void AwakeCustomActions()
        {
            _showEventBinding = new EventBinding<ShowCompanyOfferPanelEvent>(OnShowEvent);
            _hideEventBinding = new EventBinding<HideCompanyOfferPanelEvent>(OnHideEvent);

            EventBus<ShowCompanyOfferPanelEvent>.Register(_showEventBinding);
            EventBus<HideCompanyOfferPanelEvent>.Register(_hideEventBinding);

            base.AwakeCustomActions();
        }

        protected override void OnDestroyCustomActions()
        {
            EventBus<ShowCompanyOfferPanelEvent>.Deregister(_showEventBinding);
            EventBus<HideCompanyOfferPanelEvent>.Deregister(_hideEventBinding);

            base.OnDestroyCustomActions();
        }

        // --- Event handlers ---

        private void OnShowEvent(ShowCompanyOfferPanelEvent e)
        {
            _context = e.Context;
            _selectedIndex = -1;
            IsConfirmEnabled = false;

            PopulateCards(_context);

            TryActivate();
        }

        private void OnHideEvent(HideCompanyOfferPanelEvent e)
        {
            _context = null;
            _selectedIndex = -1;
            IsConfirmEnabled = false;

            TryDeactivate();
        }

        // --- Card population ---

        private void PopulateCards(OfferPhaseContext context)
        {
            CompanyOfferCardWidget[] widgets = { _cardWidget0, _cardWidget1, _cardWidget2 };

            for (int i = 0; i < widgets.Length; i++)
            {
                if (widgets[i] == null)
                    continue;

                if (i < context.OfferedCompanies.Count)
                {
                    widgets[i].Populate(context.OfferedCompanies[i]);
                    widgets[i].SetSelected(false);
                    widgets[i].TryActivate();
                }
                else
                {
                    // Pool depleted — fewer than 3 offers available.
                    widgets[i].TryDeactivate();
                }
            }
        }

        // --- Card selection (called by card click handlers in the view) ---

        /// <summary>
        /// Called when the player clicks card at index 0.
        /// </summary>
        [Binding]
        public void OnCardClicked0() => SelectCard(0);

        /// <summary>
        /// Called when the player clicks card at index 1.
        /// </summary>
        [Binding]
        public void OnCardClicked1() => SelectCard(1);

        /// <summary>
        /// Called when the player clicks card at index 2.
        /// </summary>
        [Binding]
        public void OnCardClicked2() => SelectCard(2);

        private void SelectCard(int index)
        {
            if (_context == null)
                return;

            if (index >= _context.OfferedCompanies.Count)
                return;

            _selectedIndex = index;

            CompanyOfferCardWidget[] widgets = { _cardWidget0, _cardWidget1, _cardWidget2 };
            for (int i = 0; i < widgets.Length; i++)
            {
                if (widgets[i] != null)
                    widgets[i].SetSelected(i == _selectedIndex);
            }

            IsConfirmEnabled = true;
        }

        // --- Confirm ---

        /// <summary>
        /// Resolves the OfferPhaseContext's SelectionTask with the selected company.
        /// Disabled in the view until exactly one card is selected (IsConfirmEnabled).
        /// </summary>
        [Binding]
        public void ConfirmSelection()
        {
            if (_context == null)
            {
                Debug.LogWarning("[CompanyOfferPanelVM] ConfirmSelection called but context is null.");
                return;
            }

            if (_selectedIndex < 0 || _selectedIndex >= _context.OfferedCompanies.Count)
            {
                Debug.LogWarning("[CompanyOfferPanelVM] ConfirmSelection called but no valid card is selected.");
                return;
            }

            CompanyConfigModel selected = _context.OfferedCompanies[_selectedIndex];
            _context.ConfirmSelection(selected);
        }
    }
}
