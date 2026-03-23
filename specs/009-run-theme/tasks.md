# Tasks: Run Theme System

**Input**: `specs/009-run-theme/spec.md`, `plan.md`
**Codebase audit required**: `GameConfigDomainModels.cs`, `GameConfigJsonDtos.cs`, `GameConfigMapper.cs`, `GameConfigExportService.cs`, `GameConfigEditorWindow.cs`, `RunCycleSettings.cs`, `GameManager.cs`, booster modifier framework from spec 008

## Phase 1: Theme Config Data

- [ ] T001 Read `Assets/Scripts/GameConfig/Runtime/Json/GameConfigJsonDtos.cs`, `GameConfigDomainModels.cs`, `GameConfigMapper.cs`, and `GameConfigExportService.cs` fully before writing any code — follow the exact pattern used for existing config sections
- [ ] T002 Add `RunThemeConfigSection` domain model to `Assets/Scripts/GameConfig/Runtime/Models/GameConfigDomainModels.cs`: list of `RunThemeConfigModel` entries (id, name, description, `List<RunThemeModifierEntry>`)
- [ ] T003 Add `RunThemeModifierEntry` model: `modifierType` (enum: HealthFlat, RphMultiplier, OpCostMultiplier, SynergyRphMultiplier), `value` (float), `targetType` (Global)
- [ ] T004 [P] Add corresponding DTOs to `Assets/Scripts/GameConfig/Runtime/Json/GameConfigJsonDtos.cs`
- [ ] T005 [P] Update `Assets/Scripts/GameConfig/Runtime/Mapping/GameConfigMapper.cs` to map theme DTOs → domain models
- [ ] T006 Update `Assets/Scripts/GameConfig/Editor/GameConfigExportService.cs` to serialize theme section
- [ ] T007 Create `Assets/Scripts/GameConfig/Editor/DomainEditors/RunThemeConfigDomainEditor.cs` — list of themes with editable modifier entries
- [ ] T008 Wire `RunThemeConfigDomainEditor` into `Assets/Scripts/GameConfig/Editor/GameConfigEditorWindow.cs`
- [ ] T009 Author all 5 theme config entries in the GameConfig authoring asset, export to `game-config.json`

**Checkpoint**: Theme data round-trips correctly through editor → JSON → runtime model

---

## Phase 2: ActiveRunThemeState & RunThemeService

- [ ] T010 Confirm the booster modifier framework (`IRunModifierEffect`, `GameModifierContext`) from spec 008 is in place before proceeding
- [ ] T011 Create `Assets/Scripts/Game/Themes/ActiveRunThemeState.cs` — Singleton-accessible; holds selected `RunThemeConfigModel` and its resolved modifier instances. Read-only to consumers.
- [ ] T012 Create `Assets/Scripts/Game/Themes/RunThemeService.cs` — `SelectAndApplyTheme(RunThemeConfigModel[] pool)`: randomly selects one theme, instantiates its modifier effects via `BoosterEffectFactory` (or a parallel `ThemeModifierFactory`), calls `Apply()` on each, stores in `ActiveRunThemeState`

**Checkpoint**: Theme is selected and its modifiers applied at run start via the shared framework

---

## Phase 3: Run Start Integration

- [ ] T013 Wire `RunThemeService.SelectAndApplyTheme()` into `Assets/Scripts/Game/GameFSM/GameManager.cs` `InitializeAsync()` — call after GameConfig loaded, before first turn
- [ ] T014 Create `Assets/Scripts/Game/Events/RunThemeSelectedEvent.cs` — EventBus event with selected `RunThemeConfigModel`
- [ ] T015 Emit `RunThemeSelectedEvent` from `RunThemeService` after applying the theme

**Checkpoint**: Theme is applied before turn 1 and event is emitted; modifiers are active from first turn

---

## Phase 4: Theme Reveal UI

- [ ] T016 Create `Assets/Scripts/UI/Theme/RunThemeRevealPanel.cs` — subscribes to `RunThemeSelectedEvent`; displays theme name and description; auto-dismisses after 3 seconds or on player tap
- [ ] T017 Reveal panel blocks turn start until dismissed (or auto-dismissed) — use a `UniTask.WaitUntil` or `Delay` before the first turn begins

**Checkpoint**: Player sees theme name and description before turn 1 begins

---

## Phase 5: 5 Theme Implementations

- [ ] T018 Implement `BullMarketThemeEffect` — RPH ×1.5 multiplier registered in `EconomyService`
- [ ] T019 Implement `LeanStartupThemeEffect` — subscribe to company placement event; set max health to 1 for all newly placed companies (override via `CompanyHealthState` init)
- [ ] T020 Implement `OverheadCrunchThemeEffect` — op-cost ×2 multiplier registered in `EconomyService`
- [ ] T021 Implement `ClusterEconomyThemeEffect` — synergy RPH bonus ×3 modifier; non-adjacent companies get -1 RPH (apply as a baseline RPH debuff checked in `TurnRevenueAccumulator`)
- [ ] T022 Implement `BearMarketThemeEffect` — RPH ×0.75 multiplier; reduce target worth by 20% (apply reduction to `PlayerEconomyState.TargetNetWorth` at run start)

**Checkpoint**: All 5 themes apply their modifiers correctly; effects are visible in economy calculations

---

## Phase 6: Tests & Validation

- [ ] T023 [P] Add EditMode test for GameConfig round-trip: theme data authored → exported → parsed → domain model matches
- [ ] T024 [P] Add EditMode test for `RunThemeService`: random selection from pool, modifiers applied on selection
- [ ] T025 [P] Add EditMode test for each theme effect: Apply produces correct modifier values; no effect persists without Apply
- [ ] T026 Manual smoke test: start a run, verify theme reveal panel appears with correct text; play a turn, verify economy reflects theme modifier (e.g., Bull Market shows higher RPH)
