using Boomlagoon.JSON;
using SerializableData;
using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    public class BoardCellData : ISerializableData
    {
        #region Constants
        
        private const string COL_KEY = "c";
        private const string ROW_KEY = "r"; 

        #endregion
        
        public Vector2 CellLocation { get; private set; }
        
        public BoardCellData(JSONObject jsonObj)
        {
            Deserialize(jsonObj);
        }

        public JSONObject Serialize()
        {
            JSONObject jsonObj = new JSONObject();

            jsonObj.Add(COL_KEY, CellLocation.x);
            jsonObj.Add(ROW_KEY, CellLocation.y);

            return jsonObj;       
        }

        public void Deserialize(JSONObject jsonObj)
        {
            CellLocation = new Vector2
            {
                x = (int) jsonObj.GetNumber(COL_KEY),
                y = (int) jsonObj.GetNumber(ROW_KEY)
            };
        }
    }
}