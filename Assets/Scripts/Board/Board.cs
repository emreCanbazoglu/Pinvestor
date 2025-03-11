using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public class Board : IDisposable
    {
        private Dictionary<Vector2Int, Cell> _cells 
            = new Dictionary<Vector2Int, Cell>();
        
        public IReadOnlyDictionary<Vector2Int, Cell> Cells => _cells;

        public HashSet<BoardItemBase> BoardItems = new HashSet<BoardItemBase>();
        
        public Vector2Int Dimensions { get; private set; }
        
        public Action<BoardItemBase> OnBoardItemAdded { get; set; }
        public Action<BoardItemBase> OnBoardItemRemoved { get; set; }
        

        private BoardItemSOContainer _boardItemSOContainer;
        private BoardItemWrapperPoolManager _boardItemWrapperPoolManager;
        private IBoardItemCreator[] _boardItemCreators;
        private CellLayerInfoSO[] _cellLayerInfoColl;

        public Board(
            BoardItemSOContainer boardItemSOContainer,
            BoardItemWrapperPoolManager boardItemWrapperPoolManager,
            IBoardItemCreator[] boardItemCreators,
            CellLayerInfoSO[] cellLayerInfoColl)
        {
            _boardItemSOContainer = boardItemSOContainer;
            _boardItemWrapperPoolManager = boardItemWrapperPoolManager;
            _boardItemCreators = boardItemCreators;
            _cellLayerInfoColl = cellLayerInfoColl;
        }
        
        public void Init(
            BoardData boardData, 
            bool isReloading = false,
            bool createCells = false)
        {
            Dimensions = boardData.Dimensions;
            
            if (!isReloading)
            {
                ResetBoardOffset(boardData);

                if (createCells)
                {
                    CreateCells(boardData);
                }
            }
            
            CreateItems(boardData);
        }
        
        public void TryRemoveBoardItem(BoardItemBase boardItem)
        {
            bool canRemove = BoardItems.Remove(boardItem);

            if (canRemove)
            {
                OnBoardItemRemoved?.Invoke(boardItem);
            }
        }

        public void AddBoardItemToBoard(BoardItemBase boardItem)
        {
            BoardItems.Add(boardItem);
            
            OnBoardItemAdded?.Invoke(boardItem);
        }
        
        public bool TryCreateNewBoardItem(
            BoardItemDataBase boardItemData, 
            out BoardItemBase boardItem,
            bool addToCell = true,
            bool addToBoard = true,
            bool isPlaceholder = false)
        {
            boardItem = null;
            
            BoardItemSOContainer.Instance.TryGetBoardItemInfoSO(
                boardItemData.GetBoardItemType().GetID(),
                out BoardItemInfoSO genericInfoSO);

            if (genericInfoSO is not BoardItemInfoSO infoSO)
            {
                return false;
            }

            if (addToCell)
            {
                TryGetCellAt(
                    new Vector2Int(
                        boardItemData.Col,
                        boardItemData.Row),
                    out Cell cell);

                if (cell == null || !cell.CanAddBoardItem((BoardItemTypeSO) infoSO.BoardItemTypeSO))
                {
                    return false;
                }
            }
            else
            {
                boardItemData.Col = -1;
                boardItemData.Row = -1;
            }
            
            boardItem = (BoardItemBase) Activator.CreateInstance(infoSO.BoardItemTypeRef);

            boardItem.Init(infoSO, boardItemData, isPlaceholder);

            boardItem.CreateItem();

            if (addToBoard)
            {
                AddBoardItemToBoard(boardItem);
            }

            return true;
        }

        private void ResetBoardOffset(BoardData boardData)
        {
            int maxIndexX = -1;
            int maxIndexY = -1;

            int minIndexX = 99;
            int minIndexY = 99;
            
            foreach (BoardItemDataBase boardItemData in boardData.BoardItems)
            {
                maxIndexX = Math.Max(maxIndexX, boardItemData.Col);
                maxIndexY = Math.Max(maxIndexY, boardItemData.Row);
                
                minIndexX = Math.Min(minIndexX, boardItemData.Col);
                minIndexY = Math.Min(minIndexY, boardItemData.Row);
            }

            Vector2Int offset = new Vector2Int(minIndexX, minIndexY);
            
            foreach (BoardItemDataBase boardItemData in boardData.BoardItems)
            {
                boardItemData.Col -= offset.x;
                boardItemData.Row -= offset.y;
            }
        }
        
        private void CreateCells(BoardData boardData)
        {
            _cells = new Dictionary<Vector2Int, Cell>();
            
            _boardItemSOContainer.TryGetBoardItemInfoSO(
                EGenericBoardItemType.Tile, out BoardItemInfoSO info);
            
            foreach (BoardItemDataBase boardItemData in boardData.BoardItems)
            {
                if (boardItemData.GetBoardItemType().GetID().Equals(info.BoardItemTypeSO.GetID()))
                {
                    continue;
                }
                
                List<CellLayer> layers = CreateLayers();
                
                Cell cell = new Cell(
                    this,
                    boardItemData.Col,
                    boardItemData.Row,
                    layers);
                
                _cells.TryAdd(cell.Position, cell);
            }
            
            foreach (BoardItemDataBase boardItemData in boardData.BoardItems)
            {
                if (!boardItemData.GetBoardItemType().GetID().Equals(info.BoardItemTypeSO.GetID()))
                {
                    continue;
                }

                if (Cells.ContainsKey(
                        new Vector2Int(
                            boardItemData.Col,
                            boardItemData.Row)))
                {
                    continue;
                }
                
                List<CellLayer> layers = CreateLayers();
                
                Cell cell = new Cell(
                    this,
                    boardItemData.Col,
                    boardItemData.Row,
                    layers);
                
                _cells.TryAdd(cell.Position, cell);
            }
        }

        private List<CellLayer> CreateLayers()
        {
            List<CellLayer> layers = new List<CellLayer>();
            
            foreach (CellLayerInfoSO info in _cellLayerInfoColl)
            {
                CellLayer layer = new CellLayer(info);
                
                layers.Add(layer);
            }

            return layers;
        }
        
        private void CreateItems(BoardData boardData)
        {
            List<BoardItemDataBase> boardItems = boardData.BoardItems.ToList();
            
            foreach (IBoardItemCreator itemCreator in _boardItemCreators)
            {
                itemCreator.CreateItems(
                    this,
                    boardItems,
                    out List<BoardItemDataBase> filteredBoardItems);

                boardItems = filteredBoardItems;
            }
        }
        
        public bool TryGetCellAt(
            Vector2Int coordinates,
            out Cell cell)
        {
            return Cells.TryGetValue(coordinates, out cell);
        }

        public void Dispose()
        {
            foreach (BoardItemBase boardItem in BoardItems.ToList())
            {
                TryRemoveBoardItem(boardItem);
                
                boardItem.Dispose();
            }
        }
    }
}