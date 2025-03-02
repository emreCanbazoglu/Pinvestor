using System;
using UnityEngine;

namespace Pinvestor.Game.GamePlayer
{
    public class MatchEndedEvent : IEvent
    {
        public GameMatchEndReason MatchEndReason { get; private set; }

        public MatchEndedEvent(
            GameMatchEndReason matchEndReason)
        {
            MatchEndReason = matchEndReason;
        }
    }
    
    public class GamePlayerMatchEndingController : MonoBehaviour
    {
        [SerializeField] private GamePlayerMatchEnderBaseScriptableObject[] _matchEnders
            = Array.Empty<GamePlayerMatchEnderBaseScriptableObject>();
        
        private GamePlayerMatchEnderBaseSpec[] _matchEnderSpecs
            = Array.Empty<GamePlayerMatchEnderBaseSpec>();
        
        public GamePlayer GamePlayer { get; private set; }

        public void Initialize(
            GamePlayer gamePlayer)
        {
            GamePlayer = gamePlayer;
            
            InitializeMatchEnders();
            
            RegisterToGameFSM();
        }
        
        private void OnDestroy()
        {
            UnregisterFromGameFSM();

            DisposeMatchEnders();
        }

        private void InitializeMatchEnders()
        {
            _matchEnderSpecs = new GamePlayerMatchEnderBaseSpec[_matchEnders.Length];
            
            for (int i = 0; i < _matchEnders.Length; i++)
                _matchEnderSpecs[i] 
                    = _matchEnders[i].CreateSpec(
                        GamePlayer,
                        this);
        }
        
        private void RegisterToGameFSM()
        {
            //StageSystem.StageManager.Instance.GameFSM.AddOnStateEntered(OnGameStateEntered);
            //StageSystem.StageManager.Instance.GameFSM.AddOnStateExited(OnGameStateExited);
        }
        
        private void UnregisterFromGameFSM()
        {
            /*if(StageSystem.StageManager.Instance == null)
                return;
            
            StageSystem.StageManager.Instance.GameFSM.RemoveOnStateEntered(OnGameStateEntered);
            StageSystem.StageManager.Instance.GameFSM.RemoveOnStateExited(OnGameStateExited);*/
        }
        
        private void DisposeMatchEnders()
        {
            foreach (GamePlayerMatchEnderBaseSpec ender in _matchEnderSpecs)
                ender.Dispose();
        }
        
        private void OnGameStateEntered(Enum state)
        {
            //if((GameFSM.EState)state == GameFSM.EState.Game)
                //OnGameEntered();
        }
        
        private void OnGameEntered()
        {
            RegisterToEnders();

            foreach (GamePlayerMatchEnderBaseSpec ender in _matchEnderSpecs)
            {
                ender.Reset();

                ender.MatchStarted();
            }
        }
        
        private void OnGameStateExited(Enum state)
        {
            //if((GameFSM.EState)state == GameFSM.EState.Game)
                //OnGameExited();
        }
        
        private void OnGameExited()
        {
            UnregisterFromEnders();
        }
        
        private void RegisterToEnders()
        {
            foreach (GamePlayerMatchEnderBaseSpec ender in _matchEnderSpecs)
                ender.OnMatchEndedForGamePlayer += MatchEndedForPlayerHandler;
        }

        private void UnregisterFromEnders()
        {
            foreach (GamePlayerMatchEnderBaseSpec ender in _matchEnderSpecs)
            {
                ender.OnMatchEndedForGamePlayer -= MatchEndedForPlayerHandler;
            }
        }
        
        private void MatchEndedForPlayerHandler(
            GamePlayerMatchEnderBaseSpec ender, 
            GameMatchEndReason matchEndReason)
        {
            UnregisterFromEnders();
            MatchEndedForPlayer(matchEndReason);
            
            Debug.Log("Match ended for player with reason: " + matchEndReason);
        }
        
        private void MatchEndedForPlayer(
            GameMatchEndReason matchEndReason)
        {
            foreach (GamePlayerMatchEnderBaseSpec ender in _matchEnderSpecs)
                ender.MatchEnded();
            
            EventBus<MatchEndedEvent>.Raise(
                new MatchEndedEvent(matchEndReason));
        }
    }
}