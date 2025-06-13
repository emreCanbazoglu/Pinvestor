using DG.Tweening;
using MMFramework.MMUI;
using Pinvestor.BoardSystem;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem.Authoring;
using UnityEngine;

namespace Pinvestor.UI
{
    public class ShowBoardItemInfoRequestEvent : IEvent
    {
        public BoardItemBase BoardItem { get; private set; }
        
        public ShowBoardItemInfoRequestEvent(BoardItemBase boardItem)
        {
            BoardItem = boardItem;
        }
    }
    
    public class HideBoardItemInfoRequestEvent : IEvent
    {
        public BoardItemBase BoardItem { get; private set; }
        
        public HideBoardItemInfoRequestEvent(BoardItemBase boardItem)
        {
            BoardItem = boardItem;
        }
    }
    
    public class BoardItemInfoUI : VMBase
    {
        [SerializeField] private Transform _cardPivot = null;
        
        [SerializeField] private float _cardTweenDuration = 0.5f;
        [SerializeField] private Ease _cardTweenEase = Ease.OutBounce;
        
        
        private EventBinding<ShowBoardItemInfoRequestEvent> _showInfoBinding;
        private EventBinding<HideBoardItemInfoRequestEvent> _hideInfoBinding;

        private CardWrapperBase _cardWrapper;
        private Transform _cardWrapperParent;
        
        private Tween _showTween;
        
        protected override void AwakeCustomActions()
        {
            _showInfoBinding
                = new EventBinding<ShowBoardItemInfoRequestEvent>(
                    OnShowBoardItemInfoRequest);

            _hideInfoBinding
                = new EventBinding<HideBoardItemInfoRequestEvent>(
                    OnHideBoardItemInfoRequest);
            
            EventBus<ShowBoardItemInfoRequestEvent>.Register(_showInfoBinding);
            EventBus<HideBoardItemInfoRequestEvent>.Register(_hideInfoBinding);

            base.AwakeCustomActions();
        }

        protected override void OnDestroyCustomActions()
        {
            EventBus<ShowBoardItemInfoRequestEvent>.Deregister(_showInfoBinding);
            EventBus<HideBoardItemInfoRequestEvent>.Deregister(_hideInfoBinding);
            
            base.OnDestroyCustomActions();
        }

        private void OnShowBoardItemInfoRequest(
            ShowBoardItemInfoRequestEvent e)
        {
            if (e.BoardItem == null)
                return;
            
            if(!e.BoardItem.TryGetPropertySpec(
                out BoardItemPropertySpec_CardOwner cardOwnerSpec))
                return;
            
            if (cardOwnerSpec.Card == null)
                return;
            
            TryActivate();

            _cardWrapper = cardOwnerSpec.Card.CardWrapper;
            
            _cardWrapperParent = _cardWrapper.transform.parent;
            
            _cardWrapper.transform.SetParent(
                _cardPivot, false);
            
            _cardWrapper.transform.localPosition = Vector3.zero;

            _showTween = _cardWrapper.transform
                .DOScale(1f, _cardTweenDuration)
                .SetEase(_cardTweenEase);
        }
        
        private void OnHideBoardItemInfoRequest(
            HideBoardItemInfoRequestEvent e)
        {
            if (e.BoardItem == null)
                return;

            if(!e.BoardItem.TryGetPropertySpec(
                   out BoardItemPropertySpec_CardOwner cardOwnerSpec))
                return;
            
            if (cardOwnerSpec.Card == null)
                return;
            
            if (_cardWrapper == null 
                || _cardWrapper != cardOwnerSpec.Card.CardWrapper)
                return;
            
            TryDeactivate();
            
            _showTween?.Kill();

            var cachedCardWrapper = _cardWrapper;
            
            _showTween = cachedCardWrapper.transform
                .DOScale(0f, _cardTweenDuration)
                .SetEase(_cardTweenEase)
                .OnComplete(() =>
                {
                    cachedCardWrapper.transform
                        .SetParent(_cardWrapperParent, false);
                    
                    cachedCardWrapper.transform.localPosition = Vector3.zero;
                });

            _cardWrapper = null;
        }
    }
}
