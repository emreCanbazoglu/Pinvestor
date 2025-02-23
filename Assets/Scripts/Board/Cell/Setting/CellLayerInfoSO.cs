using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    [CreateAssetMenu(menuName = "MMGame/Game/Cell/Logic/Cell Layer Info SO")]
    public class CellLayerInfoSO : ScriptableObject
    {
        [field: SerializeField]
        public BoardItemTypeSO[] ValidBoardItemTypes { get; private set; }
            = new BoardItemTypeSO[] { };
        
        [field: SerializeField]
        public bool IsMainLayer { get; private set; }
        
        [field: SerializeField]
        public bool BlocksDrop { get; private set; }
                
        [field: SerializeField]
        public bool BlocksMatch { get; private set; }
    }
}