using System.Collections;
using System.Collections.Generic;
using AbilitySystem;
using UnityEngine;

namespace AttributeSystem.Components
{
    public class AbilitySystemCharacterReference : MonoBehaviour
    {
        [field: SerializeField] public AbilitySystemCharacter AbilitySystemCharacter { get; private set; } = null;
    }
}
