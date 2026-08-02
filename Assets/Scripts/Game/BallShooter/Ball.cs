using System.Collections.Generic;
using AbilitySystem;
using MEC;
using Pinvestor.GameplayAbilitySystem.Abilities;
using UnityEngine;

namespace Pinvestor.Game.BallSystem
{
    public class BallHitEvent : IEvent
    {
        public Ball Ball { get; }
        
        public BallHitEvent(Ball ball)
        {
            Ball = ball;
        }
    }
    
    public class Ball : MonoBehaviour
    {
        [field: SerializeField] public LayerMask LayerMask { get; private set; }

        [Tooltip("Safety net: a ball that never finds its way back out of the entrance is "
                 + "despawned after this long, so the launch phase cannot deadlock.")]
        [SerializeField] private float _maxFlightDuration = 30f;

        [SerializeField] private AbilitySystemCharacter _abilitySystemCharacter = null;
        [SerializeField] private BallCollideAbilityScriptableObject _ballCollideAbility = null;
        
        private BallCollideAbilitySpec _ballCollideAbilitySpec;
        
        public bool IsActive { get; private set; } = false;
        
        private CoroutineHandle _moveRoutineHandle;

        private void OnDestroy()
        {
            // Ensure BallShooter.ShootBallAsync() is never left waiting if the ball
            // is destroyed by anything other than the normal OnTriggerExit path.
            IsActive = false;
            Timing.KillCoroutines(_moveRoutineHandle);
        }

        public void Shoot(
            BallMover ballMover,
            Vector3 direction,
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
            Vector3 direction,
            float speed)
        {
            transform.forward = direction;

            float elapsed = 0f;

            while (true)
            {
                var distance
                    = speed * Time.fixedDeltaTime;

                Step(ballMover, distance);

                elapsed += Time.fixedDeltaTime;

                if (_maxFlightDuration > 0f
                    && elapsed >= _maxFlightDuration)
                {
                    Debug.LogWarning(
                        $"[Ball] Flight exceeded {_maxFlightDuration}s without leaving through the " +
                        "entrance — despawning so the launch phase can continue.",
                        this);

                    Despawn();
                    yield break;
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        private void Despawn()
        {
            IsActive = false;

            Destroy(gameObject);
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

            // The entrance faces into the board, so a positive dot means the ball is
            // still travelling up the board and is only leaving the entrance volume
            // on its way in. Only a ball heading back out is despawned.
            if(Vector3.Dot(transform.forward, entrance.transform.forward) > 0f)
                return;

            Despawn();
        }
    }
}