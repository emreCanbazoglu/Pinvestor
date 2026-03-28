// TODO(spec-005): This file is a stub created by spec-006 to reserve the cashout UI integration point.
// When spec 005 (Company Offer Selection) merges, replace this stub with the real CompanyOfferPanel
// that shows the 3 offer cards + Portfolio section for cashout.
//
// T018 — Read CompanyOfferPanel (this file) before adding cashout UI.
// T019 — Add a "Portfolio" section listing placed companies with name, health, and cashout payout value.
// T020 — Add cashout button per listed company — calls CashoutService.TryCashout() on confirm.
// T021 — Disable cashout button when company is at PendingCollapse state.

using Pinvestor.BoardSystem.Authoring;
using Pinvestor.Game;
using Pinvestor.Game.Economy;
using UnityEngine;

namespace Pinvestor.UI.Offer
{
    /// <summary>
    /// STUB — full implementation deferred to spec 005.
    ///
    /// When spec 005 merges:
    ///   - Replace this stub with the real offer panel implementation.
    ///   - Wire the Portfolio section to show all placed companies on the board.
    ///   - Each company row shows: company name, current health, cashout payout value.
    ///   - Cashout button calls <see cref="CashoutService.TryCashout"/> on confirm.
    ///   - Disable cashout button when <see cref="Pinvestor.Game.Health.CompanyHealthState.PendingCollapse"/> is true.
    /// </summary>
    public class CompanyOfferPanel : MonoBehaviour
    {
        // TODO(spec-005): inject Turn or CashoutService reference on panel open.
        // Example wiring pattern (replace with real UI binding):
        //
        //   public void Open(Turn activeTurn)
        //   {
        //       _cashoutService = activeTurn.CashoutService;
        //       PopulatePortfolioSection(activeTurn.Board);
        //   }
        //
        //   private void OnCashoutButtonClicked(BoardItemWrapper_Company company)
        //   {
        //       if (_cashoutService.TryCashout(company))
        //           RefreshPortfolioSection();
        //   }
    }
}
