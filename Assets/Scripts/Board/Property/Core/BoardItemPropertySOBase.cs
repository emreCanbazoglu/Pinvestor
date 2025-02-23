using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardItemPropertySOBase : ScriptableObject
    {
        public abstract BoardItemPropertySpecBase CreateSpec(BoardItemBase owner);
    }
}