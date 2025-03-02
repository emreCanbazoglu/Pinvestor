using System;
using System.Collections.Generic;
using Boomlagoon.JSON;
using Cysharp.Threading.Tasks;
using MMFramework.PersistentObjectSystem;
using UnityEngine;

namespace Pinvestor.Game.GamePlayer
{
    public class GamePlayerMatchTrackerController : MonoBehaviour,
        IPersistentComponent
    {
        [SerializeField] private GamePlayerMatchTrackerBaseScriptableObject[] _trackers
            = Array.Empty<GamePlayerMatchTrackerBaseScriptableObject>();
        
        private List<GamePlayerMatchTrackerBaseSpec> _trackerSpecs
            = new List<GamePlayerMatchTrackerBaseSpec>();
        
        public GamePlayer GamePlayer { get; private set; }

        
        #region IPersistentComponent
        private PersistentObject _persistentObject;
        public PersistentObject PersistentObject
        {
            get
            {
                if (_persistentObject == null)
                    _persistentObject = GetComponent<PersistentObject>();

                return _persistentObject;

            }
            set => _persistentObject = value;
        }
        
        public Action<IPersistentComponent, bool> OnUpdate { get; set; }
        public Action<IPersistentComponent> OnReset { get; set; }

        #endregion

        public void Initialize(
            GamePlayer gamePlayer)
        {
            GamePlayer = gamePlayer;
            
            InitTrackers();
        }
        
        private void OnDestroy()
        {
            foreach (GamePlayerMatchTrackerBaseSpec trackerSpec in _trackerSpecs)
                trackerSpec.Dispose();
        }

        private void InitTrackers()
        {
            /*GameFSM gameFsm
                = GameFSMController.Instance.GetFSM<GameFSM>();
            
            foreach (GamePlayerMatchTrackerBaseScriptableObject tracker in _trackers)
            {
                GamePlayerMatchTrackerBaseSpec trackerSpec 
                    = tracker.CreateSpec(GamePlayer, gameFsm);
                
                _trackerSpecs.Add(trackerSpec);
                
                if (trackerSpec is IPersistentComponent persistentComponent)
                    persistentComponent.OnUpdate += OnTrackerUpdate;
            }*/
        }

        public bool TryGetTracker<T>(out T tracker) 
            where T : GamePlayerMatchTrackerBaseSpec
        {
            foreach (GamePlayerMatchTrackerBaseSpec trackerSpec in _trackerSpecs)
            {
                if (trackerSpec is T)
                {
                    tracker = (T)trackerSpec;
                    return true;
                }
            }

            tracker = null;
            return false;
        }
        
        #region IPersistentComponent

        private void OnTrackerUpdate(
            IPersistentComponent persistentComponent,
            bool setDirty)
        {
            OnUpdate?.Invoke(this, setDirty);
        }
        
        public JSONObject Serialize()
        {
            JSONObject jsonObj = new JSONObject();
            
            foreach (GamePlayerMatchTrackerBaseSpec trackerSpec in _trackerSpecs)
            {
                if (trackerSpec is IPersistentComponent persistentComponent)
                {
                    jsonObj.Add(
                        trackerSpec.Tracker.Key,
                        persistentComponent.Serialize());
                }
            }
            

            return jsonObj;
        }

        public async UniTask InitializeAsync(JSONObject data)
        {
            foreach (GamePlayerMatchTrackerBaseSpec trackerSpec in _trackerSpecs)
            {
                if (trackerSpec is IPersistentComponent persistentComponent)
                {
                    persistentComponent.PersistentObject = PersistentObject;

                    JSONObject trackerData = null;
                    
                    if(data != null)
                        trackerData = data.GetObject(trackerSpec.Tracker.Key);
                    
                    await persistentComponent.InitializeAsync(trackerData);
                }
            }
        }
        
        #endregion
    }
}