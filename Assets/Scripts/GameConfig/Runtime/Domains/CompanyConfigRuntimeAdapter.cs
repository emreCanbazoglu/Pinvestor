using Pinvestor.CompanySystem;

namespace Pinvestor.GameConfigSystem
{
    public sealed class CompanyConfigRuntimeAdapter
    {
        private readonly GameConfigManager _gameConfigManager;

        public CompanyConfigRuntimeAdapter(GameConfigManager gameConfigManager)
        {
            _gameConfigManager = gameConfigManager;
        }

        public bool TryGetCompanyConfig(
            CompanyIdScriptableObject companyId,
            out CompanyConfigModel companyConfig)
        {
            companyConfig = null;
            if (_gameConfigManager == null)
            {
                return false;
            }

            if (!_gameConfigManager.TryGetService<CompanyConfigService>(out CompanyConfigService service))
            {
                return false;
            }

            return service.TryGetCompanyConfig(companyId, out companyConfig);
        }
    }
}
