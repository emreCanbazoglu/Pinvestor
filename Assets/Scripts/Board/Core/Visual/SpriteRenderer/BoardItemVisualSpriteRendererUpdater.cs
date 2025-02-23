using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItemVisualSpriteRendererUpdater : MonoBehaviour
    {
        [SerializeField] private BoardItemVisualBase _boardItemVisual = null;

        [SerializeField] private SpriteRenderer[] _spriteRenderers = new SpriteRenderer[]{};

        [SerializeField] private float _defaultAlpha = 1;

        [SerializeField] private float _placeholderAlpha = 0.5f;
        
        public void ResetVisual()
        {
            foreach (SpriteRenderer spriteRenderer in _spriteRenderers)
            {
                Color color = spriteRenderer.color;

                color.a = _boardItemVisual.BoardItem.IsPlaceholder ? _placeholderAlpha : _defaultAlpha;

                spriteRenderer.color = color;
            }
        }
        
        private void Awake()
        {
            RegisterToBoardItemVisual();
        }
        
        private void OnDestroy()
        {
            UnregisterFromBoardItemVisual();
        }

        private void RegisterToBoardItemVisual()
        {
            _boardItemVisual.OnInited += OnInited;
        }

        private void UnregisterFromBoardItemVisual()
        {
            _boardItemVisual.OnInited -= OnInited;
        }

        private void OnInited()
        {
            ResetVisual();
        }
    }
}