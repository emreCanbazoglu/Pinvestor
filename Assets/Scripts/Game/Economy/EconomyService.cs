using System.Collections.Generic;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.GameConfigSystem;
using System.Linq;
using UnityEngine;

namespace Pinvestor.Game.Economy
{
    /// <summary>
    /// Applies turn resolution math: credits accumulated revenue, deducts per-company
    /// operational costs, and writes the delta to PlayerEconomyState.
    /// Op-costs come exclusively from GameConfigManager company config.
    /// </summary>
    public sealed class EconomyService
    {
        private readonly TurnRevenueAccumulator _accumulator;
        private readonly GameConfigManager _gameConfigManager;

        public EconomyService(
            TurnRevenueAccumulator accumulator,
            GameConfigManager gameConfigManager)
        {
            _accumulator = accumulator;
            _gameConfigManager = gameConfigManager;
        }

        /// <summary>
        /// Credits turn revenue and deducts op-costs for each placed company.
        /// Writes the net delta to PlayerEconomyState.
        /// Called from Turn.RunResolutionPhase().
        /// </summary>
        public void ApplyResolution(IEnumerable<BoardItem_Company> placedCompanies)
        {
            if (PlayerEconomyState.Instance == null || !PlayerEconomyState.Instance.IsInitialized)
            {
                Debug.LogWarning("[EconomyService] PlayerEconomyState not initialized. Skipping resolution.");
                return;
            }

            float totalRevenue = _accumulator.GetTotalTurnRevenue();
            float totalOpCost = CalculateTotalOpCost(placedCompanies);

            float worthBefore = PlayerEconomyState.Instance.NetWorth;

            Debug.Log(
                $"[EconomyService] Resolution: " +
                $"netWorthBefore={worthBefore}, " +
                $"turnRevenue={totalRevenue}, " +
                $"totalOpCost={totalOpCost}, " +
                $"netWorthAfter={worthBefore + totalRevenue - totalOpCost}");

            PlayerEconomyState.Instance.ApplyResolutionDelta(totalRevenue, totalOpCost);
        }

        private float CalculateTotalOpCost(IEnumerable<BoardItem_Company> placedCompanies)
        {
            if (placedCompanies == null)
                return 0f;

            if (_gameConfigManager == null || !_gameConfigManager.IsInitialized)
            {
                Debug.LogWarning(
                    "[EconomyService] GameConfigManager not available. Op-costs will be 0.");
                return 0f;
            }

            if (!_gameConfigManager.TryGetService(out CompanyConfigService companyConfigService))
            {
                Debug.LogWarning(
                    "[EconomyService] CompanyConfigService unavailable. Op-costs will be 0.");
                return 0f;
            }

            float total = 0f;
            foreach (BoardItem_Company company in placedCompanies)
            {
                if (company?.CompanyCardDataSo?.CompanyId == null)
                    continue;

                if (!companyConfigService.TryGetCompanyTurnlyCost(
                        company.CompanyCardDataSo.CompanyId,
                        out float opCost))
                    continue;

                total += opCost;
            }

            return total;
        }
    }
}
