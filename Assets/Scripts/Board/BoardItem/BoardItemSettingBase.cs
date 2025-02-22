using System;

namespace MildMania.PuzzleLevelEditor
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