using UnityEngine;

namespace Pinvestor.Game.Economy
{
    /// <summary>
    /// Per-instance valuation model for a placed company.
    /// Created on placement alongside CompanyHealthState.
    ///
    /// Initial valuation = purchase cost (from GameConfig company entry).
    /// CashoutValue = PurchaseCost * cashout_rate (from GameConfig balance section).
    /// Valuation is static for now; booster effects may modify it in future specs.
    /// </summary>
    public sealed class CompanyValuationModel
    {
        /// <summary>The cost the player paid to place this company (from GameConfig).</summary>
        public float PurchaseCost { get; }

        /// <summary>
        /// The fraction of PurchaseCost returned on cashout.
        /// Sourced from GameConfig balance section key "cashout_rate".
        /// Falls back to <see cref="DefaultCashoutRate"/> if the key is absent.
        /// </summary>
        public float CashoutRate { get; }

        /// <summary>Fallback cashout rate used when game-config.json does not define "cashout_rate".</summary>
        public const float DefaultCashoutRate = 0.5f;

        /// <summary>The key used to look up cashout rate in the balance config section.</summary>
        public const string CashoutRateKey = "cashout_rate";

        /// <summary>Computed cashout payout: PurchaseCost * CashoutRate.</summary>
        public float CashoutValue => PurchaseCost * CashoutRate;

        public CompanyValuationModel(float purchaseCost, float cashoutRate)
        {
            PurchaseCost = Mathf.Max(0f, purchaseCost);
            CashoutRate = Mathf.Clamp01(cashoutRate);
        }
    }
}
