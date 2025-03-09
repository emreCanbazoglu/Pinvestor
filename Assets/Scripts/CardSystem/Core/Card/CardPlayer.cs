using AbilitySystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pinvestor.CardSystem
{
    public class CardPlayer : MonoBehaviour
    {
        [field: SerializeField] public AbilitySystemCharacter AbilitySystemCharacter { get; private set; } = null;
        
        public Deck Deck { get; private set; } = null;
        private bool _isInitialized = false;

        public async UniTask InitializeAsync(
            IDeckDataProvider deckDataProvider)
        {
            await deckDataProvider.WaitUntilInitialized();
            
            Deck = new Deck(this, deckDataProvider);
            
            _isInitialized = true;
        }
        
        public async UniTask WaitUntilDeckInitialized()
        {
            await UniTask.WaitUntil(() => _isInitialized);
        }

    }
}
