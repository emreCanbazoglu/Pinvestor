using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    [CreateAssetMenu(
        fileName = "BoardItemFilter.OnSameRow.asset",
        menuName = "Pinvestor/Game/Board Item/Filters/On Same Row")]
    public class BoardItemFilter_OnSameRow : BoardItemFilterBaseScriptableObject
    {
        public override bool IsValid(
            BoardItemBase source, 
            BoardItemBase target)
        {
            return source.MainPiece.Cell.Position.y
                   == target.MainPiece.Cell.Position.y;
        }
    }
}