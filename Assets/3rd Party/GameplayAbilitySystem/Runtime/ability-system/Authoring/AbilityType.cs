using UnityEngine;

namespace AbilitySystem.Authoring
{
    [CreateAssetMenu(fileName = "New GameplayTag", menuName = "Gameplay Ability System/Ability Types/Create New AbilityType", order = 0)]
    public class AbilityType : ScriptableObject
    {
        public override bool Equals(object other)
        {
            if (other == null || GetType() != other.GetType())
            {
                return false;
            }

            AbilityType otherTag = (AbilityType)other;
            return this.name == otherTag.name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }
}