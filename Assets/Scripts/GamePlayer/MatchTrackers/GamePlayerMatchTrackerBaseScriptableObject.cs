using System;
using UnityEngine;

namespace Pinvestor.Game.GamePlayer
{
    public abstract class GamePlayerMatchTrackerBaseScriptableObject : ScriptableObject
    {
        [field: SerializeField] public string Key { get; private set; }
        
        public abstract GamePlayerMatchTrackerBaseSpec CreateSpec(
            GamePlayer player,
            GameFSM gameFsm);

    }

    public abstract class GamePlayerMatchTrackerBaseSpec : IDisposable
    {
        public GamePlayer Player { get; private set; }
        public GamePlayerMatchTrackerBaseScriptableObject Tracker { get; private set; }

        protected GameFSM GameFsm { get; private set; }

        protected GamePlayerMatchTrackerBaseSpec(
            GamePlayerMatchTrackerBaseScriptableObject tracker,
            GamePlayer player,
            GameFSM gameFsm)
        {
            Tracker = tracker;
            Player = player;
            
            GameFsm = gameFsm;
            
            GameFsm.AddOnStateEntered(OnGameStateEntered);
            GameFsm.AddOnStateExited(OnGameStateExited);
        }
        
        public virtual void Reset()
        {
            
        }
        
        private void OnGameStateEntered(Enum state)
        {
            if((GameFSM.EState)state == GameFSM.EState.PreGame)
                Reset();
            
            OnGameStateEnteredCore(state);
        }

        protected virtual void OnGameStateEnteredCore(Enum state)
        {
            
        }
        
        private void OnGameStateExited(Enum state)
        {
            OnGameStateExitedCore(state);
        }
        
        protected virtual void OnGameStateExitedCore(Enum state)
        {
            
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO release managed resources here
            }
        }

        public void Dispose()
        {
            GameFsm?.RemoveOnStateEntered(OnGameStateEntered);
            GameFsm?.RemoveOnStateExited(OnGameStateExited);
            

            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}