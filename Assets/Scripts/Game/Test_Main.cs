using Cysharp.Threading.Tasks;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using UnityEngine;

namespace Pinvestor.Game
{
    public class Test_Main : MonoBehaviour
    {
        [SerializeField] private GamePlayer.GamePlayer _gamePlayer = null;
        [SerializeField] private SerializedDeckDataProvider _serializedDeckDataProvider = null;
        
        [SerializeField] private Vector2Int _boardSize = new Vector2Int(5, 5);
        
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
                _serializedDeckDataProvider);
            
            await _table.WaitUntilInitialized();
            
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
            
            Turn turn = new Turn(_table.GamePlayer.CardPlayer);
            
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
