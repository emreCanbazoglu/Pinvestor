using System;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardItemSettingBase
    {
        
    }
    
    [Serializable]
    public abstract class BoardItemSettingBase<TData>  : BoardItemSettingBase
        where TData : BoardItemDataBase
    {
        public abstract bool IsEquivalentData(TData data);
    }
}