using System.Collections.Generic;
using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    public class BoardItemTerritoryProvider_Default : BoardItemTerritoryProviderBase
    {
        public override List<Vector2Int> GetTerritory(BoardItemDataBase boardItemData)
        {
            if (boardItemData == null)
            {
                return new List<Vector2Int>() {Vector2Int.zero};
            }
            
            return new List<Vector2Int>(){new Vector2Int(boardItemData.Col,boardItemData.Row)};
        }
    }
}