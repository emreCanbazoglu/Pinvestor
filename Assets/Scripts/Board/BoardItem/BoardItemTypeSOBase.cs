using System;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardItemTypeSOBase : ScriptableObject
    {
        public abstract Enum GetID();
    }
}