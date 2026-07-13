# Pinvestor — TODO & MVP Roadmap

> Created 2026-07-14 from a full audit of specs/, docs/design/, game-config.json, and Assets/Scripts.
> Goal: a **balanced, polished MVP playthrough** — complete run loop, working roster, real end-of-run UX.
> Update this doc as work merges; it is the single source of truth for "what's next."

---

## Current State (audit summary, 2026-07-14)

**Working end-to-end:** 4-phase turn cycle (Offer → Placement → Launch → Resolution), round/run cycle
with `RequiredWorth` gates, config-driven company placement, collapse detection, economy resolution
(with unit tests), offer/selection UI with DOTween polish, balance + round/turn/target HUD, HP bars,
pooled floating text.

**Specs:** 001–006 implemented/merged. 007 (Synergy), 008 (Booster Shop), 009 (Run Theme),
010 (Market News) — 0% built. 008 blocks 009 and 010.

**Biggest problems:**
1. Run ends with a `Debug.Log` and a silent 2s scene reload — no win/lose/summary UX at all.
2. Cashout backend (`CashoutService.TryCashout`) is fully implemented but has **zero callers** —
   the player cannot cash out. `CompanyOfferPanel.cs` is an empty stub.
3. No `PurchaseCost`/`Valuation` attribute — `TurnlyCost` does triple duty (price proxy, per-turn
   drain, cashout basis). Cashout value = 0.5 × TurnlyCost, which is always worse than one good
   hit (RPH is 1.5–2.2× TurnlyCost) → the headline "bubble economy" mechanic is dead weight.
4. The missing "spec-006 collapse handler" blocks the payoffs of 4 companies at once
   (TrendNecro, DeferredAlpha, AuditFog, LastMile).
5. Shop phase is a literal no-op (`ShopPlaceholderRoundPhase` → `return UniTask.CompletedTask`).
6. Zero audio, no VFX/screen-shake; only juice is card animations + floating text.
7. 8 of 16 companies have stubbed, unwired, or silently-reworked abilities.

---

## Milestone 1 — Complete the Run Loop (make it *feel* like a game)

- [x] **Win / Lose / Run Summary screen** — `RunSummaryUI` + `P_UI_RunSummary` scene panel;
      GameManager raises `ShowRunSummaryEvent` (win/loss, worth vs target, rounds survived,
      placed/collapsed/cashed-out stats via `RunStatsTracker`) and waits for
      `RunSummaryDismissedEvent` (Play Again button) before reloading. *(2026-07-14)*
- [ ] **Wire the GameFSM** — `GetTransitionDict()` is empty; either drive Win/GameOver states
      properly or delete the dead FSM states to avoid confusion.
- [x] **Cashout UI** (spec 006 T019–T021): `CompanyOfferPanel` implemented as Portfolio panel
      (`P_UI_Portfolio`) shown during offer phase — rows w/ name, HP, payout, cashout button
      (disabled on `PendingCollapse`) → `Turn.CashoutService.TryCashout()`. *(2026-07-14 —
      needs one manual playtest of the cashout click with a placed company)*
- [x] **Round transition feedback** — `RoundBannerUI` (`P_UI_RoundBanner`) shows
      "ROUND N / Target $X" with fade in/out; `Round.ExecuteAsync` waits a 1.6s beat. *(2026-07-14)*

## Milestone 2 — Economy Integrity (fix before any balance tuning)

- [x] **Dedicated `purchaseCost` config value** per company (company `values` section, key in
      `CompanyConfigValueKeys` + `TryGetPurchaseCost`); first pass = 1.5 × TurnlyCost rounded
      to 5 ($60–$90). Placement now debits the cost from Balance (`Turn.ApplyPurchaseCost`).
      Offer cards show "Cost $X". *(2026-07-14)*
- [x] **Cashout worth doing**: `CompanyValuationModel` now appreciates —
      `CashoutValue = PurchaseCost × cashout_rate + LifetimeRevenue × valuation_appreciation_rate`
      (new balance key, 0.3); wrapper feeds `RevenueGenerator` hits into the model. *(2026-07-14)*
- [ ] **Balance/playtest pass on the new economy** — purchase costs + turnly costs vs round
      `RequiredWorth` targets were NOT retuned; verify a competent run can still hit targets
      (watch: bankruptcy spiral, whether $500 initial capital is enough, whether 0.3
      appreciation makes late cashouts dominant).
- [ ] **Re-run balance pass on all 16 companies** after the above (outliers:
      CancelShieldPR ratio 1.45 w/ 4-hit trigger, DeferredAlpha worst cost w/ inert payoff,
      TrendNecro best ratio w/ inert payoff).

> ⚠️ Note: `GameConfigAuthoring.asset` was missing `companyCategory` + balance values while
> `game-config.json` had them (the JSON had drifted from the authoring asset). The asset has
> been backfilled and is authoritative again — always edit the asset and re-export, never the JSON.

## Milestone 3 — Company Roster to "all 16 real" (or cut to MVP-8)

