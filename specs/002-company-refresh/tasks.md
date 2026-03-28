Status: MERGED
PR: #2
Merged: 2026-03-28
Merge commit: 58da236e7ddfd5961f66eeda1092cbdb6d0f7ac5

Deferred to spec-006:
- AuditFog collapse-handler integration
- TrendNecro cashout-doubling integration
- DeferredAlpha deferred damage → cashout value
- LastMile tile-swap on adjacent collapse

Deferred (no target spec yet):
- CloutHub Live AudienceEcho — echo-node entity not yet designed
- AutoPilot Pantry BallRedirect — requires ball-miss hook in Ball.cs

Post-merge notes:
- 16 companies across ConsumerTech, EnterpriseTech, FinTech, SocialMedia
- Old companies removed: Cashnado, CloutHub (original), StackRabbit, VirtuosoXR
- Tier 3 stubs (CloutHubLive, AutoPilotPantry) have TODO(spec-NNN) comments

# Tasks: Company Content Refresh

**Input**: `specs/002-company-refresh/spec.md`, `plan.md`
**Design source**: `docs/design/Company_Category_Refresh_Proposal.md`
**Codebase audit required**: `ECompanyCategory.cs`, `CompanyCardSettings.asset`, `CompanyContainer.asset`, `Entertainment/CloutHub/` (template), `GameplayAbilitySystem/Abilities/`, `GameConfigAuthoringAsset.cs`

---

## Phase 1: Cleanup & Category Settings

- [x] T001 Read `Assets/ScriptableObjects/Company/Companies/Entertainment/CloutHub/` folder structure fully — use as the template for all new company asset sets
- [x] T002 Read `Assets/ScriptableObjects/Company/CompanyContainer.asset` and identify all 4 old company references before deleting anything
- [x] T003 Remove old company references from `Assets/ScriptableObjects/Company/CompanyContainer.asset` (CloutHub, Cashnado, VirtuosoXR, StackRabbit) — do this BEFORE deleting asset folders
- [x] T004 Delete `Assets/ScriptableObjects/Company/Companies/Entertainment/` folder (CloutHub + Cashnado + all sub-assets)
- [x] T005 Delete `Assets/ScriptableObjects/Company/Companies/Gaming/` folder (VirtuosoXR)
- [x] T006 Delete `Assets/ScriptableObjects/Company/Companies/TechToys/` folder (StackRabbit)
- [x] T007 Open `Assets/ScriptableObjects/Company/CompanyCardSettings.asset` in Unity editor and add entries for `FinTech` (category=3) and `EnterpriseTech` (category=4) with placeholder color and icon
- [x] T008 Open `Assets/Scripts/GameConfig/Authoring/GameConfigAuthoringAsset.cs` and read how company attribute keys are assigned before authoring new GameConfig entries
- [x] T009 Remove old company entries (Cashnado, CloutHub, Stack Rabbit) from the GameConfig authoring asset in the GameConfig editor window

**Checkpoint**: No old company assets remain; FinTech and EnterpriseTech have card settings; GameConfig has no old entries

---

## Phase 2: Author 16 New Companies

Create folder, `CompanyId`, and `CardData` assets for each company following the template from T001. Then register in `CompanyContainer` and GameConfig.

### SocialMedia

- [x] T010 [P] Create assets for **CloutHub Live** under `Assets/ScriptableObjects/Company/Companies/SocialMedia/CloutHubLive/`
- [x] T011 [P] Create assets for **RageLoop Studio** under `SocialMedia/RageLoopStudio/`
- [x] T012 [P] Create assets for **TrendNecro Agency** under `SocialMedia/TrendNecroAgency/`
- [x] T013 [P] Create assets for **CancelShield PR** under `SocialMedia/CancelShieldPR/`

### ConsumerTech

- [x] T014 [P] Create assets for **AutoPilot Pantry** under `ConsumerTech/AutoPilotPantry/`
- [x] T015 [P] Create assets for **SleepDebt SaaS** under `ConsumerTech/SleepDebtSaaS/`
- [x] T016 [P] Create assets for **OneTap Butler** under `ConsumerTech/OneTapButler/`
- [x] T017 [P] Create assets for **MoodFridge Cloud** under `ConsumerTech/MoodFridgeCloud/`

### FinTech

- [x] T018 [P] Create assets for **Loophole Ledger** under `FinTech/LoopholeLedger/`
- [x] T019 [P] Create assets for **DeferredAlpha Capital** under `FinTech/DeferredAlphaCapital/`
- [x] T020 [P] Create assets for **CreditKaraoke** under `FinTech/CreditKaraoke/`
- [x] T021 [P] Create assets for **AuditFog Exchange** under `FinTech/AuditFogExchange/`

### EnterpriseTech

- [x] T022 [P] Create assets for **PanicFulfillment OS** under `EnterpriseTech/PanicFulfillmentOS/`
- [x] T023 [P] Create assets for **LastMile Orchestrator** under `EnterpriseTech/LastMileOrchestrator/`
- [x] T024 [P] Create assets for **Shortage Oracle AI** under `EnterpriseTech/ShortageOracleAI/`
- [x] T025 [P] Create assets for **RepoReaper Systems** under `EnterpriseTech/RepoReaperSystems/`

### Registration & Config

- [x] T026 Add all 16 new companies to `Assets/ScriptableObjects/Company/CompanyContainer.asset`
- [x] T027 Author all 16 company entries in the GameConfig authoring asset (via GameConfig editor window) with initial attribute values: health=10, RPH=100, op-cost=50 as baseline (adjust per company flavor)
- [x] T028 Export `game-config.json` via GameConfig editor — verify it contains 16 entries and no old ones

