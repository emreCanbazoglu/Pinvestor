using AbilitySystem.Authoring;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardItemFilterBaseScriptableObject : ScriptableObject
    {
        public abstract bool IsValid(BoardItemBase source, BoardItemBase target);
    }
}
