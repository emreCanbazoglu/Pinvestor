using System;
using UnityEngine;

namespace Pinvestor.Game.BallSystem
{
    public class BallTarget : MonoBehaviour
    {
        public Action<Ball> OnBallCollided { get; set; }
        
        public void CollidedBy(Ball ball)
        {
            Debug.Log($"Ball collided with {gameObject.name}");
            
            OnBallCollided?.Invoke(ball);
        }
    }
}