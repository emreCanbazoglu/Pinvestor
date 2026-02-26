using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pinvestor.GameConfigSystem
{
    public sealed class GameConfigManager : Singleton<GameConfigManager>
    {
        [SerializeField] private string _resourcesPath = "GameConfig/game-config";
        [SerializeField] private bool _raiseEvents = true;

        private readonly GameConfigLoader _loader = new GameConfigLoader();
        private readonly GameConfigLookup _lookup = new GameConfigLookup();

        public bool IsInitialized { get; private set; }
        public string LastLoadError { get; private set; } = string.Empty;
        public GameConfigRootModel RootModel { get; private set; }
        private GameConfigServiceRegistry ServiceRegistry { get; set; }

        public async UniTask<bool> InitializeAsync()
        {
            if (IsInitialized)
            {
                return true;
            }

            await UniTask.SwitchToMainThread();

            if (!_loader.TryLoadFromResources(_resourcesPath, out GameConfigRootJsonDto dto, out string loadError))
            {
                return Fail(loadError);
            }

            GameConfigRootModel rootModel = GameConfigMapper.Map(dto);
            if (!_lookup.TryBuild(rootModel, out string lookupError))
            {
                return Fail(lookupError);
            }

            RootModel = rootModel;
            ServiceRegistry = GameConfigServiceFactory.Create(RootModel, _lookup);
            LastLoadError = string.Empty;
            IsInitialized = true;

            Debug.Log($"GameConfig loaded. Schema={RootModel.SchemaVersion}, Companies={RootModel.Companies.Count}");
            if (_raiseEvents)
            {
                EventBus<GameConfigLoadedEvent>.Raise(new GameConfigLoadedEvent(RootModel.SchemaVersion));
            }

            return true;
        }

        public bool TryGetService<TService>(out TService service)
            where TService : class, IGameConfigService
        {
            service = null;
            if (!IsInitialized || ServiceRegistry == null)
            {
                return false;
            }

            return ServiceRegistry.TryGet(out service);
        }

        private bool Fail(string error)
        {
            LastLoadError = error ?? "Unknown GameConfig load error.";
            IsInitialized = false;
            RootModel = null;
            ServiceRegistry = null;

            Debug.LogError($"GameConfig load failed: {LastLoadError}");
            if (_raiseEvents)
            {
                EventBus<GameConfigLoadFailedEvent>.Raise(new GameConfigLoadFailedEvent(LastLoadError));
            }

            return false;
        }
    }
}
