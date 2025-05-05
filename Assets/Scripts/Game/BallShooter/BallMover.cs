using System.Collections.Generic;
using UnityEngine;

namespace Pinvestor.Game.BallSystem
{
    public struct StepResult
    {
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
    }

    public class SimulationResult
    {
        public List<Vector3> Positions { get; private set; }
        public List<Vector3> Directions { get; private set; }
        
        public SimulationResult()
        {
            Positions = new List<Vector3>();
            Directions = new List<Vector3>();
        }
    }
    
    public class BallMover
    {
        private int _maxIterations = 10;
        
        public StepResult Step(
            Ball ball, 
            float distance)
        {
            Vector3 origin = ball.transform.position;
            Vector3 currentDir = ball.transform.forward;
            float remainingDistance = distance;
            float sphereRadius = ball.transform.localScale.x / 2f;

            for (int index = 0; index < _maxIterations && remainingDistance > 0.01f; index++)
            {
                var stepResult
                    = SimulateStep(
                        origin, 
                        currentDir, 
                        remainingDistance, 
                        sphereRadius,
                        ball.LayerMask, 
                        out Vector3 newOrigin, 
                        out Vector3 newDir,
                        out float traveled);
                
                if (stepResult)
                {
                    origin = newOrigin;
                    currentDir = newDir;
                    remainingDistance -= traveled;
                }
                else
                {
                    origin = newOrigin;
                    break;
                }
            }

            return new StepResult
            {
                Position = origin,
                Direction = currentDir
            };
        }
        
        private StepResult Step(
            Ball ball,
            float distance,
            Vector3 currentDir,
            Vector3 origin)
        {
            float remainingDistance = distance;
            float sphereRadius = ball.transform.localScale.x / 2f;

            for (int index = 0; index < _maxIterations && remainingDistance > 0.01f; index++)
            {
                if (TryGetValidHit(
                        origin,
                        currentDir,
                        sphereRadius,
                        remainingDistance,
                        ball.LayerMask,
                        out RaycastHit validHit))
                {
                    float distanceToHit = Mathf.Max(validHit.distance - 0.01f, 0f);
                    origin += currentDir * distanceToHit;
                    remainingDistance -= distanceToHit;

                    Vector3 reflectDir = Vector3.Reflect(currentDir, validHit.normal).normalized;

                    if (Vector3.Dot(reflectDir, validHit.normal) > -0.9f)
                        reflectDir = Vector3.Lerp(reflectDir, validHit.normal, 0.1f).normalized;

                    currentDir = reflectDir;
                    origin += validHit.normal * 0.01f; // small push to avoid stuck bouncing
                }
                else
                {
                    origin += currentDir * remainingDistance;
                    break;
                }
            }

            return new StepResult
            {
                Position = origin,
                Direction = currentDir
            };
        }
        
        public List<Vector3> SimulateAccurate(
            Ball ball,
            Vector3 initialDirection,
            float totalDistance,
            float stepDistance)
        {
            List<Vector3> positions = new List<Vector3>();
            
            Ball.LogVector("Simulation Initial Position", ball.transform.position);
            Ball.LogVector("Simulation Initial Direction", initialDirection);
            Debug.Log("Step Distance: " + stepDistance);
            
            Vector3 origin = ball.transform.position;
            Vector3 currentDir = initialDirection;
            float remainingDistance = totalDistance;

            while (remainingDistance > 0.01f)
            {
                float step = Mathf.Min(stepDistance, remainingDistance);

                var stepResult
                    = Step(ball, 
                        step, 
                        currentDir, 
                        origin); // overload Step
                origin = stepResult.Position;
                currentDir = stepResult.Direction;

                positions.Add(origin);
                
                remainingDistance -= step;
            }

            return positions;
        }

        
        private bool SimulateStep(
            Vector3 origin,
            Vector3 direction,
            float remainingDistance,
            float sphereRadius,
            LayerMask collisionMask,
            out Vector3 newOrigin,
            out Vector3 newDirection,
            out float distanceTraveled)
        {
            newOrigin = origin;
            newDirection = direction;
            distanceTraveled = 0f;
            
            Debug.Log("Step Distance: " + remainingDistance);

            if (TryGetValidHit(
                    origin,
                    direction, 
                    sphereRadius, 
                    remainingDistance,
                    collisionMask, 
                    out RaycastHit hit))
            {
                float distanceToHit = Mathf.Max(hit.distance - 0.01f, 0f);
                newOrigin += direction * distanceToHit;
                distanceTraveled = distanceToHit;

                Vector3 reflectDir = Vector3.Reflect(direction, hit.normal).normalized;

                // Avoid shallow stuck bounces
                if (Vector3.Dot(reflectDir, hit.normal) > -0.9f)
                    reflectDir = Vector3.Lerp(reflectDir, hit.normal, 0.1f).normalized;

                newDirection = reflectDir;
                newOrigin += hit.normal * 0.01f; // push away from surface

                return true;
            }
            else
            {
                newOrigin += direction * remainingDistance;
                distanceTraveled = remainingDistance;

                return false;
            }
        }

        
        private bool TryGetValidHit(
            Vector3 origin, 
            Vector3 direction, 
            float sphereRadius,
            float maxDistance, 
            LayerMask collisionMask,
            out RaycastHit validHit)
        {
            RaycastHit[] hits = new RaycastHit[10];
            
            var size 
                = Physics.SphereCastNonAlloc(
                    origin, 
                    sphereRadius, 
                    direction, 
                    hits, 
                    maxDistance, 
                    collisionMask,
                    QueryTriggerInteraction.Collide);
            
            if(size == 0)
            {
                validHit = default;
                return false;
            }
            
            // Manually sort hits by distance
            var sortedHits = new List<RaycastHit>(size);
            for (int i = 0; i < size; i++)
                sortedHits.Add(hits[i]);

            sortedHits.Sort(
                (a, b) 
                    => a.distance.CompareTo(b.distance));
            

            foreach (var hit in sortedHits)
            {
                if(hit.collider == null)
                    continue;

                if (hit.collider.TryGetComponent(out BallReflector _))
                {
                    validHit = hit;
                    return true;
                }
            }

            validHit = default;
            return false;
        }
    }
}