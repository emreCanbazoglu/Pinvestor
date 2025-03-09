using System;
using MMPoolingSystem;
using System.Collections.Generic;
using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.BoardSystem
{
    public class BoardItemWrapperPoolManager : Singleton<BoardItemWrapperPoolManager>
    {
        [SerializeField] private GOPoolController _poolController = null;

        [SerializeField] private GOPoolingInfo[] _poolInfos = null;

        private Dictionary<Enum, GameObjectPool> _gameObjectPools;
        private Dictionary<Enum, GameObjectPool> _GameObjectPools
        {
            get
            {
                InitPoolingInfo();

                return _gameObjectPools;
            }
        }

        public bool TryPopBoardItemVisual(
            BoardItemTypeSOBase boardItemTypeSO,
            out BoardItemWrapperBase boardItemWrapper)
        {
            boardItemWrapper = default;
            
            if (!_GameObjectPools.TryGetValue(boardItemTypeSO.GetID(), out GameObjectPool pool))
            {
                return false;
            }
            
            GOPoolObject poolObject = pool.TryPopPoolObject();

            boardItemWrapper = poolObject.GetComponent<BoardItemWrapperBase>();

            boardItemWrapper.gameObject.SetActive(true);

            return true;
        }

        protected override void AwakeCore()
        {
            InitPoolingInfo();
        }

        private void InitPoolingInfo()
        {
            if (_gameObjectPools != null)
            {
                return;
            }

            _gameObjectPools = new Dictionary<Enum, GameObjectPool>();

            foreach (GOPoolingInfo poolingInfo in _poolInfos)
            {
                BoardItemWrapperBase boardItemWrapper = poolingInfo.PoolObjectSample.GetComponent<BoardItemWrapperBase>();

                BoardItemTypeSO boardItemTypeSO = boardItemWrapper.GetBoardItemTypeSO();

                _gameObjectPools.Add(boardItemTypeSO.GetID(), _poolController.RegisterPool(poolingInfo));
            }
        }

        protected override void OnDestroyCore()
        {
            _gameObjectPools = null;
            
            base.OnDestroyCore();
        }
    }
}