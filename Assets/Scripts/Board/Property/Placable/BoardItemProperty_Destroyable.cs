using System;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.BoardSystem
{
    [CreateAssetMenu(
        fileName = "BoardItemProperty.Destroyable.Default.asset",
        menuName = "Pinvestor/Game/Board Item/Property/Destroyable/Default")]
    public class BoardItemProperty_Destroyable : BoardItemPropertySOBase
    {
        [field: SerializeField] public BoardItemActionPlayer DestroyActions { get; private set; } = null;
        
        public override BoardItemPropertySpecBase CreateSpec(
            BoardItemBase owner)
        {
            return new BoardItemPropertySpec_Destroyable(
                this,
                owner);
        }
    }

    public class BoardItemPropertySpec_Destroyable : BoardItemPropertySpecBase
    {
        public BoardItemProperty_Destroyable CastedSO { get; private set; }
        
        private bool _isDestroying;
        public bool IsDestroying
        {
            get => _isDestroying;
            protected set
            {
                _isDestroying = value;
                
                OnStabilityUpdated?.Invoke();
            }
        }
        
        public Action OnStabilityUpdated { get; set; }
        public Action OnPreDestroy { get; set; }
        public Action OnPostDestroy { get; set; }

        
        public BoardItemPropertySpec_Destroyable(
            BoardItemPropertySOBase propertySO,
            BoardItemBase owner) : base(propertySO, owner)
        {
            CastedSO = (BoardItemProperty_Destroyable)propertySO;
        }

        public bool IsStable()
        {
            return !IsDestroying;
        }
        
        public void Destroy(Action onDestroyed)
        {
            if (IsDestroying)
            {
                return;
            }
            
            IsDestroying = true;
            
            DestroyCore(onCompleted);

            void onCompleted()
            {
                if (GameManager.Instance == null)
                    return;
                
                GameManager.Instance.BoardWrapper.Board
                    .TryRemoveBoardItem(BoardItem);

                OnPreDestroy?.Invoke();

                IsDestroying = false;
                
                BoardItem.Dispose();
                
                OnPostDestroy?.Invoke();
                
                onDestroyed?.Invoke();
            }
        }
        
        protected virtual void DestroyCore(Action onCompleted)
        {
            CastedSO.DestroyActions.Execute(BoardItem, onExecuted);
            return;

            void onExecuted()
            {
                onCompleted?.Invoke();
            }
        }
    }
}