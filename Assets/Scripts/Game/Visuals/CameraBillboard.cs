using UnityEngine;

namespace Pinvestor.Game.Visuals
{
    /// <summary>
    /// Keeps a world-space visual (company labels, HP bars, floating text) facing
    /// the camera. On the 3D board every item inherits the board's orientation, so
    /// anything authored flat — TextMeshPro quads in particular — would otherwise
    /// point away from a tilted camera.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraBillboard : MonoBehaviour
    {
        [Tooltip("Camera to face. Falls back to Camera.main.")]
        [SerializeField] private Camera _camera = null;

        [Tooltip("Match the camera's orientation instead of aiming at its position. Keeps a group of labels parallel.")]
        [SerializeField] private bool _useCameraRotation = true;

        [Tooltip("Keep the visual upright by only rotating around world up.")]
        [SerializeField] private bool _lockVerticalTilt = false;

        private void LateUpdate()
        {
            Camera targetCamera = GetCamera();

            if (targetCamera == null)
                return;

            Vector3 forward = _useCameraRotation
                ? targetCamera.transform.forward
                : transform.position - targetCamera.transform.position;

            if (_lockVerticalTilt)
                forward.y = 0f;

            if (forward.sqrMagnitude < Mathf.Epsilon)
                return;

            transform.rotation = Quaternion.LookRotation(
                forward.normalized,
                _lockVerticalTilt
                    ? Vector3.up
                    : targetCamera.transform.up);
        }

        private Camera GetCamera()
        {
            if (_camera == null)
                _camera = Camera.main;

            return _camera;
        }
    }
}
