using System.Collections.Generic;
using DG.Tweening;
using MMFramework.MMUI;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game.Offer;
using Pinvestor.GameConfigSystem;
using UnityEngine;
using UnityEngine.EventSystems;
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

        [SerializeField] private RectTransform _cardParentRect = null;
        [SerializeField] private float _yOffset = 120f;

        [SerializeField] private float _cardTweenScale = 1.1f;
        [SerializeField] private float _cardTweenDuration = 0.5f;
        [SerializeField] private Ease _cardTweenEase = Ease.OutBounce;

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
        private readonly Dictionary<GameObject, Tween> _cardTweenMap = new Dictionary<GameObject, Tween>();

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

            float totalWidth = _cardParentRect.rect.width;
            int count = companies.Count;
            float cardWidth = 300f;
            float spacing = (totalWidth - cardWidth * count) / (count + 1);

            for (int i = 0; i < count; i++)
            {
                var company = companies[i];
                int cardIndex = i;

                var cardWidget = Instantiate(_cardPrefab, _cardParentRect);
                cardWidget.gameObject.name = $"OfferCard_{cardIndex}_{company.CompanyId}";

                // Position cards horizontally within the parent
                var rt = cardWidget.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0, 0.5f);
                    rt.anchorMax = new Vector2(0, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    float xPos = spacing + cardWidth * 0.5f + i * (cardWidth + spacing);
                    rt.anchoredPosition = new Vector2(xPos, _yOffset);
                }

                cardWidget.transform.localScale = Vector3.zero;

                cardWidget.PopulateFromConfig(company);

                // Wire click event via the card's EventTrigger
                var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                int capturedIndex = cardIndex;
                clickEntry.callback.AddListener(_ => OnCardClicked(capturedIndex));

                var hoverEnterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                var cardGo = cardWidget.gameObject;
                hoverEnterEntry.callback.AddListener(_ => OnCardPointerEnter(cardGo));

                var hoverExitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                hoverExitEntry.callback.AddListener(_ => OnCardPointerExit(cardGo));

                if (cardWidget.ButtonEventTrigger != null)
                {
                    cardWidget.ButtonEventTrigger.triggers.Add(clickEntry);
                    cardWidget.ButtonEventTrigger.triggers.Add(hoverEnterEntry);
                    cardWidget.ButtonEventTrigger.triggers.Add(hoverExitEntry);
                }
                else
                {
                    var eventTrigger = cardWidget.gameObject.GetComponent<EventTrigger>();
                    if (eventTrigger == null)
                        eventTrigger = cardWidget.gameObject.AddComponent<EventTrigger>();

                    eventTrigger.triggers.Add(clickEntry);
                    eventTrigger.triggers.Add(hoverEnterEntry);
                    eventTrigger.triggers.Add(hoverExitEntry);
                }

                _cardWidgets.Add(cardWidget);
            }
        }

        private void OnCardClicked(int index)
        {
            if (_context == null)
                return;

            if (index < 0 || index >= _context.OfferedCompanies.Count)
                return;

            _context.ConfirmSelection(_context.OfferedCompanies[index]);
        }

        private void OnCardPointerEnter(GameObject cardGo)
        {
            if (_cardTweenMap.TryGetValue(cardGo, out var existingTween))
                existingTween?.Kill();

            var tween = cardGo.transform
                .DOScale(_cardTweenScale, _cardTweenDuration)
                .SetEase(_cardTweenEase)
                .OnKill(() => _cardTweenMap.Remove(cardGo));

            _cardTweenMap[cardGo] = tween;
        }

        private void OnCardPointerExit(GameObject cardGo)
        {
            if (_cardTweenMap.TryGetValue(cardGo, out var existingTween))
                existingTween?.Kill();

            var tween = cardGo.transform
                .DOScale(1f, _cardTweenDuration)
                .SetEase(_cardTweenEase)
                .OnKill(() => _cardTweenMap.Remove(cardGo));

            _cardTweenMap[cardGo] = tween;
        }

        private void ShowCards()
        {
            foreach (var widget in _cardWidgets)
            {
                var go = widget.gameObject;

                if (_cardTweenMap.TryGetValue(go, out var existingTween))
                    existingTween?.Kill();

                var tween = go.transform
                    .DOScale(1f, _showUIDuration)
                    .SetEase(_showUIEase)
                    .OnKill(() => _cardTweenMap.Remove(go));

                _cardTweenMap[go] = tween;
            }
        }

        private void HideCards()
        {
            foreach (var widget in _cardWidgets)
            {
                var go = widget.gameObject;

                if (_cardTweenMap.TryGetValue(go, out var existingTween))
                    existingTween?.Kill();

                var tween = go.transform
                    .DOScale(0f, _hideUIDuration)
                    .SetEase(_hideUIEase)
                    .OnKill(() => _cardTweenMap.Remove(go));

                _cardTweenMap[go] = tween;
            }
        }

        private void ClearCards()
        {
            // Copy values before iterating — Kill() triggers OnKill callbacks
            // that remove entries from _cardTweenMap during enumeration.
            var tweens = new List<Tween>(_cardTweenMap.Values);
            _cardTweenMap.Clear();

            foreach (var tween in tweens)
                tween?.Kill();

            foreach (var widget in _cardWidgets)
            {
                if (widget != null)
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
