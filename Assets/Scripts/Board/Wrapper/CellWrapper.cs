using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.BoardSystem.Authoring
{
    public class CellWrapper : MonoBehaviour
    {
        [field: SerializeField] public Transform VisualParent { get; private set; } = null;
        public Cell Cell { get; private set; }

        public void Init(Cell cell)
        {
            Cell = cell;
        }
    }
}