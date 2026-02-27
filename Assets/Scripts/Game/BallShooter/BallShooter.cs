using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Pinvestor.GameConfigSystem;
using UnityEngine;

namespace Pinvestor.Game.BallSystem
{
    public class BallShooter : MonoBehaviour
    {
        [SerializeField] private BallShooterInputController _inputController = null;

        [SerializeField] private Transform _shootPoint = null;
        [SerializeField] private float _shootSpeed = 10f;

        [SerializeField] private float _aimCenterAngle = 90f;
        [SerializeField] private float _aimArc = 150f;
        [SerializeField] private float _angleStep = 5f;
        
        [SerializeField] private Ball _ballPrefab = null;
        
        [SerializeField] private LineRenderer _trajectoryRenderer = null;
        
        [SerializeField] private float _previewLength = 10f;
        [SerializeField] private int _previewMaxCollisions = 3;

        private float _coef = 1.0f;
        
        private BallMover _ballMover = null;
        private BallConfigProvider _ballConfigProvider = null;
        private bool _ballConfigApplied;
        
        private Ball _currentBall = null;
        
        private Vector2 _aimDirection;
        private List<Vector3> _trajectoryPoints
            = new List<Vector3>();

        private bool _waitingForShootInput;
        
        private void Awake()
        {
            _ballMover = new BallMover();
        }

        private void Update()
        {
            CheckInput();
        }

        private void CheckInput()
        {
            if (Input.GetKeyDown(KeyCode.Q))
                _coef++;
            if (Input.GetKeyDown(KeyCode.E))
                _coef--;
            
            _coef = Mathf.Clamp(_coef, 1f, 10f);
        }

        public async UniTask ShootBallAsync()
        {
            TryApplyBallConfig();

            _currentBall = CreateBall();

            _inputController.OnAimInput += OnAimInput;
            _inputController.OnShootInput += OnShootInput;
            
            _waitingForShootInput = true;
            
            _inputController.Activate();
            
            DrawTrajectoryPreviewAsync()
                .Forget();
            
            await UniTask.WaitUntil(() => !_waitingForShootInput);
            
            _inputController.Deactivate();
            
            await UniTask.WaitUntil(() => !_currentBall.IsActive);
        }
        
        private async UniTask DrawTrajectoryPreviewAsync()
        {
            while (_waitingForShootInput)
            {
                CalculateTrajectory(_aimDirection);
                DrawTrajectoryPreview();

                await UniTask.Yield();
            }
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
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(
                new Vector3(position.x, position.y, 0));

            var direction 
                = worldPosition - _shootPoint.position;
            
            direction.z = 0;
            
            _aimDirection = GetQuantizedDirection(direction);
        }
        
        private Vector3 GetQuantizedDirection(Vector3 inputDir)
        {
            if (inputDir == Vector3.zero) return Vector3.up;

            float angle = Mathf.Atan2(inputDir.y, inputDir.x) * Mathf.Rad2Deg;

            float aimMin = _aimCenterAngle - _aimArc / 2f;
            float aimMax = _aimCenterAngle + _aimArc / 2f;

            angle = Mathf.Clamp(angle, aimMin, aimMax);
            float quantized = Mathf.Round(angle / _angleStep) * _angleStep;
            float rad = quantized * Mathf.Deg2Rad;

            return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0).normalized;
        }
        
        private void CalculateTrajectory(
            Vector3 direction)
        {
            _trajectoryPoints
                = _ballMover.SimulateAccurate(
                    _currentBall,
                    direction,
                    _previewLength,
                    _shootSpeed * Time.fixedDeltaTime,
                    _previewMaxCollisions);
        }
        
        private void DrawTrajectoryPreview()
        {
            for (int index = 0; index < _trajectoryPoints.Count - 1; index++)
            {
                Vector3 position = _trajectoryPoints[index];
                Vector3 nextPosition = _trajectoryPoints[index + 1];
                
                Debug.DrawLine(position, nextPosition, Color.red);
                
            }
            
            _trajectoryRenderer.positionCount 
                = _trajectoryPoints.Count;
            _trajectoryRenderer.SetPositions(
                _trajectoryPoints.ToArray());
        }
        
        private void OnShootInput(
            Vector2 position)
        {
            _inputController.OnShootInput -= OnShootInput;
            
            _waitingForShootInput = false;
            
            ThrowBall(_aimDirection);

            _trajectoryRenderer.positionCount = 0;
            _trajectoryRenderer.SetPositions(Array.Empty<Vector3>());
        }

        private void ThrowBall(
            Vector2 direction)
        {
            _currentBall.Shoot(
                _ballMover, 
                direction, 
                _shootSpeed);
        }

        private void TryApplyBallConfig()
        {
            if (_ballConfigApplied)
            {
                return;
            }

            GameConfigManager gameConfigManager = GameConfigManager.Instance;
            if (gameConfigManager == null || !gameConfigManager.IsInitialized)
            {
                return;
            }

            if (_ballConfigProvider == null)
            {
                _ballConfigProvider = new BallConfigProvider(gameConfigManager);
            }

            BallConfigModel ballConfig = _ballConfigProvider.GetConfig();
            if (ballConfig == null)
            {
                return;
            }

            if (ballConfig.ShootSpeed > 0f)
            {
                _shootSpeed = ballConfig.ShootSpeed;
            }

            if (ballConfig.PreviewLength > 0f)
            {
                _previewLength = ballConfig.PreviewLength;
            }

            _ballConfigApplied = true;
        }
    }
}
