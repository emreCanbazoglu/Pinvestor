using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pinvestor.Game.BallShooter
{
    public class BallShooter : MonoBehaviour
    {
        [SerializeField] private BallShooterInputController _inputController = null;

        [SerializeField] private Transform _shootPoint = null;
        [SerializeField] private float _shootSpeed = 10f;
        
        [SerializeField] private Ball _ballPrefab = null;
        
        private Ball _currentBall = null;
        
        public async UniTask ShootBallAsync()
        {
            _currentBall = CreateBall();

            _inputController.OnAimInput += OnAimInput;
            _inputController.OnShootInput += OnShootInput;
            
            await UniTask.WaitUntil(() => !_currentBall.IsActive);
        }

        private Ball CreateBall()
        {
            var ball = Instantiate(
                _ballPrefab,
                _shootPoint.position,
                Quaternion.identity);
            
            ball.transform.SetParent(_shootPoint);
            
            return ball;
        }
        
        private void OnAimInput(
            Vector2 position)
        {
        }
        
        private void OnShootInput(
            Vector2 position)
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(
                new Vector3(position.x, position.y, 0));
            
            _inputController.OnShootInput -= OnShootInput;
            
            var direction 
                = worldPosition - _shootPoint.position;
            
            direction.z = 0;
            
            ThrowBall(direction);
        }

        private void ThrowBall(
            Vector2 direction)
        {
            
        }
        
        
        
        
    }
}
