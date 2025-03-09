using UnityEngine;
using UnityEngine.Serialization;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItemVisualSpriteRendererUpdater : MonoBehaviour
    {
        [FormerlySerializedAs("_boardItemVisual")] [SerializeField] private BoardItemWrapperBase boardItemWrapper = null;

        [SerializeField] private SpriteRenderer[] _spriteRenderers = new SpriteRenderer[]{};

        [SerializeField] private float _defaultAlpha = 1;

        [SerializeField] private float _placeholderAlpha = 0.5f;
        
        public void ResetVisual()
        {
            foreach (SpriteRenderer spriteRenderer in _spriteRenderers)
            {
                Color color = spriteRenderer.color;

                color.a = boardItemWrapper.BoardItem.IsPlaceholder ? _placeholderAlpha : _defaultAlpha;

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
            boardItemWrapper.OnInited += OnInited;
        }

        private void UnregisterFromBoardItemVisual()
        {
            boardItemWrapper.OnInited -= OnInited;
        }

        private void OnInited()
        {
            ResetVisual();
        }
    }
}