using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

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
        
        [FormerlySerializedAs("_boardItemVisual")] [SerializeField] private BoardItemWrapperBase boardItemWrapper = null;

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

            if (boardItemWrapper.BoardItem != null)
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
            boardItemWrapper.OnInited += OnInited;
        }

        private void UnregisterFromBoardItemVisual()
        {
            boardItemWrapper.OnInited -= OnInited;
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
            
            foreach (BoardItemPieceBase piece in boardItemWrapper.BoardItem.Pieces)
            {
                piece.OnCellUpdated += OnPieceCellUpdated;
            }
        }

        private void UnregisterFromBoardItem()
        {
            foreach (BoardItemPieceBase piece in boardItemWrapper.BoardItem.Pieces)
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

            if (boardItemWrapper.BoardItem.IsPlaceholder)
            {
                maxRow = 99;
            }
            else
            {
                // TODO: might need update for multiple pieces
                maxRow = boardItemWrapper.BoardItem.Pieces.Max(val => val.Cell.Position.y);
            }
            
            foreach (SortingOrderInfo sortingOrderInfo in _sortingOrderInfoColl)
            {
                int order = sortingOrderInfo.BaseSortingOrder + maxRow;

                sortingOrderInfo.SpriteRenderer.sortingOrder = order;
            }
        }
    }
}