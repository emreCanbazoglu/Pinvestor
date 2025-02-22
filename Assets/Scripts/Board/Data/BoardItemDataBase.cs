using System;
using Boomlagoon.JSON;
using SerializableData;

namespace Pinvestor.BoardSystem
{
    public interface IBoardItemData
    {
        
    }
    
    public abstract class BoardItemDataBase : ISerializableData, IBoardItemData
    {
        #region Constants
        
        private const string ROW_KEY = "r";
 
        private const string COL_KEY = "c";

        private const string LAYER_KEY = "l";

        #endregion
        
        public int Col { get; set; }
        public int Row { get; set; }

        public int Layer { get; set; }
        
        public abstract int GetObjectiveCount();
        public abstract Enum GetItemID();
        
        public BoardItemDataBase(JSONObject jsonObj)
        {
            Deserialize(jsonObj);
        }

        public BoardItemDataBase()
        {
            
        }

        protected BoardItemDataBase(BoardItemDataBase data)
        {
            Col = data.Col;
            Row = data.Row;
            Layer = data.Layer;
        }

        protected BoardItemDataBase(
            int col,
            int row,
            int layer = -1)
        {
            Col = col;
            Row = row;
            Layer = BoardExtensions.RetrieveBoardLayerID(layer);
        }

        public BoardItemTypeSOBase GetBoardItemType()
        {
            BoardItemSOContainer.Instance.TryGetBoardItemInfoSO(GetItemID(),
                out BoardItemInfoSO info);
            
            return info.BoardItemTypeSO;
        }

        public JSONObject Serialize()
        {
            JSONObject jsonObj = new JSONObject
            {
                { COL_KEY, Col },
                { ROW_KEY, Row },
                { LAYER_KEY, Layer }
            };

            SerializeCustomActions(jsonObj);
            
            return jsonObj;
        }

        public void Deserialize(JSONObject jsonObj)
        {
            Col = (int)jsonObj.GetNumber(COL_KEY);
            Row = (int)jsonObj.GetNumber(ROW_KEY);
            Layer = (int)jsonObj.GetNumber(LAYER_KEY);
            
            DeserializeCustomActions(jsonObj);
        }

        protected virtual void SerializeCustomActions(JSONObject jsonObj)
        {
        }
        
        protected virtual void DeserializeCustomActions(JSONObject jsonObj)
        {
        }
    }
}