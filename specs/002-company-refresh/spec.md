# Spec 002: Company Content Refresh

**Status**: Planned
**Created**: 2026-03-24
**Design Source**: `docs/design/Company_Category_Refresh_Proposal.md`
**Dependencies**: Spec 001 (GameConfig pipeline — company authoring and export)

## Overview

Replace the 4 placeholder companies (CloutHub, Cashnado, VirtuosoXR, StackRabbit) with the 16 fully-designed companies from the Category Refresh Proposal. This covers asset removal, new ScriptableObject authoring, GameConfig entries, category settings, and ability implementation for each company.

`ECompanyCategory.cs` is already updated (SocialMedia=1, ConsumerTech=2, FinTech=3, EnterpriseTech=4). The enum enum IDs for existing serialized assets are stable.

## Codebase Audit (check before implementing)

Agents **must** read these files before writing any code:

- `Assets/Scripts/Company/ECompanyCategory.cs` — already updated; do not modify
- `Assets/ScriptableObjects/Company/CompanyCardSettings.asset` — needs FinTech and EnterpriseTech entries added
- `Assets/ScriptableObjects/Company/CompanyContainer.asset` — tracks all registered companies; update on add/remove
- `Assets/ScriptableObjects/Company/Companies/Entertainment/CloutHub/` — reference for existing company asset structure to replicate
- `Assets/ScriptableObjects/GameplayAbilitySystem/Abilities/` — existing ability asset patterns
- `Assets/Resources/GameConfig/game-config.json` — has 3 old company entries (Cashnado, CloutHub, Stack Rabbit); replace via GameConfig editor
- `Assets/Scripts/GameConfig/Authoring/GameConfigAuthoringAsset.cs` — authoring source for GameConfig export
- `docs/design/Company_Category_Refresh_Proposal.md` — canonical design source for all 16 companies

## User Stories

### US1 — Remove Old Companies (P1)

As the project, the 4 placeholder companies must be fully removed so no stale data or broken references remain in the build.

**Companies to remove:**
- `Entertainment/CloutHub` (+ abilities subfolder)
- `Entertainment/Cashnado`
- `Gaming/VirtuosoXR`
- `TechToys/StackRabbit`

**Acceptance criteria:**
- All `.asset` files and folders for the 4 old companies deleted
- `CompanyContainer.asset` no longer references them
- `game-config.json` no longer contains their entries (re-export via GameConfig editor)
- No console errors about missing company references at runtime

### US2 — Category Settings for FinTech and EnterpriseTech (P1)

As the UI, FinTech and EnterpriseTech need settings entries in `CompanyCardSettings.asset` so their cards render without missing-setting errors.

**Acceptance criteria:**
- `CompanyCardSettings.asset` has entries for `FinTech` (category=3) and `EnterpriseTech` (category=4)
- Each entry has a color and icon assigned (placeholder values are acceptable for now)
- No `Widget_CompanyCard` errors for companies of these categories

### US3 — Author 16 New Companies as ScriptableObjects (P1)

As the game, each of the 16 new companies must exist as a set of Unity assets matching the structure of existing companies so they can be placed, rendered, and driven by the ability system.

**Per-company asset set required:**
- `CompanyId.[Name].asset` — unique company identifier
- `CardData.Company.[Name].asset` — card data (name, category, base attributes)
- Ability assets per company (see US4)

**Folder structure:**
```
Assets/ScriptableObjects/Company/Companies/
  SocialMedia/CloutHubLive/
  SocialMedia/RageLoopStudio/
  SocialMedia/TrendNecroAgency/
  SocialMedia/CancelShieldPR/
  ConsumerTech/AutoPilotPantry/
  ConsumerTech/SleepDebtSaaS/
  ConsumerTech/OneTapButler/
  ConsumerTech/MoodFridgeCloud/
  FinTech/LoopholeLedger/
  FinTech/DeferredAlphaCapital/
  FinTech/CreditKaraoke/
  FinTech/AuditFogExchange/
  EnterpriseTech/PanicFulfillmentOS/
  EnterpriseTech/LastMileOrchestrator/
  EnterpriseTech/ShortageOracleAI/
  EnterpriseTech/RepoReaperSystems/
```

