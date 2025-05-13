using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;

[ExecuteInEditMode]
public class FollowTarget : MonoBehaviour
{
    [SerializeField] private Transform _targetTransform = null;
    [SerializeField] private Vector3 _localOffset = Vector3.zero;

    [SerializeField] private bool _updateOnce = false;
    
    private bool _isUpdated = false;

    private void LateUpdate()
    {
        if (_updateOnce && _isUpdated)
            return;

        UpdateFollow();
    }

    private void OnEnable()
    {
        if(!Application.isPlaying)
            return;
        
        Timing.RunCoroutine(
                UpdateOnce(),
                Segment.EndOfFrame);
    }
    
    private IEnumerator<float> UpdateOnce()
    {
        yield return Timing.WaitForOneFrame;
        
        UpdateFollow();
    }

    private void UpdateFollow()
    {
        if(_targetTransform == null)
            return;
        
        _isUpdated = true;
        
        Vector3 targetPos = _targetTransform.position;

        transform.position = targetPos;
        
        // Calculate the offset in local space
        Vector3 localPositionOffset = transform.localRotation * _localOffset;

        // Apply the local offset to the local position
        transform.localPosition += localPositionOffset;
    }
}
