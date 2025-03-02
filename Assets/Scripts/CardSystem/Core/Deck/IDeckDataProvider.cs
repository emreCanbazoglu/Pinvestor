using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Pinvestor.CardSystem
{
    public interface IDeckDataProvider
    {
        UniTask WaitUntilInitialized();
        IReadOnlyList<CardData> GetCardData();
        IReadOnlyList<DeckPileData> GetPileData();
    }
}