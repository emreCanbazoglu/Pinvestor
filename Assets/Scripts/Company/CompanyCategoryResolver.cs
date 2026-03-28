using Pinvestor.GameConfigSystem;

namespace Pinvestor.CompanySystem
{
    public static class CompanyCategoryResolver
    {
        public static bool TryResolve(
            string companyId,
            out ECompanyCategory category)
        {
            if (string.IsNullOrWhiteSpace(companyId))
            {
                category = ECompanyCategory.None;
                return false;
            }

            if (GameConfigManager.Instance != null
                && GameConfigManager.Instance.IsInitialized
                && GameConfigManager.Instance.TryGetService(out CompanyConfigService companyConfigService)
                && companyConfigService.TryGetCompanyConfig(companyId, out CompanyConfigModel companyConfig)
                && companyConfig.TryGetCompanyCategory(out category))
            {
                return true;
            }

            category = ECompanyCategory.None;
            return false;
        }

        public static ECompanyCategory ResolveOrNone(
            string companyId)
        {
            return TryResolve(companyId, out ECompanyCategory category)
                ? category
                : ECompanyCategory.None;
        }
    }
}
