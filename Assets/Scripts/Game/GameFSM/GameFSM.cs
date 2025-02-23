using System;
using System.Collections.Generic;

namespace Pinvestor.Game
{
    public class GameFSM : MMFSM
    {
        public enum EState
        {
            None = 0,
            Idle = 10,
            PreGame = 20,
            Game = 30,
            Win = 41,
            GameOver = 50,
        }
        
        
        protected override Dictionary<StateTransition, Enum> GetTransitionDict()
        {

            return new Dictionary<StateTransition, Enum>() { };
        }

        protected override Enum GetEnterenceStateID()
        {
            return EState.Idle;
        }
    }
}
