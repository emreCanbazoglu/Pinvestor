using System;
using UnityEngine;

namespace Pinvestor.Game
{
    [CreateAssetMenu(
        menuName = "Pinvestor/Game/Run Cycle Settings",
        fileName = "RunCycleSettings")]
    public class RunCycleSettings : ScriptableObject
    {
        [field: SerializeField] public RoundCycleSettings[] Rounds { get; private set; }
            = Array.Empty<RoundCycleSettings>();
    }

    [Serializable]
    public class RoundCycleSettings
    {
        [field: SerializeField] public string RoundId { get; private set; } = "Round_1";
        [field: SerializeField] public int TurnCount { get; private set; } = 3;
        [field: SerializeField] public float RequiredWorth { get; private set; } = 0f;

        public RoundCycleSettings()
        {
        }

        public RoundCycleSettings(
            string roundId,
            int turnCount,
            float requiredWorth)
        {
            RoundId = roundId;
            TurnCount = turnCount;
            RequiredWorth = requiredWorth;
        }
    }
}
