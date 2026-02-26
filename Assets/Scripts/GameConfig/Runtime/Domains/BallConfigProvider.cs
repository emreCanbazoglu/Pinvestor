namespace Pinvestor.GameConfigSystem
{
    public sealed class BallConfigProvider
    {
        private readonly GameConfigManager _gameConfigManager;

        public BallConfigProvider(GameConfigManager gameConfigManager)
        {
            _gameConfigManager = gameConfigManager;
        }

        public NamedConfigSectionModel GetConfig()
        {
            if (_gameConfigManager == null)
            {
                return null;
            }

            if (_gameConfigManager.TryGetService<BallConfigService>(out BallConfigService service))
            {
                return service.Config;
            }

            return null;
        }
    }
}
