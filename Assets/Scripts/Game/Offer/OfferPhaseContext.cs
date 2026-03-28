using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Pinvestor.GameConfigSystem;
using UnityEngine;

namespace Pinvestor.Game.Offer
{
    /// <summary>
    /// Holds the 3 offered companies and the UniTaskCompletionSource used to await
    /// the player's selection. Created fresh each turn; cleared after placement receives
    /// the result.
    ///
    /// The offer phase creates this, passes it to the UI panel, and awaits SelectionTask.
    /// The UI panel resolves SelectionTask when the player confirms a selection.
    /// </summary>
    public class OfferPhaseContext
    {
        private readonly UniTaskCompletionSource<CompanyConfigModel> _completionSource
            = new UniTaskCompletionSource<CompanyConfigModel>();

        /// <summary>The companies offered this turn. Read-only view.</summary>
        public IReadOnlyList<CompanyConfigModel> OfferedCompanies { get; }

        /// <summary>
        /// Awaitable task that resolves with the selected CompanyConfigModel
        /// when the player confirms a selection via the UI.
        /// </summary>
        public UniTask<CompanyConfigModel> SelectionTask => _completionSource.Task;

        /// <summary>
        /// The confirmed selection. Populated after SelectionTask resolves.
        /// </summary>
        public CompanyConfigModel ConfirmedSelection { get; private set; }

        public OfferPhaseContext(IReadOnlyList<CompanyConfigModel> offeredCompanies)
        {
            OfferedCompanies = offeredCompanies;
        }

        /// <summary>
        /// Called by the UI when the player confirms a selection.
        /// Resolves the SelectionTask and caches the result.
        /// Safe to call only once; subsequent calls are ignored with a warning.
        /// </summary>
        public void ConfirmSelection(CompanyConfigModel selected)
        {
            if (selected == null)
            {
                Debug.LogWarning("[OfferPhaseContext] ConfirmSelection called with null. Ignoring.");
                return;
            }

            if (_completionSource.Task.Status != UniTaskStatus.Pending)
            {
                Debug.LogWarning("[OfferPhaseContext] ConfirmSelection called but task is no longer pending. Ignoring.");
                return;
            }

            ConfirmedSelection = selected;
            _completionSource.TrySetResult(selected);
            Debug.Log($"[OfferPhaseContext] Selection confirmed: {selected.CompanyId}");
        }

        /// <summary>
        /// Fallback: resolves with the first offered company and logs a warning.
        /// Called by the turn phase if the panel closes without a selection (edge case).
        /// </summary>
        public void ForceSelectFirst()
        {
            if (_completionSource.Task.Status != UniTaskStatus.Pending)
                return;

            if (OfferedCompanies == null || OfferedCompanies.Count == 0)
            {
                Debug.LogError("[OfferPhaseContext] ForceSelectFirst: no offered companies to fall back to.");
                _completionSource.TrySetCanceled();
                return;
            }

            CompanyConfigModel fallback = OfferedCompanies[0];
            Debug.LogWarning($"[OfferPhaseContext] ForceSelectFirst: auto-selecting '{fallback.CompanyId}'.");
            ConfirmedSelection = fallback;
            _completionSource.TrySetResult(fallback);
        }

        /// <summary>
        /// Clears the confirmed selection reference so this context cannot leak data
        /// into the next turn. Call after the placement phase has received the result.
        /// </summary>
        public void Clear()
        {
            ConfirmedSelection = null;
        }
    }
}
