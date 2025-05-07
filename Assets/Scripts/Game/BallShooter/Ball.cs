using System;
using System.Collections.Generic;
using AbilitySystem;
using MEC;
using Pinvestor.Game.BallSystem.Abilities;
using UnityEngine;

namespace Pinvestor.Game.BallSystem
{
    public class Ball : MonoBehaviour
    {
        [field: SerializeField] public LayerMask LayerMask { get; private set; }

        [SerializeField] private AbilitySystemCharacter _abilitySystemCharacter = null;
        [SerializeField] private BallCollideAbilityScriptableObject _ballCollideAbility = null;
        
        private BallCollideAbilitySpec _ballCollideAbilitySpec;
        
        public bool IsActive { get; private set; } = false;
        
        private CoroutineHandle _moveRoutineHandle;

        private void OnDestroy()
        {
            Timing.KillCoroutines(_moveRoutineHandle);
        }

        public void Shoot(
            BallMover ballMover,
            Vector2 direction,
            float speed)
        {
            IsActive = true;

            _moveRoutineHandle
                = Timing.RunCoroutine(
                    MoveRoutine(ballMover, direction, speed),
                    Segment.FixedUpdate);
        }

        private IEnumerator<float> MoveRoutine(
            BallMover ballMover,
            Vector2 direction,
            float speed)
        {
            transform.forward = new Vector3(direction.x, direction.y);
            
            while (true)
            {
                var distance 
                    = speed * Time.fixedDeltaTime;
                
                Step(ballMover, distance);
                
                yield return Timing.WaitForOneFrame;
            }
        }
        
        private StepResult Step(
            BallMover ballMover,
            float distance)
        {
            var result 
                = ballMover.Step(this, distance);
            
            transform.position = result.Position;
            transform.forward = result.Direction;
            
            return result;
        }

        private void OnTriggerEnter(Collider other)
        {
            if(!other.TryGetComponent(out BallTarget ballTarget))
                return;
            
            if (_ballCollideAbilitySpec == null)
            {
                _abilitySystemCharacter.TryGetAbilitySpec(
                    _ballCollideAbility,
                    out var spec);
                
                _ballCollideAbilitySpec = spec as BallCollideAbilitySpec;
            }
            
            if (_ballCollideAbilitySpec == null)
                return;
            
            _ballCollideAbilitySpec.SetCollideTarget(ballTarget);

            _abilitySystemCharacter.TryActivateAbility(
                _ballCollideAbilitySpec);
        }

        private void OnTriggerExit(Collider other)
        {
            if(!other.TryGetComponent(out BallEntrance entrance))
                return;
            
            if(transform.forward.y > 0)
                return;
            
            IsActive = false;
            
            Destroy(gameObject);
        }
    }
}