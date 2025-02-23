using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardStabilityConditionSOBase : ScriptableObject
    {
        public abstract bool IsSatisfied(bool isDebugEnabled = false);
    }
}