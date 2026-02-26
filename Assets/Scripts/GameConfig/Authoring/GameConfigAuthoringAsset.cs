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
        [SerializeField] private NamedFloatAuthoringEntry[] _ball = Array.Empty<NamedFloatAuthoringEntry>();
        [SerializeField] private NamedFloatAuthoringEntry[] _shop = Array.Empty<NamedFloatAuthoringEntry>();

        public int SchemaVersion => _schemaVersion;
        public CompanyAuthoringEntry[] Companies => _companies;
        public NamedFloatAuthoringEntry[] Balance => _balance;
        public NamedFloatAuthoringEntry[] RoundCriteria => _roundCriteria;
        public NamedFloatAuthoringEntry[] Ball => _ball;
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
}

