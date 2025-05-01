using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.BoardSystem.Authoring
{
    public class CellWrapper : MonoBehaviour
    {
        [field: SerializeField] public Transform VisualParent { get; private set; } = null;
        
        [field: SerializeField] public Transform PlacementPivot { get; private set; } = null;
        
        [SerializeField] private SpriteRenderer _spriteRenderer = null;
        
        private Color _defaultColor;
        
        public Cell Cell { get; private set; }


        private void Awake()
        {
            InitDefaultColor();
        }
        
        private void InitDefaultColor()
        {
            _defaultColor = _spriteRenderer.color;
        }

        public void Init(Cell cell)
        {
            Cell = cell;
        }
        
        public void Highlight(Color color)
        {
            _spriteRenderer.color = color;
        }
        
        public void ClearHighlight()
        {
            _spriteRenderer.color = _defaultColor;
        }
    }
}