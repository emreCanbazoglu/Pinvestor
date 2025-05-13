using System.Collections.Generic;
using MEC;
using MMFramework.CameraSystem;
using Pinvestor.CameraSystem;
using UnityEngine;

[ExecuteInEditMode]
public class CanvasLookAtCamera : MonoBehaviour
{
    [SerializeField] private CameraTypeSO _cameraType = null;
    
    [SerializeField] private bool _updateOnce = false;
    
    private Camera _camera = null;
    private bool _isUpdated = false;

    private void LateUpdate()
    {
        if (_updateOnce && _isUpdated)
            return;

        UpdateLookAt();
    }

    private void OnEnable()
    {
        if(!Application.isPlaying)
            return;
        
        Timing.RunCoroutine(
            UpdateOnce().CancelWith(gameObject),
            Segment.EndOfFrame);
    }

    private void InitializeCamera()
    {
        CameraManager.Instance.TryGetRegisteredCamera(
            _cameraType.GetID(),
            out var targetCamera);

        _camera = targetCamera.Camera;
    }
    
    private IEnumerator<float> UpdateOnce()
    {
        yield return Timing.WaitForOneFrame;

        UpdateLookAt();
    }
    
    private void UpdateLookAt()
    {
        _isUpdated = true;
        
        if(_camera == null)
            return;
        
        Quaternion lookRotation = _camera.transform.rotation;
        transform.rotation = lookRotation;
    }
}