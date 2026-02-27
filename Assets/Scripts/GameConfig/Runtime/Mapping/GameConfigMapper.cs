namespace Pinvestor.GameConfigSystem
{
    public static class GameConfigMapper
    {
        public static GameConfigRootModel Map(GameConfigRootJsonDto dto)
        {
            if (dto == null)
            {
                dto = new GameConfigRootJsonDto();
            }

            var companyParser = new CompanyConfigModuleParser();
            var balanceParser = new NamedValueModuleParser("balance");
            var roundCriteriaParser = new NamedValueModuleParser("roundCriteria");
            var runCycleParser = new RunCycleConfigModuleParser();
            var shopParser = new NamedValueModuleParser("shop");
            var ballParser = new BallConfigModuleParser();

            return new GameConfigRootModel(
                dto.schemaVersion,
                dto.generatedAtUtc ?? string.Empty,
                companyParser.Parse(dto.company),
                balanceParser.Parse(dto.balance),
                roundCriteriaParser.Parse(dto.roundCriteria),
                runCycleParser.Parse(dto.runCycle),
                ballParser.Parse(dto.ball),
                shopParser.Parse(dto.shop));
        }
    }
}
