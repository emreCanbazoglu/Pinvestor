using Pinvestor.BoardSystem.Authoring;
using UnityEngine;

namespace Pinvestor.CompanySystem
{
    public class Company : MonoBehaviour,
        IComponentProvider<BoardItemWrapper_Company>
    {
        [field: SerializeField] public CompanyIdScriptableObject CompanyId { get; private set; } = null;

        private BoardItemWrapper_Company _boardItemWrapper = null;
        
        public void SetBoardItemWrapper(
            BoardItemWrapper_Company boardItemWrapper)
        {
            _boardItemWrapper = boardItemWrapper;
        }
        
        public BoardItemWrapper_Company GetComponent()
        {
            return _boardItemWrapper;
        }
    }
}