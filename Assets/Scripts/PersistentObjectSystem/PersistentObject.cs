using Boomlagoon.JSON;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace MMFramework.PersistentObjectSystem
{
    [RequireComponent(typeof(GuidComponent))]
    public class PersistentObject : MonoBehaviour
    {
        private GuidComponent _guidComponent;

        public GuidComponent GuidComponent
        {
            get
            {
                if (_guidComponent == null)
                    _guidComponent = GetComponent<GuidComponent>();

                return _guidComponent;
            }
        }

        private IPersistentComponent _persistentComponent;

        public IPersistentComponent PersistentComponent
        {
            get
            {
                if (_persistentComponent == null)
                    _persistentComponent = GetComponent<IPersistentComponent>();

                return _persistentComponent;
            }
        }
        
        public bool IsInitialized { get; private set; }

        private bool _anyDirtyComponent;
        
        private void Awake()
        {
            RegisterToPersistentComponents();
            
            PersistentObjectManager.Instance.InvokeOnAwake(
                this);
        }
        
        private void OnDestroy()
        {
            UnregisterFromPersistentComponents();
        }

        private void RegisterToPersistentComponents()
        {
            PersistentComponent.OnUpdate += OnPersistentComponentUpdate;
            PersistentComponent.OnReset += OnPersistentComponentReset;
        }
        
        private void UnregisterFromPersistentComponents()
        {
            PersistentComponent.OnUpdate -= OnPersistentComponentUpdate;
            PersistentComponent.OnReset -= OnPersistentComponentReset;
        }

        private void OnPersistentComponentUpdate(
            IPersistentComponent persistentComponent,
            bool setDirty)
        {
            if (setDirty)
                PersistentObjectManager.Instance.DataManager
                    .SetPersistentObjectDirty(this);
            else
                _anyDirtyComponent = true;
        }
        
        private void OnPersistentComponentReset(
            IPersistentComponent persistentComponent)
        {
            ResetPersistentObject();
        }
        
        public void ForceSetDirty()
        {
            if(!_anyDirtyComponent)
                return;
            
            PersistentObjectManager.Instance.DataManager
                .SetPersistentObjectDirty(this);

            _anyDirtyComponent = false;
        }
        
        public async UniTask InitializeAsync(
            JSONObject persistentData)
        {
            await PersistentComponent.InitializeAsync(
                persistentData);

            IsInitialized = true;
        }
        
        public async UniTask WaitUntilInitializeAsync()
        {
            if (IsInitialized)
                return;

            await UniTask.WaitUntil(() => IsInitialized);
        }

        private void ResetPersistentObject()
        {
            PersistentObjectManager.Instance.DataManager
                .DeletePersistentObject(GuidComponent.GetGuid());

            _anyDirtyComponent = false;
            IsInitialized = false;
        }
    }
}