**Highest-leverage single item:**
- [ ] **Build the shared collapse handler** (referenced as "spec-006 collapse handler" in 4 ability
      SOs). Unblocks in one pass:
  - [ ] AuditFogExchange — hidden/deferred collapse (currently pure stub)
  - [ ] TrendNecroAgency — Recycled Hype stacks → cashout multiplier (tracked, payoff unwired)
  - [ ] DeferredAlphaCapital — +15% cashout value per deferred point (tracked, payoff unwired)
  - [ ] LastMileOrchestrator — tile repositioning on adjacent collapse (payout works, move doesn't)

**Separate systems needed:**
- [ ] AutoPilotPantry `BallRedirect` — needs ball-miss-detection hook in `Ball.cs`
- [ ] CloutHubLive `AudienceEcho` — needs temporary echo-node board entity type

**Silent reworks to reconcile with design doc (decide: fix code or fix doc):**
- [ ] CreditKaraoke — shipped as passive "+RPH per unique adjacent category" instead of the
      designed active neighbor-juicing extraction loop (breaks FinTech late-game combo line)
- [ ] OneTapButler — copies cheapest neighbor's RPH once on placement instead of round-by-round
      ability cloning

**MVP fallback if roster work slips:** ship with the 8 fully-working companies
(RageLoopStudio, CancelShieldPR, SleepDebtSaaS, MoodFridgeCloud, LoopholeLedger,
PanicFulfillmentOS, ShortageOracleAI, RepoReaperSystems — 2 per category) and gate the rest
behind implementation.

## Milestone 4 — Synergy System (spec 007, 0/19 tasks)

- [ ] Adjacency service, RPH-bonus synergy evaluator, cluster detection for health bonus
      (config fields `SynergyRphBonus` / `SynergyClusterHealthBonus` already exist)
- [ ] Synergy UI feedback — placement preview highlighting + active-synergy indicators
      (without this the system is invisible; treat UI as part of the spec, not polish)

## Milestone 5 — Booster Shop (spec 008, 0/33 tasks — blocks 009/010)

- [ ] `IRunModifierEffect` / `GameModifierContext` / `BoosterEffectFactory` modifier framework
- [ ] 8 booster SOs (Overclock, DeadCatBounce, VultureFund, SkeletonCrew, MarginCall,
      Diversification, TooBigToFail, HotMoney)
- [ ] Shop service + shop-phase UI (replaces `ShopPlaceholderRoundPhase` no-op)

## Milestone 6 — Juice & Polish Pass

- [ ] **Audio**: SoundManager + SFX for ball launch, hits, revenue, collapse, cashout, win/lose,
      UI clicks; light music/ambience. (Currently ZERO audio in the project.)
- [ ] **VFX**: hit impact particles, collapse effect, revenue burst, cashout effect
- [ ] **Camera/screen shake** on collapse and big hits
- [ ] **HUD juice**: balance ticker roll-up, target-bar fill animation, turn-phase indicator
- [ ] Floating text pass — differentiate revenue / damage / cashout / cost visually
- [ ] `VM_Game` is an empty class — either give it real bindings or document the widget-direct
      pattern as intentional

## Milestone 7 — Post-MVP (specs 009/010, after 008)

- [ ] Run Theme system (spec 009, 26 tasks — depends on 008's modifier framework)
- [ ] Market News system (spec 010, 34 tasks — depends on 008 + 004's TurnRevenueAccumulator);
      also finally stress-tests PanicFulfillmentOS / RepoReaperSystems "stressed board" fantasy

---

## Debt & Hygiene

- [ ] Spec 001: 8 missing EditMode tests (T020, T021, T030, T031, T037, T037a, T038, T043),
      missing `data-model.md` / `quickstart.md`, manual E2E validation (T055), constitution
      review (T056)
- [ ] Manual editor smoke tests outstanding: 002 T049/T052/T053, 003 T012/T013, 004 T017,
      005 T026, 006 T025
- [ ] Minor stubs: `BoardVisualController.Dispose`, `BoardItemProperty_PlacableBase.Remove()`
- [ ] Refresh `skills/game-element-designer/references/content-catalog.md` (stale — dated
      2026-03-22, pre-dates the 16-company roster; run
      `./skills/game-element-designer/scripts/refresh-content-catalog.sh`)
- [ ] Delete stale merged remote branches: `feat/spec-002-company-refresh`,
      `feat/spec-004-economy-resolution`, `feat/spec-006-health-collapse-cashout`
- [ ] Audit whether spec-002's deferred-to-006 items actually landed in the 006 merge
      (AuditFog / TrendNecro / DeferredAlpha / LastMile — evidence says they did NOT)

---

## Suggested MVP Definition

A player can: start a run → see the theme/target → pick from offers → place companies →
launch balls with satisfying feedback → watch revenue/damage with sound and VFX → cash out
companies strategically before they collapse → shop for a booster between rounds → win or lose
against the target → see a run summary → play again.

**Minimum scope = Milestones 1–3 + Milestone 6 (audio/VFX subset) + Milestone 5 (shop).**
Synergy (M4) strongly recommended for depth; Run Themes / Market News (M7) explicitly post-MVP.
