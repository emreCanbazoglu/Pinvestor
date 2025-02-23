using System;

namespace Pinvestor.BoardSystem.Base
{
    public interface IBoardStabilityChecker
    {
        public Action OnStabilityUpdated { get; set; }
        
        public bool IsStable();
    }
}