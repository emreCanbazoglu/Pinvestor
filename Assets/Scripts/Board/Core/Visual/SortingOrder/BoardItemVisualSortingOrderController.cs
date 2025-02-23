using System;
using System.Linq;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItemVisualSortingOrderController : MonoBehaviour
    {
        [Serializable]
        private class SortingOrderInfo
        {
            [field: SerializeField] public SpriteRenderer SpriteRenderer { get; private set; } = null;
            
            [field: SerializeField] public int BaseSortingOrder { get; private set; } = 100;
        }
        
        [SerializeField] private BoardItemVisualBase _boardItemVisual = null;

        [SerializeField] private SortingOrderInfo[] _sortingOrderInfoColl = null;

        public void IncreaseOrderBy(int orderIncrease)
        {
            foreach (SortingOrderInfo orderInfo in _sortingOrderInfoColl)
            {
                orderInfo.SpriteRenderer.sortingOrder += orderIncrease;
            }
        }

        public void ResetOrder()
        {
            UpdateSortingOrder();
        }
        
        private void Awake()
        {
            RegisterToBoardItemVisual();

            if (_boardItemVisual.BoardItem != null)
            {
                RegisterToBoardItem();
            }
        }

        private void Start()
        {
            UpdateSortingOrder();
        }

        private void OnDestroy()
        {
            UnregisterFromBoardItemVisual();

            UnregisterFromBoardItem();
        }

        private void RegisterToBoardItemVisual()
        {
            _boardItemVisual.OnInited += OnInited;
        }

        private void UnregisterFromBoardItemVisual()
        {
            _boardItemVisual.OnInited -= OnInited;
        }

        private void OnInited()
        {
            RegisterToBoardItem();
            
            UpdateSortingOrder();
        }
        
        // TODO: register only to single piece
        private void RegisterToBoardItem()
        {
            UnregisterFromBoardItem();
            
            foreach (BoardItemPieceBase piece in _boardItemVisual.BoardItem.Pieces)
            {
                piece.OnCellUpdated += OnPieceCellUpdated;
            }
        }

        private void UnregisterFromBoardItem()
        {
            foreach (BoardItemPieceBase piece in _boardItemVisual.BoardItem.Pieces)
            {
                piece.OnCellUpdated -= OnPieceCellUpdated;
            }
        }

        private void OnPieceCellUpdated(BoardItemPieceBase piece)
        {
            UpdateSortingOrder();
        }

        private void UpdateSortingOrder()
        {
            int maxRow;

            if (_boardItemVisual.BoardItem.IsPlaceholder)
            {
                maxRow = 99;
            }
            else
            {
                // TODO: might need update for multiple pieces
                maxRow = _boardItemVisual.BoardItem.Pieces.Max(val => val.Cell.Row);
            }
            
            foreach (SortingOrderInfo sortingOrderInfo in _sortingOrderInfoColl)
            {
                int order = sortingOrderInfo.BaseSortingOrder + maxRow;

                sortingOrderInfo.SpriteRenderer.sortingOrder = order;
            }
        }
    }
}