using System;
using UnityEngine;

namespace Pinvestor.CompanySystem
{
    [CreateAssetMenu(
        menuName = "Pinvestor/Company System/Company Container",
        fileName = "CompanyContainer")]
    public class CompanyContainerScriptableObject : ScriptableObject
    {
        [SerializeField] private Company[] _companies 
            = Array.Empty<Company>();

        public bool TryGetCompany(
            CompanyIdScriptableObject companyId,
            out Company company)
        {
            foreach (Company c in _companies)
            {
                if (c.CompanyId == companyId)
                {
                    company = c;
                    return true;
                }
            }

            company = null;
            return false;
        }
    }
}