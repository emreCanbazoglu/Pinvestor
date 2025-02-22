using System;
using TypeReferences;
using UnityEngine;

namespace Pinvestor.BoardSystem
{
    [CreateAssetMenu(menuName = "Pinvestor/Game/Board Item/Logic/Info/Board Item Info SO")]
    public class BoardItemInfoSO : PuzzleLevelEditor.BoardItemInfoSO
    {
        [ClassImplements(typeof(IBoardItem))]
        public ClassTypeReference BoardItemTypeRef = typeof(BoardItem_Token);

        [field: SerializeField] public BoardItemPropertySOBase[] BoardItemPropertySOs = null;
    }
}