using System;
using System.Collections.Generic;
using System.Linq;
using MildMania.PuzzleLevelEditor;
using UnityEngine;

namespace Pinvestor.BoardSystem
{
    public class Board : IDisposable
    {
        private Dictionary<Vector2, Cell> _cells 
            = new Dictionary<Vector2, Cell>();
        
        public IReadOnlyDictionary<Vector2, Cell> Cells => _cells;

        public HashSet<BoardItemBase> BoardItems = new HashSet<BoardItemBase>();
        
        public Vector2Int Dimensions { get; private set; }
        
        public Action<BoardItemBase> OnBoardItemAdded { get; set; }
        public Action<BoardItemBase> OnBoardItemRemoved { get; set; }
        

        private BoardItemSOContainer _boardItemSOContainer;
        private BoardItemVisualPoolManager _boardItemVisualPoolManager;
        private IBoardItemCreator[] _boardItemCreators;

        public Board(
            BoardItemSOContainer boardItemSOContainer,
            BoardItemVisualPoolManager boardItemVisualPoolManager,
            IBoardItemCreator[] boardItemCreators)
        {
            _boardItemSOContainer = boardItemSOContainer;
            _boardItemVisualPoolManager = boardItemVisualPoolManager;
            _boardItemCreators = boardItemCreators;
        }
        
        public void Init(
            BoardData boardData, 
            bool isReloading,
            bool createCells)
        {
            if (!isReloading)
            {
                ResetBoardOffset(boardData);

                if (createCells)
                {
                    CreateCells(boardData);

                    CreateCellVisuals();
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

        public BoardItemVisualBase CreateBoardItemVisual(
            BoardItemTypeSO boardItemType,
            BoardItemBase boardItem)
        {
            BoardItemVisualPoolManager
                .Instance
                .TryPopBoardItemVisual(boardItemType, out BoardItemVisualBase boardItemVisual);

#if UNITY_EDITOR                
            boardItemVisual.gameObject.name = "BoardItem_" + boardItemType.GetID();
#endif            
            
            boardItemVisual.Init(boardItem);
            
            boardItemVisual.transform.SetParent(BoardManager.Instance.BoardItemTransform);

            Vector2 position = new Vector2(boardItem.BoardItemData.Col, boardItem.BoardItemData.Row);
            
            //SetBoardItemVisualPosition(boardItemVisual, position);

            return boardItemVisual;
        }

        public void SetBoardItemVisualPosition(
            BoardItemVisualBase boardItemVisual,
            Vector2 position)
        {
            boardItemVisual.transform.localPosition = GetCellPosition(position);
        }

        public bool IsGeneratorCell(Cell cell)
        {
            return GeneratorCells.Contains(cell);
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
                out PuzzleLevelEditor.BoardItemInfoSO genericInfoSO);

            if (genericInfoSO is not BoardItemInfoSO infoSO)
            {
                return false;
            }

            if (addToCell)
            {
                BoardManager.Instance.Board.TryGetCellAt(new Vector2(boardItemData.Col, boardItemData.Row), out Cell cell);

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
            
            Dimensions = new Vector2Int(maxIndexX - minIndexX + 1, maxIndexY - minIndexY + 1);

            Dimensions = new Vector2Int(
                GameSOContainer.Instance.GameSO.MaxColDimension,
                GameSOContainer.Instance.GameSO.MaxRowDimension);
            
            foreach (BoardItemDataBase boardItemData in boardData.BoardItems)
            {
                boardItemData.Col -= offset.x;
                boardItemData.Row -= offset.y;
            }
        }
        
        private void CreateCells(BoardData boardData)
        {
            Cells = new Dictionary<Vector2, Cell>();
            
            BoardItemSOContainer.Instance.TryGetBoardItemInfoSO(
                EGenericBoardItemType.Tile, out PuzzleLevelEditor.BoardItemInfoSO info);
            
            foreach (BoardItemDataBase boardItemData in boardData.BoardItems)
            {
                if (boardItemData.GetBoardItemType().GetID().Equals(info.BoardItemTypeSO.GetID()))
                {
                    continue;
                }
                
                List<CellLayer> layers = CreateLayers();
                
                Cell cell = new Cell(
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

                if (Cells.ContainsKey(new Vector2(boardItemData.Col, boardItemData.Row)))
                {
                    continue;
                }
                
                List<CellLayer> layers = CreateLayers();
                
                Cell cell = new Cell(
                    boardItemData.Col,
                    boardItemData.Row,
                    layers);
                
                _cells.TryAdd(cell.Position, cell);
            }
        }

        private List<CellLayer> CreateLayers()
        {
            CellLayerInfoSO[] cellLayerInfoColl = GameSOContainer.Instance.GameSO.GetCellInfo().CellLayerInfoColl;

            List<CellLayer> layers = new List<CellLayer>();
            
            foreach (CellLayerInfoSO info in cellLayerInfoColl)
            {
                CellLayer layer = new CellLayer(info);
                
                layers.Add(layer);
            }

            return layers;
        }

        private void CreateCellVisuals()
        {
            GameSO gameSO = GameSOContainer.Instance.GameSO;
            
            GameObject cellVisualPrefab = gameSO.GetCellInfo().CellVisualPrefab;

            Transform parentTransform = BoardManager.Instance.BoardTransform;

            Board board = BoardManager.Instance.Board;

            Vector2 cellVisualSize = board.GetCellVisualSize();

            foreach (KeyValuePair<Vector2, Cell> keyValuePair in Cells)
            {
                Vector2 position = keyValuePair.Key;
                
                Vector3 cellPosition = GetCellPosition(position);
                
                GameObject cellObject = GameObject.Instantiate(
                    cellVisualPrefab,
                    cellPosition,
                    Quaternion.identity,
                    parentTransform);

#if UNITY_EDITOR                
                cellObject.name = "Cell_" + position.x + "_" + position.y;
#endif
                
                CellVisual cellVisual = cellObject.GetComponent<CellVisual>();

                this.TryGetCellAt(position, out Cell cell);
                
                cellVisual.VisualParent.localPosition = Vector3.zero;
                
                cellVisual.Init(cell);
                cell.InitVisual(cellVisual);
            }
        }

        public Vector3 GetCellPosition(Vector2 position)
        {
            Vector2 cellSize = GameSOContainer.Instance.GameSO.GetCellInfo().CellSize;
            
            return new Vector3(cellSize.x * position.x, cellSize.y * position.y, 0);
        }
        
        private void CreateItems(BoardData boardData)
        {
            List<BoardItemDataBase> boardItems = boardData.BoardItems.ToList();
            
            foreach (IBoardItemCreator itemCreator in _boardItemCreators)
            {
                itemCreator.CreateItems(
                    boardItems,
                    out List<BoardItemDataBase> filteredBoardItems);

                boardItems = filteredBoardItems;
            }
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