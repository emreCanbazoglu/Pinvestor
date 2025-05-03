using UnityEngine;

namespace Pinvestor.Game
{
    [CreateAssetMenu(
        menuName = "Pinvestor/Game/Stage/Stage Settings",
        fileName = "StageSettings")]
    public class StageSettings : ScriptableObject
    {
        [field: SerializeField] public int TurnCount { get; private set; } = 0;
        [field: SerializeField] public float MinimumNetWorthToBeatStage { get; private set; } = 0;
    }
}