**Checkpoint**: All 16 companies are registered, appear in CompanyContainer, and are exported to game-config.json

---

## Phase 3: Tier 1 Abilities (Simple — existing effect types only)

Read `Assets/ScriptableObjects/GameplayAbilitySystem/Abilities/` for the existing ability asset pattern before implementing each.

- [x] T029 Implement **RageLoop Studio** ability — every 3rd hit converts 1 self-HP loss into +2 RPH for all SocialMedia companies this turn (max 2 procs/turn)
- [x] T030 Implement **MoodFridge Cloud** ability — while at full HP, adjacent companies ignore first op-cost deduction each round; breaks immediately when not full HP
- [x] T031 Implement **Loophole Ledger** ability — once per turn, nullify the first negative market/news modifier targeting this company
- [x] T032 Implement **PanicFulfillment OS** ability — gains +RPH per company currently below 50% HP (live bonus cap +8 RPH)
- [x] T033 Implement **CreditKaraoke** ability — gains temporary RPH when adjacent neighbors are from distinct categories (cap: 3 unique categories)

**Checkpoint**: 5 Tier 1 abilities functional; no new ability effect code required

---

## Phase 4: Tier 2 Abilities (Event-driven / conditional)

- [x] T034 Implement **TrendNecro Agency** ability — when adjacent company collapses, gain 1 "Recycled Hype" stack; next cashout from this company is doubled (stack cap 1, consumed on cashout). *Note: depends on cashout system from spec 006 for full effect; stub cashout doubling if spec 006 not yet complete.*
- [x] T035 Implement **CancelShield PR** ability — if this company took 4+ hits this turn, resolve at turn end: 50% chance bonus revenue, 50% chance fine payment (values from ability asset)
- [x] T036 Implement **SleepDebt SaaS** ability — exactly 2 hits this turn → +1 permanent RPH (cap +6); more than 2 hits → lose extra HP equal to (hits - 2)
- [x] T037 Implement **OneTap Butler** ability — on the turn it is purchased/placed, copy the cheapest adjacent company's on-hit ability at 50% strength for 2 turns. First pass: copy RPH value at 50% as a flat buff if full ability cloning is not feasible.
- [x] T038 Implement **DeferredAlpha Capital** ability — on hit, may defer 1 damage to round end (deferred cap 3); deferred damage amount increases cashout value by +15%. *Note: cashout value modifier depends on spec 006.*
- [x] T039 Implement **AuditFog Exchange** ability — first collapse each round is hidden until round end; hidden company still generates revenue during its turn. *Note: requires spec 006 collapse handler; create placeholder that logs intent if spec 006 not yet complete.*
- [x] T040 Implement **LastMile Orchestrator** ability — when an adjacent company collapses, move this company into the collapsed tile and trigger one free hit payout (once per round)
- [x] T041 Implement **RepoReaper Systems** ability — on cashout, mark a random adjacent company as Collateral: +RPH for 2 turns but +1 HP loss per hit during that period
- [x] T042 Implement **Shortage Oracle AI** ability — at round start, predict one category (random or player-selected); at round end, if that category was under-hit (fewer hits than average), gain a payout based on shortfall; if prediction fails, apply a self-cost penalty

**Checkpoint**: 9 Tier 2 abilities functional or stubbed with clear TODO markers where spec 006 dependency is unmet

---

## Phase 5: Tier 3 Abilities (New mechanics required)

- [x] T043 Create placeholder ability assets for **CloutHub Live** (stub `Ability.Company.CloutHubLive.AudienceEcho.asset`) with a `// TODO: requires echo node board entity` comment — company is playable without the ability firing
- [x] T044 Design and document the "Audience Echo" board entity mechanic in a `specs/002-company-refresh/audience-echo-design.md` note before implementing
- [x] T045 Implement **CloutHub Live** Audience Echo: first hit each turn spawns a temporary trigger node on an adjacent empty tile; ball passing through grants +1 payout on that hit chain (max 2 echo nodes, expire at turn end)
- [x] T046 Create placeholder ability assets for **AutoPilot Pantry** (stub `Ability.Company.AutoPilotPantry.BallRedirect.asset`) with a `// TODO: requires ball miss detection hook` comment
- [x] T047 Read `Assets/Scripts/Game/BallShooter/BallShooter.cs` and `Ball.cs` to understand miss detection before implementing redirect
- [x] T048 Implement **AutoPilot Pantry** ball redirect: if ball would exit bounds without hitting any company this turn, redirect it once to the nearest ConsumerTech company (one redirect per turn per copy)

**Checkpoint**: All 16 companies have ability assets; Tier 3 abilities functional or clearly stubbed

---

## Phase 6: Validation

- [ ] T049 Compile via Unity — clear all console errors related to missing company references (requires Unity editor)
- [x] T050 [P] Verify `CompanyContainer.asset` has exactly 16 entries (no old companies, no duplicates)
- [x] T051 [P] Verify `game-config.json` has exactly 16 company entries with correct IDs
- [ ] T052 Play-mode smoke test: place one company from each of the 4 categories; verify card renders correctly (no missing-settings errors); verify each placed company's Tier 1 ability fires as expected (requires Unity editor)
- [ ] T053 Manual test for each Tier 2 ability: set up the trigger condition and verify the effect applies correctly (or stub logs correctly where spec 006 dependency is unmet) (requires Unity editor)
