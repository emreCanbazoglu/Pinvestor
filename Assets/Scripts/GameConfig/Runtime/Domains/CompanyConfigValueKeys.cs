namespace Pinvestor.GameConfigSystem
{
    public static class CompanyConfigValueKeys
    {
        public const string CompanyCategory = "companyCategory";

        /// <summary>
        /// One-time acquisition cost debited from Balance when the company is placed.
        /// Also the base of the cashout value. Distinct from the per-turn TurnlyCost attribute.
        /// </summary>
        public const string PurchaseCost = "purchaseCost";
    }
}
