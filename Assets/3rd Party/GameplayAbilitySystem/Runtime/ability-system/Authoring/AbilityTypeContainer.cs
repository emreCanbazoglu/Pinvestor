using System;
using UnityEngine;

namespace AbilitySystem.Authoring
{
    [CreateAssetMenu(fileName = "New AbilityTypeContainer", menuName = "Gameplay Ability System/Ability Types/Create New AbilityTypeContainer", order = 0)]
    public class AbilityTypeContainer : ScriptableObject
    {
        [field: SerializeField] public AbilityType[] AbilityTypes
            = Array.Empty<AbilityType>();
    }
}