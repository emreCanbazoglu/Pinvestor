using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    public abstract class PaintBoardItemConditionBase : MonoBehaviour
    {
        [SerializeField] private string _warning = "";
        
        public bool CanBePainted(BoardItemBase boardItem, BoardItemDataBase data)
        {
            if (IsSatisfied(boardItem, data))
            {
                return true;
            }
            
            NotificationManager.Instance.ShowNotification(new NotificationManager.Notification(_warning));

            return false;
        }

        protected abstract bool IsSatisfied(BoardItemBase boardItem, BoardItemDataBase data);
    }
}