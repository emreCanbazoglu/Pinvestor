using System;
using System.Collections.Generic;
using MildMania.PuzzleLevelEditor;
using UnityEngine;

namespace Pinvestor.BoardSystem
{
    public abstract class BoardItemBase : IDisposable
    {
        public BoardItemVisualBase Visual { get; protected set; }
        
        public List<BoardItemPieceBase> Pieces { get; protected set; }

        public Dictionary<Type, BoardItemPropertySpecBase> BoardItemPropertySpecs { get; private set; } = new();

        public BoardItemDataBase BoardItemData { get; private set; }

        public bool IsStable { get; private set; }

        public bool IsPlaceholder { get; private set; }
        
        protected BoardItemTypeSO _boardItemTypeSO;

        public Action<bool> OnItemStabilityUpdated { get; set; }

        public Action<BoardItemBase> OnDisposed { get; set; }
        
        private IGameStabilityChecker[] _stabilityCheckers;

        private IGameStabilityChecker[] _StabilityCheckers
        {
            get
            {
                if (_stabilityCheckers == null)
                {
                    List<IGameStabilityChecker> checkers = new List<IGameStabilityChecker>();
                    
                    foreach (KeyValuePair<Type,BoardItemPropertySpecBase> keyValuePair in BoardItemPropertySpecs)
                    {
                        if (keyValuePair.Value is IGameStabilityChecker stabilityChecker)
                        {
                            checkers.Add(stabilityChecker);
                        }
                    }

                    _stabilityCheckers = checkers.ToArray();
                }

                return _stabilityCheckers;
            }
        }

        protected abstract List<BoardItemPieceBase> CreatePieces(bool isPlaceholder = false);
        
        protected abstract BoardItemVisualBase CreateVisual();
        
        protected virtual void InitCore(BoardItemDataBase data)
        {

        }

        protected virtual void DisposeCore()
        {

        }

        protected virtual void CreateItemCore()
        {

        }

        public void Dispose()
        {
            UnregisterFromStabilityCheckers();

            DisposeSpecs();
            
            DisposePieces();

            DisposeVisual();

            DisposeCore();
            
            OnDisposed?.Invoke(this);
        }

        public void Init(
            BoardItemInfoSO infoSO,
            BoardItemDataBase data,
            bool isPlaceholder = false)
        {
            _boardItemTypeSO = (BoardItemTypeSO)infoSO.BoardItemTypeSO;

            BoardItemData = data;

            IsPlaceholder = isPlaceholder;

            InitSpecs(infoSO);

            InitCore(data);
        }

        public void CreateItem()
        {
            Pieces = CreatePieces(IsPlaceholder);

            Visual = CreateVisual();

            OnStabilityUpdated();

            RegisterToStabilityCheckers();

            CreateItemCore();
        }

        public BoardItemTypeSO GetBoardItemType()
        {
            return _boardItemTypeSO;
        }

        public bool CheckIsStable(bool isDebugEnabled = false)
        {
            if (isDebugEnabled && !IsStable)
            {
                foreach (IGameStabilityChecker stabilityChecker in _StabilityCheckers)
                {
                    bool isStable = stabilityChecker.IsStable();

                    if (!isStable)
                    {
                        Debug.Log("Unstable BoardItem: " + GetType() + " Checker: " + stabilityChecker.GetType());
                    }
                }
            }

            return IsStable;
        }

        public bool TryGetPropertySpec<T>(out T spec)
            where T : BoardItemPropertySpecBase
        {
            spec = default;

            if (!BoardItemPropertySpecs.TryGetValue(typeof(T), out BoardItemPropertySpecBase specBase))
            {
                return false;
            }

            spec = (T)specBase;
            
            return true;
        }
        
        public void AddSpec(BoardItemPropertySOBase propertySO)
        {
            BoardItemPropertySpecBase propertySpec = propertySO.CreateSpec(this); 

            BoardItemPropertySpecs.Add(propertySpec.GetType(), propertySpec);
            BoardItemPropertySpecs.TryAdd(propertySpec.GetType().BaseType, propertySpec);
        }

        public void RemoveFromCell()
        {
            DisposePieces();
        }

        private void InitSpecs(BoardItemInfoSO infoSO)
        {
            foreach (BoardItemPropertySOBase propertySO in infoSO.BoardItemPropertySOs)
            {
                AddSpec(propertySO);
            }
        }

        private void RegisterToStabilityCheckers()
        {
            foreach (IGameStabilityChecker gameStabilityChecker in _StabilityCheckers)
            {
                gameStabilityChecker.OnStabilityUpdated += OnStabilityUpdated;
            }
        }

        private void UnregisterFromStabilityCheckers()
        {
            foreach (IGameStabilityChecker gameStabilityChecker in _StabilityCheckers)
            {
                gameStabilityChecker.OnStabilityUpdated -= OnStabilityUpdated;
            }
        }

        private void OnStabilityUpdated()
        {
            bool isStable = IsStable;

            bool allStable = true;
            
            foreach (IGameStabilityChecker stabilityChecker in _StabilityCheckers)
            {
                if (!stabilityChecker.IsStable())
                {
                    allStable = false;
                    break;
                }
            }
            
            IsStable = allStable;

            if (isStable == IsStable)
            {
                return;
            }

            OnItemStabilityUpdated?.Invoke(IsStable);
        }
        
        private void DisposePieces()
        {
            foreach (BoardItemPieceBase piece in Pieces)
            {
                piece.Dispose();
            }
        }

        private void DisposeVisual()
        {
            if (Visual)
            {
                Visual.gameObject.SetActive(false);
            }
        }

        private void DisposeSpecs()
        {
            foreach (KeyValuePair<Type,BoardItemPropertySpecBase> keyValuePair in BoardItemPropertySpecs)
            {
                keyValuePair.Value.Dispose();
            }
        }
    }
}

