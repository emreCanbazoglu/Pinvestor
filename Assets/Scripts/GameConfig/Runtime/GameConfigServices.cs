using Pinvestor.CompanySystem;

namespace Pinvestor.GameConfigSystem
{
    public interface IGameConfigService
    {
    }

    public sealed class GameConfigServiceRegistry
    {
        private readonly System.Collections.Generic.Dictionary<System.Type, IGameConfigService> _services
            = new System.Collections.Generic.Dictionary<System.Type, IGameConfigService>();

        public void Register<TService>(TService service)
            where TService : class, IGameConfigService
        {
            if (service == null)
            {
                return;
            }

            _services[typeof(TService)] = service;
        }

        public bool TryGet<TService>(out TService service)
            where TService : class, IGameConfigService
        {
            if (_services.TryGetValue(typeof(TService), out IGameConfigService rawService))
            {
                service = rawService as TService;
                return service != null;
            }

            service = null;
            return false;
        }
    }

    public static class GameConfigServiceFactory
    {
        public static GameConfigServiceRegistry Create(
            GameConfigRootModel rootModel,
            GameConfigLookup lookup)
        {
            var registry = new GameConfigServiceRegistry();

            registry.Register(new CompanyConfigService(lookup));
            registry.Register(new BalanceConfigService(rootModel.Balance));
            registry.Register(new RoundCriteriaConfigService(rootModel.RoundCriteria));
            registry.Register(new RunCycleConfigService(rootModel.RunCycle));
            registry.Register(new BallConfigService(rootModel.Ball));
            registry.Register(new ShopConfigService(rootModel.Shop));

            return registry;
        }
    }

    public sealed class CompanyConfigService : IGameConfigService
    {
        private readonly GameConfigLookup _lookup;

        public CompanyConfigService(GameConfigLookup lookup)
        {
            _lookup = lookup;
        }

        public bool TryGetCompanyConfig(
            CompanyIdScriptableObject companyId,
            out CompanyConfigModel companyConfig)
        {
            companyConfig = null;
            if (companyId == null)
            {
                return false;
            }

            return TryGetCompanyConfig(companyId.CompanyId, out companyConfig);
        }

        public bool TryGetCompanyConfig(
            string companyId,
            out CompanyConfigModel companyConfig)
        {
            companyConfig = null;
            if (string.IsNullOrWhiteSpace(companyId))
            {
                return false;
            }

            return _lookup.TryGetCompany(companyId, out companyConfig);
        }

        public bool TryGetCompanyMaxHP(
            CompanyIdScriptableObject companyId,
            out float value)
        {
            value = 0f;
            if (!TryGetCompanyConfig(companyId, out CompanyConfigModel config))
            {
                return false;
            }

            return config.TryGetMaxHP(out value);
        }

        public bool TryGetCompanyRevenuePerHit(
            CompanyIdScriptableObject companyId,
            out float value)
        {
            value = 0f;
            if (!TryGetCompanyConfig(companyId, out CompanyConfigModel config))
            {
                return false;
            }

            return config.TryGetRevenuePerHit(out value);
        }

        public bool TryGetCompanyTurnlyCost(
            CompanyIdScriptableObject companyId,
            out float value)
        {
            value = 0f;
            if (!TryGetCompanyConfig(companyId, out CompanyConfigModel config))
            {
                return false;
            }

            return config.TryGetTurnlyCost(out value);
        }
    }

    public abstract class NamedSectionConfigServiceBase : IGameConfigService
    {
        public NamedConfigSectionModel Config { get; }

        protected NamedSectionConfigServiceBase(NamedConfigSectionModel config)
        {
            Config = config;
        }

        public bool TryGetValue(string key, out float value)
        {
            value = default(float);
            return Config != null && Config.TryGetValue(key, out value);
        }
    }

    public sealed class BalanceConfigService : NamedSectionConfigServiceBase
    {
        public BalanceConfigService(NamedConfigSectionModel config) : base(config) { }
    }

    public sealed class RoundCriteriaConfigService : NamedSectionConfigServiceBase
    {
        public RoundCriteriaConfigService(NamedConfigSectionModel config) : base(config) { }
    }

    public sealed class BallConfigService : IGameConfigService
    {
        public BallConfigModel Config { get; }

        public BallConfigService(BallConfigModel config)
        {
            Config = config;
        }
    }

    public sealed class RunCycleConfigService : IGameConfigService
    {
        public RunCycleConfigModel Config { get; }

        public RunCycleConfigService(RunCycleConfigModel config)
        {
            Config = config;
        }
    }

    public sealed class ShopConfigService : NamedSectionConfigServiceBase
    {
        public ShopConfigService(NamedConfigSectionModel config) : base(config) { }
    }
}
