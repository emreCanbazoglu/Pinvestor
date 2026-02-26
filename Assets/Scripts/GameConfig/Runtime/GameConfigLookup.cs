using System.Collections.Generic;

namespace Pinvestor.GameConfigSystem
{
    public sealed class GameConfigLookup
    {
        private readonly Dictionary<string, CompanyConfigModel> _companyById
            = new Dictionary<string, CompanyConfigModel>();

        public bool TryBuild(
            GameConfigRootModel rootModel,
            out string error)
        {
            _companyById.Clear();

            if (rootModel == null)
            {
                error = "GameConfig root model is null.";
                return false;
            }

            foreach (CompanyConfigModel company in rootModel.Companies)
            {
                if (company == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(company.CompanyId))
                {
                    error = "Company config contains empty companyId.";
                    return false;
                }

                if (!_companyById.TryAdd(company.CompanyId, company))
                {
                    error = $"Duplicate company config id detected: {company.CompanyId}";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public bool TryGetCompany(
            string companyId,
            out CompanyConfigModel companyConfig)
        {
            return _companyById.TryGetValue(companyId, out companyConfig);
        }
    }
}
