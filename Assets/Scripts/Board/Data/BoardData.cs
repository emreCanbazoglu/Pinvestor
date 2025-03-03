using System;
using System.Collections.Generic;
using Boomlagoon.JSON;
using SerializableData;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public interface IBaseBoardItemData
    {
        Enum GetItemID();
        void SetID(Enum type);
    }
    
    public class BoardData : ISerializableData
    {
        #region Constants
        
        private const string DIMENSIONS_KEY = "Dimensions";
        private const string ITEMS_KEY = "Items";

        #endregion
        
        public Vector2Int Dimensions { get; private set; }
        public List<BoardItemDataBase> BoardItems { get; private set; }


        public BoardData(
            Vector2Int dimensions)
        {
            Dimensions = dimensions;
            BoardItems = new List<BoardItemDataBase>();
        }

        public BoardData(
            Vector2Int dimensions,
            List<BoardItemDataBase> boardItems)
        {
            Dimensions = dimensions;
            BoardItems = new List<BoardItemDataBase>(boardItems);
        }
        
        public BoardData(JSONObject jsonObj)
        {
            Deserialize(jsonObj);
        }

        public JSONObject Serialize()
        {
            JSONObject jsonObj = new JSONObject();
            
            jsonObj.Add(DIMENSIONS_KEY, new JSONObject
            {
                { "x", Dimensions.x },
                { "y", Dimensions.y }
            });

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
            JSONObject dimensionsJsonObj = jsonObj.GetObject(DIMENSIONS_KEY);

            Dimensions = new Vector2Int(
                (int)dimensionsJsonObj.GetNumber("x"),
                (int)dimensionsJsonObj.GetNumber("y"));
            
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