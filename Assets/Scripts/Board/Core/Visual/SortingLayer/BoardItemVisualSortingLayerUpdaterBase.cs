using UnityEngine;
using UnityEngine.Serialization;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardItemVisualSortingLayerUpdaterBase : MonoBehaviour
    {
        [FormerlySerializedAs("_boardItemVisual")] [SerializeField] private BoardItemWrapperBase boardItemWrapper = null;
        
        private const string UI_LAYER = "UI";

        public abstract void SetLayer(string layer);
        public abstract void SetDefaultLayer();

        public void MoveToUILayer()
        {
            SetLayer(UI_LAYER);
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
            if (boardItemWrapper.IsInited)
            {
                OnInited();
                return;
            }
            
            boardItemWrapper.OnInited += OnInited;
        }

        private void UnregisterFromBoardItemVisual()
        {
            boardItemWrapper.OnInited -= OnInited;
        }

        private void OnInited()
        {
            SetDefaultLayer();
        }
    }
}