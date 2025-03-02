using Cysharp.Threading.Tasks;
using Pinvestor.CardSystem;
using UnityEngine;

namespace Pinvestor.Game.GamePlayer
{
    public class GamePlayer : MonoBehaviour
    {
        [field: SerializeField] public CardPlayer CardPlayer { get; private set; } = null;

        private GamePlayerMatchTrackerController _matchTrackerController;
        public GamePlayerMatchTrackerController MatchTrackerController
        {
            get
            {
                if(_matchTrackerController == null)
                    _matchTrackerController 
                        = GetComponent<GamePlayerMatchTrackerController>();
                
                return _matchTrackerController;
            }
        }
        
        private GamePlayerMatchEndingController _matchEndingController;
        public GamePlayerMatchEndingController MatchEndingController
        {
            get
            {
                if(_matchEndingController == null)
                    _matchEndingController 
                        = GetComponent<GamePlayerMatchEndingController>();
                
                return _matchEndingController;
            }
        }


        public async UniTask Initialize(
            IDeckDataProvider deckDataProvider)
        {
            MatchTrackerController.Initialize(this);
            MatchEndingController.Initialize(this);
            
            await CardPlayer.InitializeAsync(deckDataProvider);
        }
        
        public async UniTask WaitUntilInitialized()
        {
            await CardPlayer.WaitUntilDeckInitialized();
        }
    }
}
