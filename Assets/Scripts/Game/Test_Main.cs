using System;
using Cysharp.Threading.Tasks;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using Pinvestor.CardSystem.Authoring;
using UnityEngine;

namespace Pinvestor.Game
{
    public class Test_Main : MonoBehaviour
    {
        [SerializeField] private GamePlayer.GamePlayer _gamePlayer = null;
        [SerializeField] private SerializedDeckDataProvider _serializedDeckDataProvider = null;
        
        [SerializeField] private Vector2Int _boardSize = new Vector2Int(5, 5);
        
        [SerializeField] private BoardWrapper _boardWrapper = null;
        [SerializeField] private CompanySelectionPileWrapper _companySelectionPileWrapper = null;
        
        [SerializeField] private CellLayerInfoSO[] _cellLayerInfoColl 
            = Array.Empty<CellLayerInfoSO>();
        
        private Table _table;

        [Header("Test Data")]
        [SerializeField] private int _selectedCompanyCardIndex;
        
        private EventBinding<CompanySelectionRequestEvent> _companySelectionRequestEventBinding;

        private CompanySelectionRequestEvent _companySelectionRequestEvent;
        
        private void Awake()
        {
            _companySelectionRequestEventBinding
                = new EventBinding<CompanySelectionRequestEvent>(OnCompanySelectionRequest);
            
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            _table = new Table(
                GetBoardData(),
                _gamePlayer,
                _serializedDeckDataProvider,
                _cellLayerInfoColl);
            
            await _table.WaitUntilInitialized();
            
            _boardWrapper.WrapBoard(_table.Board);
            _companySelectionPileWrapper.WrapPile(
                _table.GamePlayer.CardPlayer.Deck
                    .TryGetDeckPile(EDeckPile.CompanySelection, out var pile)
                        ? pile as CompanySelectionPile
                        : null);
            
            Debug.Log("Table initialized");

            PlayAsync().Forget();
        }
        
        private BoardData GetBoardData()
        {
            return new BoardData(_boardSize);
        }

        private async UniTask PlayAsync()
        {
            Debug.Log("Playing...");
            
            Turn turn = new Turn(_table.GamePlayer.CardPlayer, null);
            
            EventBus<CompanySelectionRequestEvent>
                .Register(_companySelectionRequestEventBinding);
            
            await turn.StartAsync();
        }
        
         private void OnCompanySelectionRequest(
            CompanySelectionRequestEvent e)
        {
            _companySelectionRequestEvent = e;
            
            EventBus<CompanySelectionRequestEvent>
                .Deregister(_companySelectionRequestEventBinding);
            
        }
         
        [ContextMenu("Select Company Card")]
        public void SelectCompanyCard()
        {
            if(_companySelectionRequestEvent == null)
                return;
            
            _companySelectionRequestEvent.OnCompanyCardSelected(
                _companySelectionRequestEvent.CompanyCards[_selectedCompanyCardIndex]);
        }
    }
}
