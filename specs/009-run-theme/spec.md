# Spec 009: Run Theme System

**Status**: Planned
**Created**: 2026-03-24
**Dependencies**: Spec 001 (GameConfig — theme data), Spec 004 (economy — RPH/cost modifier hooks), Spec 007 (synergy — adjacency modifier hooks), Spec 008 (booster modifier framework — shared modifier infrastructure)

## Overview

Implement Run Themes: macro rule modifiers selected at run start that are active for the entire run. Themes are known upfront, define the run's identity, and interact with all core systems. This spec also delivers the first set of themes.

Themes share the same modifier framework established by the Booster Shop (spec 008) — this spec should extend, not duplicate, that infrastructure.

## Codebase Audit (check before implementing)

Agents **must** read these files before writing any code:

- `Assets/Scripts/GameConfig/Runtime/Models/GameConfigDomainModels.cs` — check for any existing run theme / modifier models
- `Assets/Scripts/GameConfig/Editor/DomainEditors/RunCycleConfigDomainEditor.cs` — existing run cycle editor; theme selection may extend this
- `Assets/Scripts/Game/GameFSM/GameManager.cs` — run start is here; theme must be applied at initialization
- `Assets/Scripts/Game/RunCycle/RunCycleSettings.cs` — run settings ScriptableObject; theme reference may be added here
- Booster modifier framework files from Spec 008 — theme modifiers must reuse this, not create a parallel stack
- `Assets/Scripts/GameplayAbilitySystem/` — check for global effect application patterns

## User Stories

### US1 — Theme Selected at Run Start (P1)

As the game, one Run Theme must be selected at the beginning of each run so the player knows the macro conditions they are playing under.

**Acceptance criteria:**
- One theme is active per run
- Theme is determined at run start (random from pool for now; player selection is a future feature)
- Active theme is displayed to the player at run start (a simple modal/banner is sufficient)
- Theme remains active and unchanged for the entire run

### US2 — Theme Applies Macro Modifiers (P1)

As the game systems, a Run Theme modifies one or more game rules globally for the entire run so each run has a distinct structural identity.

**Acceptance criteria:**
- Theme modifiers are applied at run initialization (before first turn)
- Modifier types supported:
  - Health modifier (starting health ± N for all companies)
  - RPH modifier (global RPH multiplier or flat bonus)
  - Operational cost modifier (global op-cost multiplier or flat change)
  - Adjacency rule modifier (e.g., disable synergy, invert synergy, enable diagonal adjacency)
- All modifier values come from the theme's config data (not hardcoded)
- Modifiers stack correctly with booster modifiers (no double-application)

### US3 — Implemented Theme Set (P1)

A first set of themes must be implemented and functional to demonstrate the system.

**Required themes (minimum 5):**

| Name | Modifier(s) | Identity |
|---|---|---|
| **Bull Market** | Global RPH ×1.5 | Everything earns more — but so do the stakes |
| **Lean Startup** | All companies start with 1 health | Build with disposable companies or protect them with boosters |
| **Overhead Crunch** | Operational costs ×2 | Efficiency matters; idle companies bleed you dry |
| **Cluster Economy** | Same-industry RPH synergy bonus ×3, but non-adjacent companies have -1 RPH | Go deep or go home |
| **Bear Market** | Global RPH ×0.75, target net worth reduced by 20% | Lower ceiling, lower bar — a more forgiving crawl |

**Acceptance criteria for each theme:**
- Effect matches description
- Values are config-driven
- Theme name and description shown at run start

### US4 — Theme Data in GameConfig (P1)

As the config system, Run Themes must be authored and exported through the existing GameConfig pipeline so designers can add or modify themes without code changes.

**Acceptance criteria:**
- Theme data is part of `GameConfigRoot` (new section or part of existing run cycle section)
- `GameConfigEditorWindow` exposes a theme domain editor
- Theme data exports to `game-config.json` correctly
- `GameConfigManager` provides a theme accessor at runtime

## Scope

**In scope:**
- `RunThemeConfigModel` (domain model) and corresponding DTOs
- Theme domain editor in `GameConfigEditorWindow`
- `ActiveRunThemeState` runtime model: holds selected theme + its resolved modifier values
- Theme modifier application at run start (extends booster modifier framework)
- Run-start theme reveal UI (simple modal showing theme name + description)
- 5 themes implemented and authored

**Out of scope:**
- Player-selectable themes at run start (random selection only for now)
- Theme-specific visual identity (colour palettes, music changes)
- Themes that require Market News (spec 010) features
- Mid-run theme changes

## Key Entities

| Entity | Type | Purpose |
|---|---|---|
| `RunThemeConfigModel` | Config domain model | Theme name, description, modifier list |
| `RunThemeModifierEntry` | Config model | Modifier type enum + value (used by theme and booster framework) |
| `ActiveRunThemeState` | Runtime model | Currently active theme + resolved modifiers |
| `RunThemeService` | Runtime service | Applies theme modifiers at run start, exposes active theme |
| `RunThemeRevealPanel` | UI View | Simple modal shown at run start with theme name + description |

## Notes

- The modifier framework from spec 008 (booster effects) must be generalized enough to accept theme modifiers — this spec should not create a separate modifier stack
- Theme modifier values must not conflict with booster modifier values by overwriting each other — design a composable modifier chain (additive/multiplicative layers)
- For the "Lean Startup" theme, the health modifier applies at the moment a company is placed (not retroactively mid-turn)
- Theme selection randomness should use a seeded source for reproducibility
