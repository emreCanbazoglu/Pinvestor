using TypeReferences;
using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    [CreateAssetMenu(menuName = "Puzzle Game/Board Item/Logic/Info/Board Item Info SO")]
    public class BoardItemInfoSO : ScriptableObject
    {
        [field: SerializeField] public BoardItemTypeSOBase BoardItemTypeSO { get; private set; }
            
        [ClassImplements(typeof(IBoardItemData))]
        public ClassTypeReference BoardItemDataTypeRef = typeof(BoardItemData_GenericItem_Tile);
            
        [field: SerializeField] public bool CanBeObjective = false;
        [field: SerializeField] public bool ObjectiveOnly = false;
    }
}