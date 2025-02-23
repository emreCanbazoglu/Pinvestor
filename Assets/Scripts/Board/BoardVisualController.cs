using System;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    /*public class BoardVisualController : IDisposable
    {
        public BoardItemVisualBase CreateBoardItemVisual(
            BoardItemTypeSOBase boardItemType,
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
            boardItemVisual.transform.localPosition 
                = GetCellPosition(position);
        }
        
        private void CreateCellVisuals()
        {
            GameSO gameSO = GameSOContainer.Instance.GameSO;
            
            GameObject cellVisualPrefab = gameSO.GetCellInfo().CellVisualPrefab;

            Transform parentTransform = BoardManager.Instance.BoardTransform;
            
            Vector2 cellVisualSize = GetCellVisualSize();

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

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }*/
}