# Plan: Run Theme System

**Spec**: `specs/009-run-theme/spec.md`
**Created**: 2026-03-24

## Technical Approach

Run Themes are authoring data in GameConfig (new `RunThemeConfigSection`) exposed through the existing editor pipeline. At run start, `GameManager` selects a theme randomly, applies it via `RunThemeService` using the `IBoosterEffect` framework from spec 008, and emits a `RunThemeSelectedEvent`. A lightweight reveal panel shows the theme before the first turn. Themes are permanent-for-run modifiers and reuse the same modifier chain as boosters — no separate stack.

## Architecture Decisions

### Themes Reuse IBoosterEffect Framework from Spec 008
Theme modifiers implement `IBoosterEffect` (or a shared `IRunModifierEffect` interface that `IBoosterEffect` also implements). They are applied once at run start and are never removed during the run. This is the intended generalization of the modifier framework.

**Do not build a separate theme modifier stack.** If spec 008 isn't complete, build the modifier framework in spec 008 and reference it here.

### Theme Data in GameConfig — New Section
`RunThemeConfigSection` added to `GameConfigRoot`. Fields per theme: id, name, description, `List<RunModifierEntry>`. `RunModifierEntry` has: `modifierType` (enum: HealthFlat, RphMultiplier, OpCostMultiplier, SynergyRphMultiplier, etc.), `value`, `targetType` (Global / SpecificIndustry).

This data is exported by the existing `GameConfigExportService` after adding the new DTO and mapper entries.

### Theme Selection at GameManager Init
`GameManager.InitializeAsync()` (or `PlayConfiguredRunAsync()`) calls `RunThemeService.SelectAndApplyTheme()` before the first turn. Selection is random from the GameConfig pool using a seeded RNG (same seed source as the rest of the game).

### ActiveRunThemeState — Accessible but Read-Only
`ActiveRunThemeState` is a singleton-accessible runtime model holding the selected theme's config and resolved modifier values. UI reads from it. Other systems (synergy, economy) don't need to read it directly — their modifier values are already baked into the modifier chain by `RunThemeService.Apply()`.

### Theme Reveal Panel — Non-Blocking Modal
Shown after `SelectAndApplyTheme()` and before the first turn. Auto-advances after 3 seconds or on player tap. Uses a `UniTask.WaitUntil` or simple timer — not a blocking await.

## Phase Breakdown

### Phase 1: Theme Config Data
Add `RunThemeConfigSection` + DTOs to GameConfig pipeline. Update `GameConfigMapper` and `GameConfigExportService`. Add `RunThemeConfigDomainEditor` to `GameConfigEditorWindow`. Author 5 theme ScriptableObjects (or config entries).

### Phase 2: ActiveRunThemeState & RunThemeService
`ActiveRunThemeState` runtime model. `RunThemeService.SelectAndApplyTheme()` — random selection, applies modifiers via spec 008 framework.

### Phase 3: Run Start Integration
Wire `RunThemeService` into `GameManager.InitializeAsync()` / `PlayConfiguredRunAsync()`. Apply before first turn.

### Phase 4: Theme Reveal UI
`RunThemeRevealPanel`: shows theme name + description. Auto-advances or tap-to-dismiss. `RunThemeSelectedEvent` on EventBus drives panel open.

### Phase 5: 5 Theme Implementations
Implement modifier effects for each theme using `IBoosterEffect` / `IRunModifierEffect`. Validate each against the modifier chain.

### Phase 6: Tests & Validation
EditMode tests: theme selection from pool, modifier application, GameConfig round-trip. Manual smoke test for each theme.

## Key Risks

- **Modifier framework dependency**: This spec cannot be fully implemented until spec 008's `IBoosterEffect` / modifier framework is in place. If implementing out of order, build the framework interfaces first in a shared location.
- **Theme modifiers + booster modifiers stacking**: "Cluster Economy" theme sets `SynergyRphMultiplier ×3`. If a booster also modifies synergy RPH, the stacking order must be defined. Establish the modifier chain order in spec 008's plan and follow it here.
- **GameConfig pipeline extension**: Adding a new config section requires touching DTOs, mapper, export service, and editor. Follow the existing pattern from spec 001 exactly.
