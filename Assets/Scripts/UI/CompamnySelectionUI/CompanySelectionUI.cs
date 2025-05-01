using System.Collections.Generic;
using DG.Tweening;
using MMFramework.MMUI;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.CardSystem.Authoring;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Pinvestor.UI
{
    public class InitializeCompanySelectionUIEvent : IEvent
    {
        public CompanySelectionPileWrapper PileWrapper { get; }

        public InitializeCompanySelectionUIEvent(
            CompanySelectionPileWrapper pileWrapper)
        {
            PileWrapper = pileWrapper;
        }
    }
    
    public class ShowCompanySelectionUIEvent : IEvent { }
    public class HideCompanySelectionUIEvent : IEvent { }
    
    public class CompanyCardSelectedEvent : IEvent
    {
        public CompanyCardWrapper CompanyCard { get; }

        public CompanyCardSelectedEvent(
            CompanyCardWrapper companyCard)
        {
            CompanyCard = companyCard;
        }
    }
    
    public class CompanySelectionUI : VMBase
    {
        [SerializeField] private RectTransform _cardParentRect = null;
        [SerializeField] private float _yOffset = 100f;
        
        [SerializeField] private float _cardTweenScale = 1.2f;
        [SerializeField] private float _cardTweenDuration = 0.5f;
        [SerializeField] private Ease _cardTweenEase = Ease.OutBounce;
        
        [SerializeField] private float _hideUIDuration = 0.2f;
        [SerializeField] private Ease _hideUIEase = Ease.OutBack;
        
        [SerializeField] private float _showUIDuration = 0.2f;
        [SerializeField] private Ease _showUIEase = Ease.OutBack;
        
        private CompanySelectionPileWrapper _pileWrapper;
        
        private EventBinding<InitializeCompanySelectionUIEvent> _initializeEventBinding;
        private EventBinding<ShowCompanySelectionUIEvent> _showEventBinding;
        private EventBinding<HideCompanySelectionUIEvent> _hideEventBinding;
        
        private readonly Dictionary<CompanyCardWrapper, List<EventTrigger.Entry>>
            _cardEventTriggerEntryMap
                = new Dictionary<CompanyCardWrapper, List<EventTrigger.Entry>>();
        
        private readonly Dictionary<CompanyCardWrapper, Tween> 
            _cardTweenMap
                = new Dictionary<CompanyCardWrapper, Tween>();
        
        protected override void AwakeCustomActions()
        {
            _initializeEventBinding
                = new EventBinding<InitializeCompanySelectionUIEvent>(
                    OnInitializeUIEvent);
            
            _showEventBinding
                = new EventBinding<ShowCompanySelectionUIEvent>(
                    OnShowUIEvent);
            
            _hideEventBinding
                = new EventBinding<HideCompanySelectionUIEvent>(
                    OnHideUIEvent);
            
            EventBus<InitializeCompanySelectionUIEvent>.Register(_initializeEventBinding);
            EventBus<ShowCompanySelectionUIEvent>.Register(_showEventBinding);
            EventBus<HideCompanySelectionUIEvent>.Register(_hideEventBinding);
            
            base.AwakeCustomActions();
        }
        
        protected override void OnDestroyCustomActions()
        {
            EventBus<InitializeCompanySelectionUIEvent>.Deregister(_initializeEventBinding);
            EventBus<ShowCompanySelectionUIEvent>.Deregister(_showEventBinding);
            EventBus<HideCompanySelectionUIEvent>.Deregister(_hideEventBinding);
            
            ClearCardEventTriggers();
            
            base.OnDestroyCustomActions();
        }

        protected override void DeactivatedCustomActions()
        {
            ClearCardEventTriggers();
            ClearCardTweens();
            
            base.DeactivatedCustomActions();
        }

        private void OnInitializeUIEvent(
            InitializeCompanySelectionUIEvent e)
        {
            _pileWrapper = e.PileWrapper;

            var companyCards
                = _pileWrapper.CompanyCardMap;

            foreach (var kvp in companyCards)
            {
                var company = kvp.Key;
                var card = kvp.Value;

                card.transform.SetParent(
                    _cardParentRect,
                    false);

                RepositionCard(
                    card,
                    company);
            }
        }

        private void OnShowUIEvent(
            ShowCompanySelectionUIEvent e)
        {
            var companyCards
                = _pileWrapper.CompanyCardMap;

            foreach (var kvp in companyCards)
            {
                var company = kvp.Key;
                var card = kvp.Value;

                _cardEventTriggerEntryMap
                    .TryGetValue(card, out var eventTriggerEntries);

                if (eventTriggerEntries == null)
                {
                    eventTriggerEntries = new List<EventTrigger.Entry>();
                    _cardEventTriggerEntryMap[card] = eventTriggerEntries;
                }

                EventTrigger.Entry onPointerEnterEntry
                    = new EventTrigger.Entry
                    {
                        eventID = EventTriggerType.PointerEnter
                    };

                onPointerEnterEntry.callback.AddListener(
                    (data) => 
                        OnCardPointerEnter(card, (PointerEventData)data));

                card.Widget.ButtonEventTrigger
                    .triggers.Add(onPointerEnterEntry);

                EventTrigger.Entry onPointerExitEntry
                    = new EventTrigger.Entry
                    {
                        eventID = EventTriggerType.PointerExit
                    };

                onPointerExitEntry.callback.AddListener(
                    (data) => 
                        OnCardPointerExit(card, (PointerEventData)data));

                card.Widget.ButtonEventTrigger
                    .triggers.Add(onPointerExitEntry);

                _cardEventTriggerEntryMap[card]
                    .Add(onPointerEnterEntry);
                _cardEventTriggerEntryMap[card]
                    .Add(onPointerExitEntry);
                
                var tween = card.transform
                    .DOScale(1f, _showUIDuration)
                    .SetEase(_showUIEase)
                    .OnKill(() => _cardTweenMap.Remove(card));
                
                _cardTweenMap[card] = tween;
            }
        }

        private void RepositionCard(
            CompanyCardWrapper card,
            BoardItemWrapper_Company company)
        {
            var worldPosition = company.transform.position;
            var screenPosition 
                = Camera.main.WorldToScreenPoint(worldPosition);
            
            RectTransform cardRectTransform = card.GetComponent<RectTransform>();
            
            Vector2 uiPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _cardParentRect, screenPosition, Camera.main, out uiPosition))
            {
                cardRectTransform.anchoredPosition 
                    = uiPosition + new Vector2(0, _yOffset);
            }
        }
        
        private void OnCardPointerEnter(
            CompanyCardWrapper card,
            PointerEventData eventData)
        {
            HighlightCard(card);
        }
        
        private void OnCardPointerExit(
            CompanyCardWrapper card,
            PointerEventData eventData)
        {
            UnhighlightCardEvent(
                card);
        }
        
        private void HighlightCard(
            CompanyCardWrapper card)
        {
            var tween = _cardTweenMap.GetValueOrDefault(card);

            tween?.Kill();
            
            tween = card.transform
                .DOScale(_cardTweenScale, _cardTweenDuration)
                .SetEase(_cardTweenEase)
                .OnKill(() => _cardTweenMap.Remove(card));
            
            _cardTweenMap[card] = tween;
        }
        
        private void UnhighlightCardEvent(
            CompanyCardWrapper card)
        {
            var tween = _cardTweenMap.GetValueOrDefault(card);

            tween?.Kill();
            
            tween = card.transform
                .DOScale(1f, _cardTweenDuration)
                .SetEase(_cardTweenEase)
                .OnKill(() => _cardTweenMap.Remove(card));
            
            _cardTweenMap[card] = tween;
        }
        

        private void OnHideUIEvent(
            HideCompanySelectionUIEvent e)
        {
            var companyCards
                = _pileWrapper.CompanyCardMap;
            
            foreach (var kvp in companyCards)
            {
                var card = kvp.Value;
                var tween = _cardTweenMap.GetValueOrDefault(card);

                tween.Kill();
                
                tween = card.transform
                    .DOScale(0, _hideUIDuration)
                    .SetEase(_hideUIEase)
                    .OnKill(() => _cardTweenMap.Remove(card));
                
                _cardTweenMap[card] = tween;
            }

            ClearCardEventTriggers();
        }

        private void ClearCardEventTriggers()
        {
            foreach (var kvp in _cardEventTriggerEntryMap)
            {
                var card = kvp.Key;
                var eventTriggerEntries = kvp.Value;

                foreach (var entry in eventTriggerEntries)
                {
                    card.Widget.ButtonEventTrigger
                        .triggers.Remove(entry);
                    
                    entry.callback.RemoveAllListeners();
                }
            }
            
            _cardEventTriggerEntryMap.Clear();
        }

        private void ClearCardTweens()
        {
            foreach (var kvp in _cardTweenMap)
            {
                var tween = kvp.Value;
                tween.Kill();
            }

            _cardTweenMap.Clear();
        }
    }
}
