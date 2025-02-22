using UnityEngine;

namespace MildMania.MMGame.Game
{
    public class BoardItemVisualSortingLayerUpdater_SpriteRenderer : BoardItemVisualSortingLayerUpdaterBase
    {
        [SerializeField] private string _defaultLayer = "Default";
        
        [SerializeField] private SpriteRenderer[] _spriteRendererColl = null;

        public override void SetLayer(string layer)
        {
            foreach (SpriteRenderer spriteRenderer in _spriteRendererColl)
            {
                spriteRenderer.sortingLayerName = layer;
            }
        }

        public override void SetDefaultLayer()
        {
            _spriteRendererColl.ForEach(i => i.sortingLayerName = _defaultLayer);
        }
    }
}