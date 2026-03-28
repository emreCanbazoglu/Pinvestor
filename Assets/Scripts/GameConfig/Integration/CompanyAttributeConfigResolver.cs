using Pinvestor.CompanySystem;
using UnityEngine;

namespace Pinvestor.GameConfigSystem
{
    public sealed class CompanyAttributeConfigResolver
    {
        private readonly GameConfigManager _gameConfigManager;

        public CompanyAttributeConfigResolver(GameConfigManager gameConfigManager)
        {
            _gameConfigManager = gameConfigManager;
        }

        public bool TryResolveCompanyConfig(
            CompanyIdScriptableObject companyId,
            out CompanyConfigModel companyConfig)
        {
            companyConfig = null;
            if (companyId == null)
            {
                Debug.LogError("CompanyAttributeConfigResolver: CompanyId is null.");
                return false;
            }

            return TryResolveCompanyConfig(companyId.CompanyId, out companyConfig);
        }

        public bool TryResolveCompanyConfig(
            string companyId,
            out CompanyConfigModel companyConfig)
        {
            companyConfig = null;
            if (_gameConfigManager == null)
            {
                Debug.LogError("CompanyAttributeConfigResolver: GameConfigManager is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(companyId))
            {
                Debug.LogError("CompanyAttributeConfigResolver: CompanyId is null or empty.");
                return false;
            }

            if (!_gameConfigManager.IsInitialized)
            {
                Debug.LogError("CompanyAttributeConfigResolver: GameConfigManager is not initialized.");
                return false;
            }

            if (!_gameConfigManager.TryGetService<CompanyConfigService>(out CompanyConfigService service))
            {
                Debug.LogError("CompanyAttributeConfigResolver: CompanyConfigService is unavailable.");
                return false;
            }

            if (service.TryGetCompanyConfig(companyId, out companyConfig))
            {
                return true;
            }

            Debug.LogError($"CompanyAttributeConfigResolver: Missing config for companyId '{companyId}'.");
            return false;
        }
    }
}
