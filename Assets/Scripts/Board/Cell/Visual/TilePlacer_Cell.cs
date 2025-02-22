using System;
using MildMania.PuzzleLevelEditor;
using UnityEngine;

namespace MildMania.MMGame.Game
{
    [Serializable]
    public class TilePlacer_Cell : TilePlacerBase<GameObject>
    {
        [SerializeField] private Transform _parent = null;

        private GameObject _instantiatedCell;
        
        protected override void ToggleTileVisual(
            Vector2 tilePosition,
            GameObject visual,
            ETileVisualType visualType,
            bool isActive)
        {
            if (!isActive)
            {
                return;
            }

            UpdateVisual(visual);
        }

        private void UpdateVisual(GameObject visual)
        {
            if (_instantiatedCell)
            {
                GameObject.Destroy(_instantiatedCell);
            }
            
            _instantiatedCell = GameObject.Instantiate(visual, _parent);
        }

        protected override void ToggleTileVisualPatch(
            Vector2 tilePosition,
            GameObject visual,
            ETileVisualPatchType patchType,
            bool isActive)
        {
        }
    }
}