using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardStabilityConditionSOBase : ScriptableObject
    {
        public abstract bool IsSatisfied(
            Board board,
            bool isDebugEnabled = false);
    }
}