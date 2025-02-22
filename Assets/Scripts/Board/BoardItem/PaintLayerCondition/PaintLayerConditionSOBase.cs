using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    public abstract class PaintLayerConditionSOBase : ScriptableObject
    {
        [SerializeField] private string _warning = "";
        
        protected abstract bool IsSatisfied(CellLayer layer, BoardItemDataBase boardItemData);
        
        public bool CanBePainted(CellLayer layer, BoardItemDataBase boardItemData)
        {
            if (IsSatisfied(layer, boardItemData))
            {
                return true;
            }
            
            NotificationManager.Instance.ShowNotification(new NotificationManager.Notification(_warning));

            return false;
        }
    }
}