**Acceptance criteria:**
- All 16 companies have their asset sets created
- `CompanyContainer.asset` references all 16
- `game-config.json` contains entries for all 16 (exported via GameConfig editor)

### US4 — Implement Company Abilities (P1)

Each company has a unique rule-bending ability from the design doc. All 16 must be implemented and functional.

**Ability complexity tiers:**

**Tier 1 — Simple (RPH/stat modifier, existing ability effect types):**
| Company | Ability Summary |
|---|---|
| RageLoop Studio | Every 3rd hit → +2 RPH for all SocialMedia this turn |
| MoodFridge Cloud | While at full HP, adjacent companies ignore first op-cost deduction per round |
| Loophole Ledger | Once per turn, nullify first negative market/news modifier on this company |
| PanicFulfillment OS | Gains RPH per company currently below 50% HP |
| CreditKaraoke | Gains temporary RPH when adjacent neighbors are from distinct categories |

**Tier 2 — Moderate (event-driven, conditional effects):**
| Company | Ability Summary |
|---|---|
| TrendNecro Agency | When adjacent company collapses → next cashout from this company is doubled (stack cap 1) |
| CancelShield PR | If took 4+ hits this turn → bonus revenue or fine at turn end |
| SleepDebt SaaS | Exactly 2 hits → +1 permanent RPH; over-hit → extra HP loss |
| OneTap Butler | On purchase turn → copy adjacent company's on-hit ability at 50% for 2 turns |
| DeferredAlpha Capital | On hit → may defer 1 damage to round end; deferred damage increases cashout value +15% |
| AuditFog Exchange | First collapse each round is hidden until round end; hidden company still pays out |
| LastMile Orchestrator | When adjacent company collapses → move into its tile, trigger one free hit payout |
| RepoReaper Systems | Cashinout marks random adjacent company as Collateral: +RPH but extra HP loss on hit (2 turns) |
| Shortage Oracle AI | At round start, predict one category; if under-hit → gain payout based on shortfall |

**Tier 3 — Complex (requires new ability mechanics):**
| Company | Ability Summary |
|---|---|
| CloutHub Live | First hit each turn spawns "Audience Echo" on adjacent tile; passing through grants +1 payout on hit chain (max 2 echoes, expire end of turn) |
| AutoPilot Pantry | If ball would miss all companies this turn → redirect once to nearest ConsumerTech company |

**Acceptance criteria:**
- All 16 abilities implemented and functional in play-mode
- Tier 1 and Tier 2 abilities completed first; Tier 3 may follow as a separate pass
- Ability effect values are authored in ability assets (not hardcoded)
- Guardrails from the design doc are enforced (caps, once-per-turn limits, etc.)

## Scope

**In scope:**
- Delete old company asset folders (4 companies)
- Update `CompanyContainer.asset` and `CompanyCardSettings.asset`
- Create 16 new company asset sets
- Author 16 companies in GameConfig authoring asset and re-export
- Implement all 16 abilities (Tier 1 + 2 in first pass, Tier 3 in second pass)

**Out of scope:**
- Company card visual art / illustrations (placeholder colors/icons only)
- Balance tuning (attribute values are initial estimates, not final)
- Cashout system mechanics (spec 006) — the cashout-doubling ability (TrendNecro, DeferredAlpha) depends on spec 006 being implemented first

## Notes

- Tier 3 abilities (CloutHub Live, AutoPilot Pantry) require new ability mechanics not present in the current system. They should be scoped as a second implementation pass with explicit new ability effect types authored and tested separately.
- `OneTap Butler`'s "copy adjacent ability" mechanic is complex and may require runtime ability cloning — evaluate feasibility before committing to this exact behavior; a simplified version (copy RPH value at 50%) is an acceptable first pass.
- `AuditFog Exchange`'s hidden collapse requires coordination with spec 006 (collapse handler) — implement after spec 006 is in place.
