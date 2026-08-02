using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.BoardSystem.Authoring
{
    public class CellWrapper : MonoBehaviour
    {
        /// <summary>Root the cell visual hangs off. Offset on the board plane by BoardWrapper.</summary>
        [field: SerializeField] public Transform VisualParent { get; private set; } = null;

        /// <summary>Where a placed board item snaps to — sits on top of the cell surface.</summary>
        [field: SerializeField] public Transform PlacementPivot { get; private set; } = null;

        [Tooltip("3D cell visuals. Tinted through a MaterialPropertyBlock so materials are not instanced.")]
        [SerializeField] private Renderer[] _highlightRenderers = null;

        [Tooltip("Legacy 2D cell visual. Only used when no highlight renderers are assigned.")]
        [SerializeField] private SpriteRenderer _spriteRenderer = null;

        [Tooltip("Shader color property tinted on the highlight renderers.")]
        [SerializeField] private string _colorPropertyName = "_BaseColor";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private int _colorPropertyId = BaseColorId;

        private MaterialPropertyBlock _propertyBlock;

        private Color[] _defaultRendererColors;
        private Color _defaultSpriteColor = Color.white;

        public Cell Cell { get; private set; }

        private void Awake()
        {
            InitDefaultColor();
        }

        private void InitDefaultColor()
        {
            _colorPropertyId = string.IsNullOrEmpty(_colorPropertyName)
                ? BaseColorId
                : Shader.PropertyToID(_colorPropertyName);

            if (_spriteRenderer != null)
                _defaultSpriteColor = _spriteRenderer.color;

            if (!HasHighlightRenderers())
                return;

            _propertyBlock = new MaterialPropertyBlock();
            _defaultRendererColors = new Color[_highlightRenderers.Length];

            for (int i = 0; i < _highlightRenderers.Length; i++)
            {
                Renderer renderer = _highlightRenderers[i];

                _defaultRendererColors[i] = renderer != null
                                            && renderer.sharedMaterial != null
                                            && renderer.sharedMaterial.HasProperty(_colorPropertyId)
                    ? renderer.sharedMaterial.GetColor(_colorPropertyId)
                    : Color.white;
            }
        }

        public void Init(Cell cell)
        {
            Cell = cell;
        }

        public void Highlight(Color color)
        {
            if (HasHighlightRenderers())
            {
                for (int i = 0; i < _highlightRenderers.Length; i++)
                    SetRendererColor(i, color);

                return;
            }

            if (_spriteRenderer != null)
                _spriteRenderer.color = color;
        }

        public void ClearHighlight()
        {
            if (HasHighlightRenderers())
            {
                for (int i = 0; i < _highlightRenderers.Length; i++)
                    SetRendererColor(i, _defaultRendererColors[i]);

                return;
            }

            if (_spriteRenderer != null)
                _spriteRenderer.color = _defaultSpriteColor;
        }

        private void SetRendererColor(int index, Color color)
        {
            Renderer renderer = _highlightRenderers[index];

            if (renderer == null)
                return;

            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_colorPropertyId, color);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private bool HasHighlightRenderers()
        {
            return _highlightRenderers != null
                   && _highlightRenderers.Length > 0;
        }
    }
}
