using System;
using System.Collections.Generic;
using Boomlagoon.JSON;
using SerializableData;

namespace Pinvestor.BoardSystem
{
    public interface IBaseBoardItemData
    {
        Enum GetItemID();
        void SetID(Enum type);
    }
    
    public class BoardData : ISerializableData
    {
        #region Constants
        
        private const string ITEMS_KEY = "Items";

        #endregion
        
        public List<BoardItemDataBase> BoardItems { get; private set; }


        public BoardData()
        {
            BoardItems = new List<BoardItemDataBase>();
        }

        public BoardData(
            List<BoardItemDataBase> boardItems)
        {
            BoardItems = new List<BoardItemDataBase>(boardItems);
        }
        
        public BoardData(JSONObject jsonObj)
        {
            Deserialize(jsonObj);
        }

        public JSONObject Serialize()
        {
            JSONObject jsonObj = new JSONObject();

            JSONObject itemsJsonObj = new JSONObject();
            
            foreach (BoardItemInfoSO boardItemInfoSO in BoardItemDataFactory.GetBoardItemDataMap())
            {
                JSONArray jsonArray = new JSONArray();

                foreach (BoardItemDataBase val in BoardItems)
                {
                    if(val.GetItemID().Equals(boardItemInfoSO.BoardItemTypeSO.GetID()))
                    {
                        jsonArray.Add(val.Serialize());
                    }
                }
                
                itemsJsonObj.Add(boardItemInfoSO.BoardItemTypeSO.GetID().ToString(), jsonArray);
            }

            jsonObj.Add(ITEMS_KEY, itemsJsonObj);
            
            return jsonObj;
        }

        public void Deserialize(JSONObject jsonObj)
        {
            BoardItems = new List<BoardItemDataBase>();
            
            JSONObject itemDataJsonObj = jsonObj.GetObject(ITEMS_KEY);

            foreach (KeyValuePair<string, JSONValue> kvp in itemDataJsonObj)
            {
                bool hasBoardItem = BoardItemSOContainer.Instance.TryGetBoardItemType(kvp.Key, out BoardItemTypeSOBase boardItemTypeSO);

                if (!hasBoardItem)
                {
                    continue;
                }
                
                BoardItemSOContainer.Instance.TryGetBoardItemInfoSO(boardItemTypeSO.GetID(),
                    out BoardItemInfoSO infoSO);

                JSONArray dataArray = kvp.Value.Array;
                
                if (dataArray == null)
                {
                    continue;
                }

                foreach (JSONValue val in dataArray)
                {
                    BoardItemDataBase boardItemData 
                        = BoardItemDataFactory.CreateBoardItemData(
                            infoSO.BoardItemTypeSO, 
                            val.Obj);
                    
                    if(boardItemData is IBaseBoardItemData baseBoardItemData)
                        baseBoardItemData.SetID(boardItemTypeSO.GetID());
                    
                    BoardItems.Add(boardItemData);
                }
            }
        }
    }
}