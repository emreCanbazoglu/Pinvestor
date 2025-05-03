using System.Collections.Generic;
using MEC;
using UnityEngine;

namespace Pinvestor.Game.BallShooter
{
    public class Ball : MonoBehaviour
    {
        [SerializeField] private LayerMask _layerMask = default;
        public bool IsActive { get; private set; } = false;
        
        public Vector2 Direction { get; private set; }
        
        private CoroutineHandle _moveRoutineHandle;
        
        public void Shoot(
            Vector2 direction,
            float speed)
        {
            IsActive = true;

            _moveRoutineHandle
                = Timing.RunCoroutine(
                    MoveRoutine(direction, speed),
                    Segment.FixedUpdate);
        }

        private IEnumerator<float> MoveRoutine(
            Vector2 direction,
            float speed)
        {
            Direction = direction;
            
            while (true)
            {
                Step(speed, Time.fixedDeltaTime);
                
                yield return Timing.WaitForOneFrame;
            }
        }
        
        private void Step(
            float speed,
            float deltaTime)
        {
            Vector2 movement = Direction * speed * deltaTime;

            ContactFilter2D contactFilter = new ContactFilter2D
            {
                useTriggers = true,
                useLayerMask = true,
                layerMask = _layerMask
            };
            
            RaycastHit2D[] hits = new RaycastHit2D[10];

            var hitCount 
                = Physics2D.Raycast(
                    transform.position,
                    Direction,
                    contactFilter,
                    hits,
                    movement.magnitude);
            
            if (hitCount > 0)
            {
                // Get the closest hit
                RaycastHit2D hit = hits[0];

                // Calculate reflection vector
                Vector2 reflectDir = Vector2.Reflect(movement, hit.normal);

                // Move to the hit point (slightly before to avoid sticking)
                float distanceToHit = hit.distance - 0.01f;
                Vector2 positionBeforeHit = (Vector2)transform.position + movement.normalized * distanceToHit;
                transform.position = positionBeforeHit;

                // Move in the reflected direction for the remaining distance
                float remainingDistance = movement.magnitude - distanceToHit;
                transform.position += (Vector3)(reflectDir.normalized * remainingDistance);
                
                // Update direction
                Direction = reflectDir;
            }
            else
            {
                // No hit, move normally
                transform.position += (Vector3)movement;
            }
        }
    }
}