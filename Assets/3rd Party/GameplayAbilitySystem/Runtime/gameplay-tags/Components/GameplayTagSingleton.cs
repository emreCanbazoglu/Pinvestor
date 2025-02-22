using GameplayTag.Authoring;
using UnityEngine;

namespace GameplayTag
{
    public class GameplayTagSingleton : Singleton<GameplayTagSingleton>
    {
        [field: SerializeField] public GameplayTagContainerScriptableObject GameplayTagContainer { get; private set; }
    }
}
