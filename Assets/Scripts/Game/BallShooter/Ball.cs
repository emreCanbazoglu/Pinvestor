using System.Collections.Generic;
using MEC;
using UnityEngine;

namespace Pinvestor.Game.BallSystem
{
    public class Ball : MonoBehaviour
    {
        [field: SerializeField] public LayerMask LayerMask { get; private set; }

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

        private void OnTriggerExit(Collider other)
        {
            if(!other.TryGetComponent(out BallEntrance entrance))
                return;
            
            if(transform.forward.y > 0)
                return;
            
            IsActive = false;
            
            Destroy(gameObject);
        }
        
        public static void LogVector(string label, Vector3 v, int decimals = 6)
        {
            string format = "F" + decimals;
            Debug.Log($"{label} = ({v.x.ToString(format)}, {v.y.ToString(format)}, {v.z.ToString(format)})");
        }
    }
}