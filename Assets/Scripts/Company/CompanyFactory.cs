using UnityEngine;

namespace Pinvestor.CompanySystem
{
    public class CompanyFactory : Singleton<CompanyFactory>
    {
        [SerializeField] private CompanyContainerScriptableObject _companyContainerScriptableObject = null;
        
        public void TryCreateCompany(
            CompanyIdScriptableObject companyId,
            out Company company)
        {
            if (_companyContainerScriptableObject
                .TryGetCompany(companyId, out Company c))
                company = Instantiate(c);
            else
                company = null;
        }
    }
}