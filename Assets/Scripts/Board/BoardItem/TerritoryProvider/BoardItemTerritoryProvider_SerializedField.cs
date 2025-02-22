using System.Collections.Generic;
using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    public class BoardItemTerritoryProvider_SerializedField : BoardItemTerritoryProviderBase
    {
        [SerializeField] private List<Vector2Int> _territory = new List<Vector2Int>();
        
        public override List<Vector2Int> GetTerritory(BoardItemDataBase boardItemData)
        {
            List<Vector2Int> territory = new List<Vector2Int>(_territory);
            
            Vector2Int cellPosition = new Vector2Int(boardItemData.Col, boardItemData.Row);
            
            for (int i = 0; i < territory.Count; i++)
            {
                territory[i] += cellPosition;
            }
            
            return territory;
        }
    }
}