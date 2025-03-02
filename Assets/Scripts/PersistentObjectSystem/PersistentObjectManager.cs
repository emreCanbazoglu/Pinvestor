using Boomlagoon.JSON;
using Cysharp.Threading.Tasks;

namespace MMFramework.PersistentObjectSystem
{
    public class PersistentObjectManager : Singleton<PersistentObjectManager>
    {
        private PersistentObjectDataManagerBase _dataManager;

        public PersistentObjectDataManagerBase DataManager
        {
            get
            {
                if (_dataManager == null)
                    _dataManager = GetComponent<PersistentObjectDataManagerBase>();

                return _dataManager;
            }
        }
        
        public void InvokeOnAwake(
            PersistentObject persistentObject)
        {
            InitializePersistentObjectAsync(persistentObject).Forget();
        }
        
        private async UniTask InitializePersistentObjectAsync(
            PersistentObject persistentObject)
        {
            JSONObject persistentData
                = await DataManager
                    .LoadPersistentObjectsAsync(
                        persistentObject.GuidComponent.GetGuid());

            await persistentObject.InitializeAsync(
                persistentData);
        }
    }
}