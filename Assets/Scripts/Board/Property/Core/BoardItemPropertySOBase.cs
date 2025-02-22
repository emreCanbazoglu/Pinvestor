using UnityEngine;

namespace MildMania.MMGame.Game
{
    public abstract class BoardItemPropertySOBase : ScriptableObject
    {
        public abstract BoardItemPropertySpecBase CreateSpec(BoardItemBase owner);
    }
}