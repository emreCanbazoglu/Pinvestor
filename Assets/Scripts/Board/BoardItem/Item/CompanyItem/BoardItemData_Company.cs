using System;
using Boomlagoon.JSON;
using Pinvestor.CompanySystem;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItemData_Company : BoardItemDataBase
    {
        public string CompanyName { get; set; }
        public ECompanyCategory CompanyCategory { get; set; }
        public float Valuation { get; set; }
        public float RunlyCost { get; set; }
        public float RevenuePerHit { get; set; }
        public int MaxHealth { get; set; }
        public int Health { get; set; }
        
        //TODO: Add attached card data
        //TODO: Add company bonuses

        public BoardItemData_Company(
            int col,
            int row,
            string companyName,
            ECompanyCategory companyCategory,
            float valuation,
            float runlyCost,
            float revenuePerHit,
            int maxHealth,
            int health) : base(col, row)
        {
            CompanyName = companyName;
            CompanyCategory = companyCategory;
            Valuation = valuation;
            RunlyCost = runlyCost;
            RevenuePerHit = revenuePerHit;
            MaxHealth = maxHealth;
            Health = health;
        }
        
        public BoardItemData_Company(BoardItemData_Company data) 
            : base(data)
        {
            CompanyName = data.CompanyName;
            CompanyCategory = data.CompanyCategory;
            Valuation = data.Valuation;
            RunlyCost = data.RunlyCost;
            RevenuePerHit = data.RevenuePerHit;
            MaxHealth = data.MaxHealth;
            Health = data.Health;
        }
        
        public override Enum GetItemID()
        {
            return EBoardItem.Company;
        }

        #region Serialization

        private const string COMPANY_NAME_KEY = "CompanyName";
        private const string COMPANY_CATEGORY_KEY = "CompanyCategory";
        private const string VALUATION_KEY = "Valuation";
        private const string RUNLY_COST_KEY = "RunlyCost";
        private const string REVENUE_PER_HIT_KEY = "RevenuePerHit";
        private const string MAX_HEALTH_KEY = "MaxHealth";
        private const string HEALTH_KEY = "Health";
        
        protected override void SerializeCustomActions(JSONObject jsonObj)
        {
            jsonObj.Add(COMPANY_NAME_KEY, CompanyName);
            jsonObj.Add(COMPANY_CATEGORY_KEY, (int)CompanyCategory);
            jsonObj.Add(VALUATION_KEY, Valuation);
            jsonObj.Add(RUNLY_COST_KEY, RunlyCost);
            jsonObj.Add(REVENUE_PER_HIT_KEY, RevenuePerHit);
            jsonObj.Add(MAX_HEALTH_KEY, MaxHealth);
            jsonObj.Add(HEALTH_KEY, Health);

            base.SerializeCustomActions(jsonObj);
        }
        
        protected override void DeserializeCustomActions(JSONObject jsonObj)
        {
            CompanyName = jsonObj.GetString(COMPANY_NAME_KEY);
            CompanyCategory = (ECompanyCategory)jsonObj.GetNumber(COMPANY_CATEGORY_KEY);
            Valuation = (float)jsonObj.GetNumber(VALUATION_KEY);
            RunlyCost = (float)jsonObj.GetNumber(RUNLY_COST_KEY);
            RevenuePerHit = (float)jsonObj.GetNumber(REVENUE_PER_HIT_KEY);
            MaxHealth = (int)jsonObj.GetNumber(MAX_HEALTH_KEY);
            Health = (int)jsonObj.GetNumber(HEALTH_KEY);

            base.DeserializeCustomActions(jsonObj);
        }
        

        #endregion
    }
}