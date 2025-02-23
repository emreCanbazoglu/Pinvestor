using TypeReferences;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    [CreateAssetMenu(menuName = "Puzzle Game/Board Item/Logic/Info/Board Item Info SO")]
    public class BoardItemInfoSO : ScriptableObject
    {
        [field: SerializeField] public BoardItemTypeSOBase BoardItemTypeSO { get; private set; }
            
        [ClassImplements(typeof(IBoardItemData))]
        public ClassTypeReference BoardItemDataTypeRef = typeof(BoardItemData_GenericItem_Tile);
        
        [ClassImplements(typeof(IBoardItem))]
        public ClassTypeReference BoardItemTypeRef = typeof(BoardItemBase);

        [field: SerializeField] public BoardItemPropertySOBase[] BoardItemPropertySOs = null;
    }
}