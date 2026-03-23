# Tasks: Market News System

**Input**: `specs/010-market-news/spec.md`, `plan.md`
**Codebase audit required**: `Turn.cs` (RunResolutionPhase), `Round.cs`, `GameConfigDomainModels.cs`, `GameConfigExportService.cs`, booster modifier framework from spec 008, `TurnRevenueAccumulator.cs` from spec 004

## Phase 1: News Config Data

- [ ] T001 Read `Assets/Scripts/GameConfig/Runtime/Json/GameConfigJsonDtos.cs`, `GameConfigDomainModels.cs`, `GameConfigMapper.cs`, and `GameConfigExportService.cs` fully before writing any code — follow the pattern from existing config sections
- [ ] T002 Add `MarketNewsConfigSection` domain model to `GameConfigDomainModels.cs`: list of `MarketNewsEventConfig` entries
- [ ] T003 Add `MarketNewsEventConfig` model: `eventId`, `displayName`, `description`, `effectType` (enum), `targetType` (Global / SpecificIndustry), `durationTurns` (1 or 2), `triggerMode` (RandomChance / FixedInterval / BoardCondition), `triggerChance` (float), `triggerInterval` (int), modifier value fields
- [ ] T004 [P] Add corresponding DTOs to `GameConfigJsonDtos.cs`
- [ ] T005 [P] Update `GameConfigMapper.cs` for news event DTO → domain model mapping
- [ ] T006 Update `GameConfigExportService.cs` to serialize market news section
- [ ] T007 Create `Assets/Scripts/GameConfig/Editor/DomainEditors/MarketNewsConfigDomainEditor.cs`
- [ ] T008 Wire `MarketNewsConfigDomainEditor` into `GameConfigEditorWindow.cs`
- [ ] T009 Author all 6 news event config entries in GameConfig authoring asset, export to `game-config.json`

**Checkpoint**: News event data round-trips through editor → JSON → runtime

---

## Phase 2: Temporary Modifier Support

- [ ] T010 Review `IRunModifierEffect.Remove()` in the spec 008 framework — confirm it is implemented for all effect types used by news events (RPH multiplier, op-cost multiplier, health modifier)
- [ ] T011 If any relevant effect type lacks a `Remove()` implementation, add it now. Log a warning if `Remove()` is called on an effect that was never applied.
- [ ] T012 Add an industry-filter parameter to the RPH modifier: `SynergyRphModifier` or a new `IndustryRphModifier` that applies only to companies matching a given `IndustryTag`. This is required for industry-specific news effects.

**Checkpoint**: All modifier types used by news effects support both Apply and Remove; industry filtering is supported

---

## Phase 3: ActiveMarketNewsState & MarketNewsResolver

- [ ] T013 Create `Assets/Scripts/Game/MarketNews/ActiveMarketNewsState.cs` — holds current `MarketNewsEventConfig` (nullable) and `TurnsRemaining` counter
- [ ] T014 Create `Assets/Scripts/Game/Events/MarketNewsStartedEvent.cs` and `MarketNewsExpiredEvent.cs` — EventBus events with event name, target description, and duration
- [ ] T015 Create `Assets/Scripts/Game/MarketNews/MarketNewsResolver.cs` with `OnResolutionEnd()` method:
  - Step 1: If active event exists, decrement `TurnsRemaining`. If 0 → call `Remove()` on effect, clear state, emit `MarketNewsExpiredEvent`
  - Step 2: Evaluate trigger conditions for a new event. If triggered → randomly select from pool (weight by trigger chance), resolve industry target if SpecificIndustry, call `Apply()`, set state, emit `MarketNewsStartedEvent`
  - Guard: if no companies of targeted industry exist on board, skip the event gracefully
- [ ] T016 Wire `MarketNewsResolver.OnResolutionEnd()` into `Assets/Scripts/Game/Turn.cs` `RunResolutionPhase()` — call after collapse handling and economy resolution

**Checkpoint**: News events trigger, persist for correct duration, and expire cleanly

---

## Phase 4: Turn Interval & Board Condition Triggers

- [ ] T017 Implement `RandomChance` trigger in `MarketNewsResolver`: roll `Random.value` against `triggerChance` each resolution
- [ ] T018 Implement `FixedInterval` trigger: track turn count in `MarketNewsResolver`, trigger when `turnCount % triggerInterval == 0`
- [ ] T019 Implement `BoardCondition` trigger: check if any industry has `>= N` companies on board (N from config); if so, target that industry specifically
- [ ] T020 Ensure only one trigger mode is evaluated per resolution and at most one event activates per turn

**Checkpoint**: All three trigger modes fire at correct times

---

## Phase 5: 6 News Event Implementations

- [ ] T021 Create `Assets/Scripts/Game/MarketNews/Effects/` folder for all news effect classes
- [ ] T022 Implement `IndustrySurgeEffect` — Apply: register RPH ×2 modifier filtered to target industry. Remove: unregister.
- [ ] T023 Implement `RegulatoryInvestigationEffect` — Apply: register RPH ×0.5 modifier filtered to target industry. Remove: unregister.
- [ ] T024 Implement `SupplyChainDisruptionEffect` — Apply: register op-cost +50% global modifier in `EconomyService`. Remove: unregister.
- [ ] T025 Implement `MarketCrashEffect` — Apply: register global RPH ×0.5 modifier. Remove: unregister.
- [ ] T026 Implement `FundingBoomEffect` — Apply: grant +1 health to all companies currently on board matching target industry (direct call to `CompanyHealthState.GrantBonusHealth(1)` for each). No Remove needed (health grant is permanent; duration just controls when it applies).
- [ ] T027 Implement `HostileAcquisitionEffect` — Apply: randomly select one company on board matching target industry; call `CompanyHealthState.TakeDamage(2)` on it. Guard: if no matching companies, skip and log. No Remove needed (damage is immediate).

**Checkpoint**: All 6 news events apply their effects correctly; industry filtering works; edge cases guarded

---

## Phase 6: News Banner UI

- [ ] T028 Create `Assets/Scripts/UI/MarketNews/MarketNewsBannerPanel.cs` — subscribes to `MarketNewsStartedEvent` and `MarketNewsExpiredEvent`; displays event name, description, target, and duration
- [ ] T029 Banner auto-dismisses after 3 seconds or on player tap (non-blocking — does NOT pause turn flow)
- [ ] T030 On expiry banner: show "Event ended: [name]" with 2-second auto-dismiss

**Checkpoint**: Player is informed of news trigger and expiry without the game pausing

---

## Phase 7: Tests & Validation

- [ ] T031 [P] Add EditMode test for GameConfig round-trip: news event data authored → exported → parsed → domain model matches
- [ ] T032 [P] Add EditMode test for `MarketNewsResolver`: duration countdown and expiry, RandomChance trigger math, FixedInterval trigger, graceful skip when no matching industry companies
- [ ] T033 [P] Add EditMode test for each news effect: Apply produces correct modifier; Remove undoes it; FundingBoom and HostileAcquisition apply immediate effects correctly
- [ ] T034 Manual smoke test: configure a news event with 100% trigger chance; verify it fires on first resolution, persists for stated duration, expires, and a new one can trigger immediately after
