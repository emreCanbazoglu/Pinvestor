using System;
using UnityEngine;
#if MM_NETCODE
using Unity.Netcode;
#endif

namespace AbilitySystem
{
    public enum ETarget
    {
        NONE = 0,
        TARGET = 1,
        DIRECTION = 3,
        SELF_DIRECTION = 4,
        POSITION = 5,
    }
    
    public struct AbilityTargetData : IEquatable<AbilityTargetData>
    {
        public ETarget TargetType;

        public Transform[] Targets;

        public Vector3[] Positions;

        public Vector3[] Directions;

        public bool HasTargets()
        {
            switch (TargetType)
            {
                case ETarget.TARGET:
                    return Targets.Length > 0;
                case ETarget.DIRECTION:
                    return Directions.Length > 0;
                case ETarget.POSITION:
                    return Positions.Length > 0;
                case ETarget.SELF_DIRECTION:
                    return true;
                default:
                    return true;
            }
        }
        
        public Vector3 GetTargetPosition()
        {
            if (!HasTargets())
                return Vector3.positiveInfinity;
            
            switch (TargetType)
            {
                case ETarget.TARGET:
                    if(Targets.Length == 0
                       || Targets[0] == null)
                        return Vector3.positiveInfinity;
                    
                    return Targets[0].position;
                case ETarget.DIRECTION:
                    return Positions[0];
                case ETarget.POSITION:
                    return Positions[0];
                case ETarget.SELF_DIRECTION:
                    return Directions[0];
                default:
                    return Vector3.zero;
            }
        }

        public bool Equals(AbilityTargetData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Targets, other.Targets) 
                   && Equals(Positions, other.Positions) 
                   && Equals(Directions, other.Directions) 
                   && TargetType == other.TargetType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AbilityTargetData)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Targets, Positions, Directions, (int)TargetType);
        }
    }
}
