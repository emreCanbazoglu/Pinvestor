using Cysharp.Threading.Tasks;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;

namespace Pinvestor.Game
{
    public class Table
    {
        public Board Board { get; private set; }
        public GamePlayer.GamePlayer GamePlayer { get; private set; }
        
        public Table(
            Board board,
            GamePlayer.GamePlayer gamePlayer,
            IDeckDataProvider deckDataProvider)
        {
            Board = board;
            GamePlayer = gamePlayer;
            
            GamePlayer.Initialize(deckDataProvider).Forget();
        }

        public async UniTask WaitUntilInitialized()
        {
            await GamePlayer.WaitUntilInitialized();
        }
    }
}
