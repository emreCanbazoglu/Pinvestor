using System.Collections.Generic;
using Pinvestor.GameConfigSystem;
using UnityEngine;

namespace Pinvestor.Game.Offer
{
    /// <summary>
    /// Draws up to 3 distinct company configs from the RunCompanyPool for a single turn's offer.
    /// Handles pool depletion gracefully — if fewer than 3 are available, returns what exists.
    /// </summary>
    public class CompanyOfferDrawer
    {
        private const int TargetOfferCount = 3;

        private readonly RunCompanyPool _pool;
        private readonly CompanyConfigService _companyConfigService;

        public CompanyOfferDrawer(
            RunCompanyPool pool,
            CompanyConfigService companyConfigService)
        {
            _pool = pool;
            _companyConfigService = companyConfigService;
        }

        /// <summary>
        /// Draws up to 3 distinct companies from the pool.
        /// Returns an empty list if the pool is empty.
        /// The drawn company IDs are NOT removed from the pool here —
        /// removal happens after selection (placed) or after offer resolves (discarded).
        /// </summary>
        public List<CompanyConfigModel> DrawOffer()
        {
            IReadOnlyList<string> available = _pool.Available;

            if (available.Count == 0)
            {
                Debug.LogWarning("[CompanyOfferDrawer] Pool is empty. No offers to draw.");
                return new List<CompanyConfigModel>();
            }

            // Shuffle indices for a random draw without modifying the pool list.
            List<int> indices = new List<int>(available.Count);
            for (int i = 0; i < available.Count; i++)
                indices.Add(i);

            // Fisher-Yates shuffle using UnityEngine.Random.
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            int drawCount = Mathf.Min(TargetOfferCount, available.Count);
            List<CompanyConfigModel> result = new List<CompanyConfigModel>(drawCount);

            for (int i = 0; i < drawCount; i++)
            {
                string companyId = available[indices[i]];
                if (_companyConfigService.TryGetCompanyConfig(companyId, out CompanyConfigModel config))
                {
                    result.Add(config);
                }
                else
                {
                    Debug.LogWarning($"[CompanyOfferDrawer] No CompanyConfigModel found for id '{companyId}'. Skipping.");
                }
            }

            Debug.Log($"[CompanyOfferDrawer] Drew {result.Count} offers from pool ({available.Count} available).");
            return result;
        }
    }
}
