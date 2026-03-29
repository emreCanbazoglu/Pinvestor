namespace Pinvestor.CardSystem
{
    public interface ICompanyCardDataSoProvider
    {
        CompanyCardDataScriptableObject GetCompanyCardData();
        void SetCompanyCardData(CompanyCardDataScriptableObject companyCardData);
    }
}