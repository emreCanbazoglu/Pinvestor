using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.GameConfigSystem;
using UnityEngine;

namespace Pinvestor.Game.BallSystem
{
    public class BallShooter : MonoBehaviour
    {
        [SerializeField] private BallShooterInputController _inputController = null;

        [SerializeField] private Transform _shootPoint = null;
        [SerializeField] private float _shootSpeed = 10f;

        [Tooltip("Camera used to project the aim pointer onto the board surface. Falls back to Camera.main.")]
        [SerializeField] private Camera _camera = null;

        [Tooltip("Aim angles are measured on the board plane: 0 = board +X (right), 90 = board +Z (away from the shooter).")]
        [SerializeField] private float _aimCenterAngle = 90f;
        [SerializeField] private float _aimArc = 150f;
        [SerializeField] private float _angleStep = 5f;

        [SerializeField] private Ball _ballPrefab = null;
        
        [SerializeField] private LineRenderer _trajectoryRenderer = null;
        [SerializeField] private Transform _trajectoryImpactMarker = null;
        
        [SerializeField] private float _previewLength = 10f;
        [SerializeField] private int _previewMaxCollisions = 3;

        private float _coef = 1.0f;
        
        private BallMover _ballMover = null;
        private BallConfigProvider _ballConfigProvider = null;
        private bool _ballConfigApplied;
        
        private Ball _currentBall = null;

        /// <summary>World-space aim direction, always lying on the board plane.</summary>
        private Vector3 _aimDirection;
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

            // Default to the center of the aim arc so the preview is valid before
            // the player has moved the pointer.
            _aimDirection = GetQuantizedDirection(
                GameManager.Instance.BoardWrapper,
                Vector3.zero);

            _inputController.OnAimInput += OnAimInput;
            _inputController.OnShootInput += OnShootInput;
            
            _waitingForShootInput = true;
            
            _inputController.Activate();
            
            DrawTrajectoryPreviewAsync()
                .Forget();
            
            await UniTask.WaitUntil(() => !_waitingForShootInput);

            // Was never unsubscribed, so every turn stacked another handler and
            // each one re-ran the aim plane raycast.
            _inputController.OnAimInput -= OnAimInput;

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
            BoardWrapper boardWrapper = GameManager.Instance.BoardWrapper;

            // Under a perspective camera the pointer only becomes an aim target
            // once its ray is intersected with the board surface.
            if (!boardWrapper.TryGetSurfacePoint(
                    GetCamera(),
                    position,
                    out Vector3 surfacePoint))
                return;

            _aimDirection = GetQuantizedDirection(
                boardWrapper,
                surfacePoint - _shootPoint.position);
        }

        /// <summary>
        /// Clamps and quantizes an aim direction on the board plane. Angles are
        /// measured in board-local space (0 = local +X, 90 = local +Z), so the
        /// authored arc keeps its meaning however the board is oriented.
        /// </summary>
        private Vector3 GetQuantizedDirection(
            BoardWrapper boardWrapper,
            Vector3 worldDirection)
        {
            Transform boardTransform = boardWrapper.transform;

            Vector3 localDirection
                = boardTransform.InverseTransformDirection(worldDirection);

            // Flatten onto the board plane — the pointer sits above it and the
            // shoot point may be offset in height.
            localDirection.y = 0f;

            float angle = localDirection.sqrMagnitude < Mathf.Epsilon
                ? _aimCenterAngle
                : Mathf.Atan2(localDirection.z, localDirection.x) * Mathf.Rad2Deg;

            float aimMin = _aimCenterAngle - _aimArc / 2f;
            float aimMax = _aimCenterAngle + _aimArc / 2f;

            angle = Mathf.Clamp(angle, aimMin, aimMax);
            float quantized = Mathf.Round(angle / _angleStep) * _angleStep;
            float rad = quantized * Mathf.Deg2Rad;

            return boardTransform.TransformDirection(
                new Vector3(
                    Mathf.Cos(rad),
                    0f,
                    Mathf.Sin(rad))).normalized;
        }

        private Camera GetCamera()
        {
            if (_camera == null)
                _camera = Camera.main;

            return _camera;
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

            if (_trajectoryImpactMarker != null)
            {
                bool hasImpact = _trajectoryPoints.Count > 0;
                _trajectoryImpactMarker.gameObject.SetActive(hasImpact);
                if (hasImpact)
                    _trajectoryImpactMarker.position = _trajectoryPoints[^1];
            }
        }
        
        private void OnShootInput(
            Vector2 position)
        {
            _inputController.OnShootInput -= OnShootInput;
            
            _waitingForShootInput = false;
            
            ThrowBall(_aimDirection);

            _trajectoryRenderer.positionCount = 0;
            _trajectoryRenderer.SetPositions(Array.Empty<Vector3>());

            if (_trajectoryImpactMarker != null)
                _trajectoryImpactMarker.gameObject.SetActive(false);
        }

        private void ThrowBall(
            Vector3 direction)
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
