using System;
using UnityEngine;

namespace Pinvestor.GameConfigSystem
{
    [CreateAssetMenu(
        menuName = "Pinvestor/Game Config/Game Config Authoring",
        fileName = "GameConfigAuthoring")]
    public sealed class GameConfigAuthoringAsset : ScriptableObject
    {
        [SerializeField] private int _schemaVersion = 1;
        [SerializeField] private CompanyAuthoringEntry[] _companies = Array.Empty<CompanyAuthoringEntry>();
        [SerializeField] private NamedFloatAuthoringEntry[] _balance = Array.Empty<NamedFloatAuthoringEntry>();
        [SerializeField] private NamedFloatAuthoringEntry[] _roundCriteria = Array.Empty<NamedFloatAuthoringEntry>();
        [SerializeField] private RoundCycleAuthoringEntry[] _runCycleRounds = Array.Empty<RoundCycleAuthoringEntry>();
        [SerializeField] private BallAuthoringConfig _ball = new BallAuthoringConfig();
        [SerializeField] private NamedFloatAuthoringEntry[] _shop = Array.Empty<NamedFloatAuthoringEntry>();

        public int SchemaVersion => _schemaVersion;
        public CompanyAuthoringEntry[] Companies => _companies;
        public NamedFloatAuthoringEntry[] Balance => _balance;
        public NamedFloatAuthoringEntry[] RoundCriteria => _roundCriteria;
        public RoundCycleAuthoringEntry[] RunCycleRounds => _runCycleRounds;
        public BallAuthoringConfig Ball => _ball;
        public NamedFloatAuthoringEntry[] Shop => _shop;
    }

    [Serializable]
    public sealed class CompanyAuthoringEntry
    {
        public string companyId = string.Empty;
        public NamedFloatAuthoringEntry[] attributes = Array.Empty<NamedFloatAuthoringEntry>();
        public NamedFloatAuthoringEntry[] values = Array.Empty<NamedFloatAuthoringEntry>();
    }

    [Serializable]
    public sealed class NamedFloatAuthoringEntry
    {
        public string key = string.Empty;
        public float value;
    }

    [Serializable]
    public sealed class BallAuthoringConfig
    {
        public float shootSpeed = 10f;
        public float previewLength = 10f;
    }

    [Serializable]
    public sealed class RoundCycleAuthoringEntry
    {
        public string roundId = "Round_1";
        public int turnCount = 3;
        public float requiredWorth;
    }
}
