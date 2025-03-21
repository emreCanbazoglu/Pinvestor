using System;
using Boomlagoon.JSON;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItemData_Company : BoardItemDataBase
    {
        public string RefCardId { get; set; }
        public int Health { get; set; }
        
        //TODO: Add attached card data
        //TODO: Add company bonuses

        public BoardItemData_Company(
            string refCardId,
            int col = -1,
            int row = -1,
            int health = 0) : base(col, row)
        {
            RefCardId = refCardId;
            Health = health;
        }
        
        public BoardItemData_Company(BoardItemData_Company data) 
            : base(data)
        {
            RefCardId = data.RefCardId;
            Health = data.Health;
        }
        
        public override Enum GetItemID()
        {
            return EBoardItem.Company;
        }

        #region Serialization

        private const string REF_CARD_ID = "RefCardId";
        private const string HEALTH_KEY = "Health";
        
        protected override void SerializeCustomActions(JSONObject jsonObj)
        {
            jsonObj.Add(REF_CARD_ID, RefCardId);
            jsonObj.Add(HEALTH_KEY, Health);

            base.SerializeCustomActions(jsonObj);
        }
        
        protected override void DeserializeCustomActions(JSONObject jsonObj)
        {
            RefCardId = jsonObj.GetString(REF_CARD_ID);
            Health = (int)jsonObj.GetNumber(HEALTH_KEY);

            base.DeserializeCustomActions(jsonObj);
        }
        

        #endregion
    }
}