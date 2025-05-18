using AbilitySystem.Authoring;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardItemFilterBaseScriptableObject : ScriptableObject,
        IAbilityTargetFilter
    {
        public abstract bool IsValid(BoardItemBase source, BoardItemBase target);

        public virtual string GetSubjectQualifier()
        {
            return string.Empty;
        }

        public virtual string GetLocationQualifier()
        {
            return string.Empty;
        }
    }
}
