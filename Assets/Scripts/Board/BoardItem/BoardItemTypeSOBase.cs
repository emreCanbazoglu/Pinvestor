using System;
using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    public abstract class BoardItemTypeSOBase : ScriptableObject
    {
        public abstract Enum GetID();
    }
}