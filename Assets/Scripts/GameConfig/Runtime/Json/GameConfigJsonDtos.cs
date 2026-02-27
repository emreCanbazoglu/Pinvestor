using System;

namespace Pinvestor.GameConfigSystem
{
    [Serializable]
    public class GameConfigRootJsonDto
    {
        public int schemaVersion = 1;
        public string generatedAtUtc = string.Empty;
        public CompanyConfigSectionJsonDto company = new CompanyConfigSectionJsonDto();
        public NamedValueSectionJsonDto balance = new NamedValueSectionJsonDto();
        public NamedValueSectionJsonDto roundCriteria = new NamedValueSectionJsonDto();
        public RunCycleConfigJsonDto runCycle = new RunCycleConfigJsonDto();
        public BallConfigJsonDto ball = new BallConfigJsonDto();
        public NamedValueSectionJsonDto shop = new NamedValueSectionJsonDto();
    }

    [Serializable]
    public class CompanyConfigSectionJsonDto
    {
        public CompanyConfigEntryJsonDto[] entries = Array.Empty<CompanyConfigEntryJsonDto>();
    }

    [Serializable]
    public class CompanyConfigEntryJsonDto
    {
        public string companyId = string.Empty;
        public NamedFloatJsonDto[] attributes = Array.Empty<NamedFloatJsonDto>();
        public NamedFloatJsonDto[] values = Array.Empty<NamedFloatJsonDto>();
    }

    [Serializable]
    public class NamedValueSectionJsonDto
    {
        public NamedFloatJsonDto[] values = Array.Empty<NamedFloatJsonDto>();
    }

    [Serializable]
    public class NamedFloatJsonDto
    {
        public string key = string.Empty;
        public float value;
    }

    [Serializable]
    public class BallConfigJsonDto
    {
        public float shootSpeed = 10f;
        public float previewLength = 10f;
    }

    [Serializable]
    public class RunCycleConfigJsonDto
    {
        public RoundCycleConfigJsonDto[] rounds = Array.Empty<RoundCycleConfigJsonDto>();
    }

    [Serializable]
    public class RoundCycleConfigJsonDto
    {
        public string roundId = "Round_1";
        public int turnCount = 3;
        public float requiredWorth = 0f;
    }
}
