using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    [CreateAssetMenu(
        fileName = "BoardItemFilter.IsCompany.asset",
        menuName = "Pinvestor/Game/Board Item/Filters/Is Company")]
    public class BoardItemFilter_IsCompany : BoardItemFilterBaseScriptableObject
    {
        public override bool IsValid(
            BoardItemBase source, 
            BoardItemBase target)
        {
            return (EBoardItem)target.GetBoardItemType().GetID()
                   == EBoardItem.Company;
        }
    }
}