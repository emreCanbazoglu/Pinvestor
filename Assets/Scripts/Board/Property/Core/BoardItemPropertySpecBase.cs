using System;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardItemPropertySpecBase : IDisposable
    {
        public BoardItemPropertySOBase PropertySO { get; private set; }
        
        public BoardItemBase BoardItem { get; private set; }
        
        public BoardItemPropertySpecBase(BoardItemPropertySOBase propertySO, BoardItemBase owner)
        {
            PropertySO = propertySO;
            BoardItem = owner;
        }

        protected virtual void DisposeCore()
        {
            
        }
        
        public void Dispose()
        {
            DisposeCore();
        }
    }
}