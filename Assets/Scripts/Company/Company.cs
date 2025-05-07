using Pinvestor.BoardSystem.Authoring;
using Pinvestor.Game.BallSystem;
using UnityEngine;

namespace Pinvestor.CompanySystem
{
    public class Company : MonoBehaviour,
        IComponentProvider<BoardItemWrapper_Company>
    {
        [field: SerializeField] public CompanyIdScriptableObject CompanyId { get; private set; } = null;

        [SerializeField] private BallTarget _ballTarget = null;
        
        private BoardItemWrapper_Company _boardItemWrapper = null;

        private void OnEnable()
        {
            _ballTarget.OnBallCollided += OnBallCollided;
        }

        private void OnDisable()
        {
            _ballTarget.OnBallCollided -= OnBallCollided;
        }

        public void SetBoardItemWrapper(
            BoardItemWrapper_Company boardItemWrapper)
        {
            _boardItemWrapper = boardItemWrapper;
        }
        
        private void OnBallCollided(Ball ball)
        {

        }
        
        public BoardItemWrapper_Company GetComponent()
        {
            return _boardItemWrapper;
        }
    }
}