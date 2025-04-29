using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public class CanPlaceBoardItemResult
    {
        public bool CanPlace { get; private set; }
        public Vector2Int[] TargetCellIndices { get; private set; }

        public CanPlaceBoardItemResult(
            bool canPlace,
            Vector2Int[] targetCellIndices)
        {
            CanPlace = canPlace;
            TargetCellIndices = targetCellIndices;
        }
        
        public static CanPlaceBoardItemResult Failure()
        {
            return new CanPlaceBoardItemResult(false, null);
        }
    }
    
    public class Board : IDisposable
    {
        private Dictionary<Vector2Int, Cell> _cells 
            = new Dictionary<Vector2Int, Cell>();
        
        public IReadOnlyDictionary<Vector2Int, Cell> Cells => _cells;

        public readonly HashSet<BoardItemBase> BoardItems 
            = new HashSet<BoardItemBase>();
        
        public Vector2Int Dimensions { get; private set; }

        private readonly BoardItemSOContainer _boardItemSOContainer;
        private readonly BoardItemWrapperPoolManager _boardItemWrapperPoolManager;
        private readonly BoardItemFactory _boardItemFactory;
        private readonly CellLayerInfoSO[] _cellLayerInfoColl;
        
        public Action<BoardItemBase> OnBoardItemAdded { get; set; }
        public Action<BoardItemBase> OnBoardItemRemoved { get; set; }

        public Board(
            BoardItemSOContainer boardItemSOContainer,
            BoardItemWrapperPoolManager boardItemWrapperPoolManager,
            BoardItemFactory boardItemFactory,
            CellLayerInfoSO[] cellLayerInfoColl)
        {
            _boardItemSOContainer = boardItemSOContainer;
            _boardItemWrapperPoolManager = boardItemWrapperPoolManager;
            _boardItemFactory = boardItemFactory;
            _cellLayerInfoColl = cellLayerInfoColl;
        }
        
        public void Init(
            BoardData boardData, 
            bool isReloading = false,
            bool createCells = false)
        {
            Dimensions = boardData.Dimensions;
            
            Debug.Log("Board Init: " + Dimensions);
            
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

            Vector2Int dimensions = boardData.Dimensions;

            for (int x = 0; x < dimensions.x; x++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    List<CellLayer> layers = CreateLayers();
                
                    Cell cell = new Cell(
                        this,
                        x,
                        y,
                        layers);
                
                    _cells.TryAdd(cell.Position, cell);
                }
            }
            
            /*foreach (BoardItemDataBase boardItemData in boardData.BoardItems)
            {
                if (boardItemData.GetBoardItemType()
                    .GetID().Equals(
                        info.BoardItemTypeSO.GetID()))
                {
                }
            }*/
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
            List<BoardItemDataBase> boardItemData = boardData.BoardItems.ToList();
            
            foreach (var data in boardItemData)
            {
                TryGetCellAt(
                    new Vector2Int(
                        data.Col,
                        data.Row),
                    out Cell cell);

                if (cell == null 
                    || !cell.CanAddBoardItem(
                            data.GetBoardItemType() as BoardItemTypeSO))
                    continue;
                
                var boardItem
                    = _boardItemFactory.CreateBoardItem(
                        data);

                if (boardItem == null)
                {
                    Debug.LogError("Couldn't create board item: " + data.GetBoardItemType().GetID());
                    
                    continue;
                }

                AddBoardItemToBoard(boardItem);
            }
        }
        
        public bool TryGetCellAt(
            Vector2Int coordinates,
            out Cell cell)
        {
            return Cells.TryGetValue(coordinates, out cell);
        }
        
        public CanPlaceBoardItemResult CanPlaceBoardItem(
            BoardItemBase boardItem,
            Vector2Int selectedCellIndex)
        {
            var targetCellIndices
                = new Vector2Int[boardItem.Pieces.Count];
            
            int index = 0;
            
            foreach (var piece in boardItem.Pieces)
            {
                Vector2Int targetCellIndex 
                    = selectedCellIndex + piece.LocalCoords;

                if (targetCellIndex.x < 0 || targetCellIndex.x >= Dimensions.x ||
                    targetCellIndex.y < 0 || targetCellIndex.y >= Dimensions.y)
                    return CanPlaceBoardItemResult.Failure();

                if (!Cells[targetCellIndex].CanAddBoardItem(
                        boardItem.GetBoardItemType()))
                    return CanPlaceBoardItemResult.Failure();

                targetCellIndices[index] = targetCellIndex;
                index++;
            }

            return new CanPlaceBoardItemResult(true, targetCellIndices);
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