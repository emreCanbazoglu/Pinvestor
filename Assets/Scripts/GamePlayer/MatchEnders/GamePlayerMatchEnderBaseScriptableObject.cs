using System;
using UnityEngine;

namespace Pinvestor.Game.GamePlayer
{
    public abstract class GamePlayerMatchEnderBaseScriptableObject : ScriptableObject
    {
        [field: SerializeField] public GameMatchEndReason MatchEndReason { get; private set; } = null;

        public abstract GamePlayerMatchEnderBaseSpec CreateSpec(
            GamePlayer player,
            GamePlayerMatchEndingController controller);
    }
    
    public abstract class GamePlayerMatchEnderBaseSpec : IDisposable
    {
        public GamePlayer Player { get; private set; }
        public GamePlayerMatchEndingController Controller { get; private set; }
        public GamePlayerMatchEnderBaseScriptableObject Ender { get; private set; }

        public Action<GamePlayerMatchEnderBaseSpec, GameMatchEndReason> OnMatchEndedForGamePlayer { get; set; }
        
        public GamePlayerMatchEnderBaseSpec(
            GamePlayerMatchEnderBaseScriptableObject ender,
            GamePlayer player,
            GamePlayerMatchEndingController controller)
        {
            Ender = ender;
            Player = player;
            Controller = controller;
        }
        
        public abstract void MatchStarted();

        public virtual void MatchEnded() { }
        
        public virtual void GameExited() { }

        protected void Ended()
        {
            OnMatchEndedForGamePlayer?.Invoke(this, Ender.MatchEndReason);
        }
        
        public virtual void Reset()
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

            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
