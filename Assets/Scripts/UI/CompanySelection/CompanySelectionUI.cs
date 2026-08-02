using System.Collections.Generic;
using DG.Tweening;
using MMFramework.MMUI;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game.Offer;
using Pinvestor.GameConfigSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityWeld.Binding;

namespace Pinvestor.UI
{
    [Binding]
    public class CompanySelectionUI : VMBase
    {
        [SerializeField] private ButtonWidget _hideButton = null;
        [SerializeField] private ButtonWidget _showButton = null;

        [SerializeField] private Image _bgImage = null;

        [Tooltip("Carrier with the HorizontalLayoutGroup that arranges the offer cards.")]
        [SerializeField] private RectTransform _cardParentRect = null;

        [SerializeField] private float _hideUIDuration = 0.1f;
        [SerializeField] private Ease _hideUIEase = Ease.OutBack;

        [SerializeField] private float _showUIDuration = 0.1f;
        [SerializeField] private Ease _showUIEase = Ease.OutBounce;

        [Header("Card Prefab")]
        [SerializeField] private Widget_CompanyCard _cardPrefab = null;

        private OfferPhaseContext _context;

        private EventBinding<ShowCompanyOfferPanelEvent> _showOfferBinding;
        private EventBinding<HideCompanyOfferPanelEvent> _hideOfferBinding;

        private readonly List<Widget_CompanyCard> _cardWidgets = new List<Widget_CompanyCard>();

        protected override void AwakeCustomActions()
        {
            _showOfferBinding = new EventBinding<ShowCompanyOfferPanelEvent>(OnShowOfferEvent);
            _hideOfferBinding = new EventBinding<HideCompanyOfferPanelEvent>(OnHideOfferEvent);

            EventBus<ShowCompanyOfferPanelEvent>.Register(_showOfferBinding);
            EventBus<HideCompanyOfferPanelEvent>.Register(_hideOfferBinding);

            base.AwakeCustomActions();
        }

        protected override void OnDestroyCustomActions()
        {
            EventBus<ShowCompanyOfferPanelEvent>.Deregister(_showOfferBinding);
            EventBus<HideCompanyOfferPanelEvent>.Deregister(_hideOfferBinding);

            ClearCards();

            base.OnDestroyCustomActions();
        }

        protected override void DeactivatedCustomActions()
        {
            ClearCards();

            base.DeactivatedCustomActions();
        }

        private void OnShowOfferEvent(ShowCompanyOfferPanelEvent e)
        {
            _context = e.Context;

            CreateOfferCards(_context.OfferedCompanies);

            _bgImage.enabled = true;

            TryActivate();

            ShowCards();

            _hideButton.TryActivate();
            _showButton.TryDeactivate();
        }

        private void OnHideOfferEvent(HideCompanyOfferPanelEvent e)
        {
            _context = null;

            HideCards();

            _bgImage.enabled = false;

            _hideButton.TryDeactivate();
            _showButton.TryDeactivate();

            TryDeactivate();
        }

        private void CreateOfferCards(IReadOnlyList<CompanyConfigModel> companies)
        {
            ClearCards();

            if (_cardPrefab == null)
            {
                Debug.LogError("[CompanySelectionUI] _cardPrefab is not assigned. Cannot create offer cards.");
                return;
            }

            for (int i = 0; i < companies.Count; i++)
            {
                var company = companies[i];

                // Layout is the carrier's HorizontalLayoutGroup job — the panel only
                // parents the card and lets the group place it.
                var cardWidget = Instantiate(_cardPrefab, _cardParentRect);
                cardWidget.gameObject.name = $"OfferCard_{i}_{company.CompanyId}";

                cardWidget.transform.localScale = Vector3.zero;

                cardWidget.PopulateFromConfig(company);
                cardWidget.OnClicked += OnCardClicked;

                cardWidget.TryActivate();

                _cardWidgets.Add(cardWidget);
            }
        }

        private void OnCardClicked(Widget_CompanyCard cardWidget)
        {
            if (_context == null || cardWidget.Model == null)
                return;

            _context.ConfirmSelection(cardWidget.Model);
        }

        private void ShowCards()
        {
            foreach (var widget in _cardWidgets)
                widget.PlayShow(_showUIDuration, _showUIEase);
        }

        private void HideCards()
        {
            foreach (var widget in _cardWidgets)
                widget.PlayHide(_hideUIDuration, _hideUIEase);
        }

        private void ClearCards()
        {
            foreach (var widget in _cardWidgets)
            {
                if (widget == null)
                    continue;

                widget.OnClicked -= OnCardClicked;

                Destroy(widget.gameObject);
            }

            _cardWidgets.Clear();
        }

        [Binding]
        public void OnHideButtonClick()
        {
            HideCards();

            _bgImage.enabled = false;

            _hideButton.TryDeactivate();
            _showButton.TryActivate();

            EventBus<OnViewBoardModeEnterEvent>
                .Raise(new OnViewBoardModeEnterEvent());
        }

        [Binding]
        public void OnShowButtonClick()
        {
            ShowCards();

            _bgImage.enabled = true;

            _hideButton.TryActivate();
            _showButton.TryDeactivate();

            EventBus<OnViewBoardModeExitEvent>
                .Raise(new OnViewBoardModeExitEvent());
        }
    }
}
