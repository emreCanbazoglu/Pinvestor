﻿using System.Collections.Generic;

namespace Pinvestor.BoardSystem.Base
{
    public class CellLayer
    {
        public BoardItemPieceBase RegisteredBoardItemPiece { get; private set; }

        public bool IsMainLayer { get; private set; }


        private readonly HashSet<BoardItemTypeSO> _validBoardItemTypes = new();

        private bool _isBoardPieceRegistered = false;
        
        public CellLayer(CellLayerInfoSO cellLayerInfoSO)
        {
            IsMainLayer = cellLayerInfoSO.IsMainLayer;
            
            foreach (BoardItemTypeSO itemType in cellLayerInfoSO.ValidBoardItemTypes)
            {
                _validBoardItemTypes.Add(itemType);
            }
        }

        public bool CanRegisterBoardItem(BoardItemTypeSO boardItemTypeSO)
        {
            return !IsFull() && IsValidBoardItemType(boardItemTypeSO);
        }

        public bool IsFull()
        {
            return _isBoardPieceRegistered;
        }
        
        public bool IsValidBoardItemType(BoardItemTypeSO boardItemTypeSO)
        {
            return _validBoardItemTypes.Contains(boardItemTypeSO);
        }
        
        public bool TryRegisterBoardItemPiece(BoardItemPieceBase boardItemPiece, bool force = false)
        {
            if (!force 
                && !CanRegisterBoardItem(boardItemPiece.ParentItem.GetBoardItemType()))
            {
                return false;
            }

            RegisteredBoardItemPiece = boardItemPiece;

            _isBoardPieceRegistered = true;
            
            return true;
        }

        public bool TryUnregisterBoardItemPiece(BoardItemPieceBase boardItemPiece)
        {
            if (RegisteredBoardItemPiece == boardItemPiece)
            {
                RegisteredBoardItemPiece = null;

                _isBoardPieceRegistered = false;

                return true;
            }

            return false;
        }
    }
}