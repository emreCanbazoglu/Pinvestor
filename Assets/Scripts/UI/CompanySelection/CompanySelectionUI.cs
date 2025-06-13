using System.Collections.Generic;
using System.ComponentModel;
using DG.Tweening;
using MMFramework.MMUI;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem.Authoring;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityWeld.Binding;

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
    public class DeactivateCompanySelectionUIEvent : IEvent { }
    

    
    public class CompanyCardSelectedEvent : IEvent
    {
        public CompanyCardWrapper CompanyCard { get; }

        public CompanyCardSelectedEvent(
            CompanyCardWrapper companyCard)
        {
            CompanyCard = companyCard;
        }
    }
    
    [Binding]
    public class CompanySelectionUI : VMBase
    {
        [SerializeField] private ButtonWidget _hideButton = null;
        [SerializeField] private ButtonWidget _showButton = null;
        
        [SerializeField] private Image _bgImage = null;
        
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
        private EventBinding<DeactivateCompanySelectionUIEvent> _deactivateEventBinding;
        
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
            
            _deactivateEventBinding
                = new EventBinding<DeactivateCompanySelectionUIEvent>(
                    OnDeactivateUIEvent);
            
            EventBus<InitializeCompanySelectionUIEvent>.Register(_initializeEventBinding);
            EventBus<ShowCompanySelectionUIEvent>.Register(_showEventBinding);
            EventBus<HideCompanySelectionUIEvent>.Register(_hideEventBinding);
            EventBus<DeactivateCompanySelectionUIEvent>.Register(_deactivateEventBinding);
            
            base.AwakeCustomActions();
        }

        protected override void OnDestroyCustomActions()
        {
            EventBus<InitializeCompanySelectionUIEvent>.Deregister(_initializeEventBinding);
            EventBus<ShowCompanySelectionUIEvent>.Deregister(_showEventBinding);
            EventBus<HideCompanySelectionUIEvent>.Deregister(_hideEventBinding);
            EventBus<DeactivateCompanySelectionUIEvent>.Deregister(_deactivateEventBinding);
            
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
                
                EventTrigger.Entry onClickEntry
                    = new EventTrigger.Entry
                    {
                        eventID = EventTriggerType.PointerClick
                    };
                
                onClickEntry.callback.AddListener(
                    (data) => OnCardClick(card, (PointerEventData)data));
                
                card.Widget.ButtonEventTrigger
                    .triggers.Add(onClickEntry);

                _cardEventTriggerEntryMap[card]
                    .Add(onPointerEnterEntry);
                _cardEventTriggerEntryMap[card]
                    .Add(onPointerExitEntry);
                _cardEventTriggerEntryMap[card]
                    .Add(onClickEntry);
                
                var tween = card.transform
                    .DOScale(1f, _showUIDuration)
                    .SetEase(_showUIEase)
                    .OnKill(() => _cardTweenMap.Remove(card));
                
                _cardTweenMap[card] = tween;
            }
            
            _bgImage.enabled = true;
            
            TryActivate();
            
            _hideButton.TryActivate();
            _showButton.TryDeactivate();
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
        
        private void OnCardClick(
            CompanyCardWrapper card, 
            PointerEventData eventData)
        {
            if(eventData.button != PointerEventData.InputButton.Left)
                return;
            
            EventBus<CompanyCardSelectedEvent>
                .Raise(new CompanyCardSelectedEvent(card));
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
            
            _bgImage.enabled = false;
            
            _hideButton.TryDeactivate();
            _showButton.TryActivate();
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
        
        private void OnDeactivateUIEvent(
            DeactivateCompanySelectionUIEvent e)
        {
            ClearCardEventTriggers();
            ClearCardTweens();
            
            _bgImage.enabled = false;
            
            _pileWrapper = null;
            
            TryDeactivate();
        }
        
        [Binding]
        public void OnHideButtonClick()
        {
            EventBus<HideCompanySelectionUIEvent>
                .Raise(new HideCompanySelectionUIEvent());
            
            EventBus<OnViewBoardModeEnterEvent>
                .Raise(new OnViewBoardModeEnterEvent());
        }

        [Binding]
        public void OnShowButtonClick()
        {
            EventBus<ShowCompanySelectionUIEvent>
                .Raise(new ShowCompanySelectionUIEvent());
            
            EventBus<OnViewBoardModeExitEvent>
                .Raise(new OnViewBoardModeExitEvent());
        }
    }
}
