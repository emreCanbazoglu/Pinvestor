// Portfolio/cashout section of the offer phase (spec-006 T019–T021).
// Shows all placed companies with health + cashout value while the offer panel is
// open, and lets the player cash out via CashoutService on the active Turn.

using System.Collections.Generic;
using System.Linq;
using MMFramework.MMUI;
using Pinvestor.BoardSystem;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game;
using Pinvestor.Game.Offer;
using UnityEngine;
using UnityWeld.Binding;

namespace Pinvestor.UI.Offer
{
    /// <summary>
    /// Portfolio section shown during the Offer Phase. Lists every company on the
    /// board with name, health, and cashout payout; each row's cashout button calls
    /// <see cref="Pinvestor.Game.Economy.CashoutService.TryCashout"/> on the active turn.
    /// Buttons are disabled for companies pending collapse (T021).
    /// </summary>
    [Binding]
    public class CompanyOfferPanel : VMBase
    {
        [SerializeField] private RectTransform _rowParent = null;
        [SerializeField] private Widget_PortfolioRow _rowPrefab = null;
        [SerializeField] private GameObject _emptyStateLabel = null;

        private Turn _activeTurn;

        private EventBinding<ShowCompanyOfferPanelEvent> _showBinding;
        private EventBinding<HideCompanyOfferPanelEvent> _hideBinding;

        private readonly List<Widget_PortfolioRow> _rows = new List<Widget_PortfolioRow>();

        protected override void AwakeCustomActions()
        {
            _showBinding = new EventBinding<ShowCompanyOfferPanelEvent>(OnShowOfferEvent);
            _hideBinding = new EventBinding<HideCompanyOfferPanelEvent>(OnHideOfferEvent);

            EventBus<ShowCompanyOfferPanelEvent>.Register(_showBinding);
            EventBus<HideCompanyOfferPanelEvent>.Register(_hideBinding);

            base.AwakeCustomActions();
        }

        protected override void OnDestroyCustomActions()
        {
            EventBus<ShowCompanyOfferPanelEvent>.Deregister(_showBinding);
            EventBus<HideCompanyOfferPanelEvent>.Deregister(_hideBinding);

            ClearRows();

            base.OnDestroyCustomActions();
        }

        protected override void DeactivatedCustomActions()
        {
            ClearRows();
            _activeTurn = null;

            base.DeactivatedCustomActions();
        }

        private void OnShowOfferEvent(ShowCompanyOfferPanelEvent e)
        {
            _activeTurn = e.Turn;

            if (_activeTurn == null)
            {
                Debug.LogWarning("[CompanyOfferPanel] Show event carried no Turn. Portfolio section unavailable.");
                return;
            }

            TryActivate();

            RefreshPortfolio();
        }

        private void OnHideOfferEvent(HideCompanyOfferPanelEvent e)
        {
            TryDeactivate();
        }

        private void RefreshPortfolio()
        {
            ClearRows();

            if (_activeTurn?.Board == null || _rowPrefab == null || _rowParent == null)
                return;

            List<BoardItemWrapper_Company> holdings = CollectHoldings();

            if (_emptyStateLabel != null)
                _emptyStateLabel.SetActive(holdings.Count == 0);

            foreach (BoardItemWrapper_Company companyWrapper in holdings)
            {
                Widget_PortfolioRow row = Instantiate(_rowPrefab, _rowParent);
                row.gameObject.name = $"PortfolioRow_{companyWrapper.Company?.CompanyId?.CompanyId}";
                row.gameObject.SetActive(true);

                BoardItemWrapper_Company captured = companyWrapper;
                row.Populate(captured, () => OnCashoutClicked(captured));

                _rows.Add(row);
            }
        }

        private List<BoardItemWrapper_Company> CollectHoldings()
        {
            var holdings = new List<BoardItemWrapper_Company>();

            foreach (BoardItem_Company companyItem in _activeTurn.Board.BoardItems.OfType<BoardItem_Company>())
            {
                if (!(companyItem.Wrapper is BoardItemWrapper_Company wrapper))
                    continue;

                // Skip companies already mid-destruction (collapsed or cashed out this frame).
                if (companyItem.TryGetPropertySpec(out BoardItemPropertySpec_Destroyable destroyableSpec)
                    && destroyableSpec.IsDestroying)
                    continue;

                holdings.Add(wrapper);
            }

            return holdings;
        }

        private void OnCashoutClicked(BoardItemWrapper_Company companyWrapper)
        {
            if (_activeTurn?.CashoutService == null)
                return;

            if (_activeTurn.CashoutService.TryCashout(companyWrapper))
                RefreshPortfolio();
        }

        private void ClearRows()
        {
            foreach (Widget_PortfolioRow row in _rows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }

            _rows.Clear();

            if (_emptyStateLabel != null)
                _emptyStateLabel.SetActive(false);
        }
    }
}
