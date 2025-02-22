using System;
using System.Linq;
using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    public abstract class BoardItemBase : MonoBehaviour
    {
        [field: SerializeField] public BoardItemTypeSOBase BoardItemTypeSO { get; private set; } = default;

        [field: SerializeField] public BoardItemTerritoryProviderBase TerritoryProvider { get; private set; } = null;
        
        public bool IsPlaceholderItem { get; set; }

        public bool IsPainted { get; protected set; }

        private BoardItemHighlighterBase _highlighter;
        public BoardItemHighlighterBase Highlighter
        {
            get
            {
                if (_highlighter == null)
                {
                    _highlighter = GetComponentInChildren<BoardItemHighlighterBase>();
                }

                return _highlighter;
            }
        }
        
        protected Cell _parentCell;
        public Cell ParentCell
        {
            get
            {
                if (_parentCell == null)
                {
                    _parentCell = GetComponentInParent<Cell>();
                }

                return _parentCell;
            }
        }

        public CellLayer ParentLayer
        {
            get
            {
                ParentCell
                    .CellController
                    .TryGetLayerToPlaceBoardItem(BoardItemTypeSO, out CellLayer layer);

                return layer;
            }
        }

        public abstract BoardItemDataBase GetGenericData();

        public abstract Type GetDataType();

        public abstract bool TryPaint(BoardItemDataBase data);
        
        public abstract bool TryErase(BoardItemDataBase data);
        
        public abstract bool HasSetting();
        
        public abstract bool TryGetSetting(BoardItemDataBase data, out BoardItemSettingBase setting);
    }
    
    public abstract class BoardItemBase<TData, TSetting> : BoardItemBase
        where TData : BoardItemDataBase
        where TSetting : BoardItemSettingBase
    {
        [field: SerializeField] public TSetting[] BoardItemSettings { get; private set; } = new TSetting[]{};
        
        [SerializeField] private bool _hasSetting = false; 

        public TData Data { get; protected set; }

        #region Conditions

        private PaintBoardItemConditionBase[] _paintConditions;
        private PaintBoardItemConditionBase[] _PaintConditions
        {
            get
            {
                if (_paintConditions == null)
                {
                    _paintConditions = GetComponentsInChildren<PaintBoardItemConditionBase>();
                }

                return _paintConditions;
            }
        }
        
        private EraseBoardItemConditionBase[] _eraseConditions;
        private EraseBoardItemConditionBase[] _EraseConditions
        {
            get
            {
                if (_eraseConditions == null)
                {
                    _eraseConditions = GetComponentsInChildren<EraseBoardItemConditionBase>();
                }

                return _eraseConditions;
            }
        }
        

        #endregion
        
        public Action<BoardItemBase> OnPainted { get; set; }

        public override Type GetDataType()
        {
            return typeof(TData);
        }

        public override BoardItemDataBase GetGenericData()
        {
            return Data;
        }

        public TData GetData()
        {
            return Data;
        }

        public override bool HasSetting()
        {
            return BoardItemSettings.Length > 0 || _hasSetting;
        }

        public override bool TryGetSetting(BoardItemDataBase data, out BoardItemSettingBase setting)
        {
            setting = null;
            
            if (_hasSetting)
            {
                return true;
            }
            
            bool result = TryGetSetting((TData)data, out TSetting s);
            
            if (!result)
            {
                return false;
            }

            setting = s;

            return true;
        }

        public bool TryGetSetting(TData data, out TSetting setting)
        {
            setting = null;
            
            if (!HasSetting())
            {
                return false;
            }
            
            setting = BoardItemSettings
                .FirstOrDefault(i => (i as BoardItemSettingBase<TData>).IsEquivalentData(data));

            return setting != null;
        }

        public override bool TryPaint(BoardItemDataBase data)
        {
            return TryPaint((TData)data);
        }

        public bool TryPaint(TData data)
        {
            if (!CanBePainted(data))
            {
                return false;
            }

            PrePaintCustomActions(data);

            Data = data;
            
            IsPainted = true;

            _parentCell = null;
            
            if (!IsPlaceholderItem)
            {
                BoardEditor.Instance.Painted(data);
            }
            
            gameObject.SetActive(true);

            PaintCustomActions(data);

            OnPainted?.Invoke(this);
            
            return true;
        }
        
        public override bool TryErase(BoardItemDataBase data)
        {
            return TryErase((TData)data);
        }

        public bool TryErase(TData data)
        {
            if (!CanBeErased(data))
            {
                return false;
            }

            IsPainted = false;
            
            BoardEditor.Instance.Erased(data);

            EraseCustomActions();

            gameObject.SetActive(false);

            _parentCell = null;

            return true;
        }

        public bool CanBePainted(TData data)
        {
            bool canPaint = _PaintConditions.All(i => i.CanBePainted(this, data));

            canPaint &= CanBePaintedCustomActions(data);
            
            return canPaint;
        }
        
        public bool CanBeErased(TData data)
        {
            bool canErase = _EraseConditions.All(i => i.CanBeErased(this, data));

            canErase &= CanBeErasedCustomActions(data);

            return canErase;
        }

        protected virtual void PrePaintCustomActions(TData data)
        {
            
        }
        
        protected virtual void PaintCustomActions(TData data)
        {
        }

        protected virtual bool CanBePaintedCustomActions(TData data)
        {
            return true;
        }

        protected virtual void EraseCustomActions()
        {
        }
        
        protected virtual bool CanBeErasedCustomActions(TData data)
        {
            return true;
        }
    }
}