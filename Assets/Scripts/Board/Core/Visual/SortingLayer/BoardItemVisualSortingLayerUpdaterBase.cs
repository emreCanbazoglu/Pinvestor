using UnityEngine;

namespace MildMania.MMGame.Game
{
    public abstract class BoardItemVisualSortingLayerUpdaterBase : MonoBehaviour
    {
        [SerializeField] private BoardItemVisualBase _boardItemVisual = null;
        
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
            if (_boardItemVisual.IsInited)
            {
                OnInited();
                return;
            }
            
            _boardItemVisual.OnInited += OnInited;
        }

        private void UnregisterFromBoardItemVisual()
        {
            _boardItemVisual.OnInited -= OnInited;
        }

        private void OnInited()
        {
            SetDefaultLayer();
        }
    }
}