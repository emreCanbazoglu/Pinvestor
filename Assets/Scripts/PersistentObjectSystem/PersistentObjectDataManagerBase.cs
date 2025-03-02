using System;
using System.Collections.Generic;
using System.Linq;
using Boomlagoon.JSON;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MMFramework.PersistentObjectSystem
{
    public abstract class PersistentObjectDataManagerBase : MonoBehaviour
    {
        private readonly List<PersistentObject> _dirtyPersistentObjects
            = new List<PersistentObject>();

        public abstract UniTask WaitUntilInitialize();

        public void SetPersistentObjectDirty(
            PersistentObject persistentObject)
        {
            if(_dirtyPersistentObjects.Contains(persistentObject))
                return;
            
            _dirtyPersistentObjects.Add(persistentObject);
            
            SavePersistentObjectsAsync().Forget();
            
            _dirtyPersistentObjects.Clear();
        }
        
        public abstract void DeletePersistentObject(
            Guid guid);

        private async UniTask SavePersistentObjectsAsync()
        {
            await SavePersistentObjectsCoreAsync(
                _dirtyPersistentObjects.ToList());
        }
        
        protected abstract UniTask SavePersistentObjectsCoreAsync(
            List<PersistentObject> persistentObjects);
        
        public async UniTask<JSONObject> LoadPersistentObjectsAsync(
            Guid guid)
        {
            await WaitUntilInitialize();
            
            return await LoadPersistentObjectsCoreAsync(
                guid);
        }
        
        protected abstract UniTask<JSONObject> LoadPersistentObjectsCoreAsync(
            Guid guid);
        
    }
}