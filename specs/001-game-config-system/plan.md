# Implementation Plan: Global Game Config System

**Branch**: `001-game-config-system` | **Date**: 2026-02-26 | **Spec**: `/Users/emre/Desktop/MM-Projects/Pinvestor/specs/001-game-config-system/spec.md`
**Input**: Feature specification from `/specs/001-game-config-system/spec.md`

## Summary

Build a global Game Config system with:
- A Unity Editor tool for editing multiple config domains (company, balance, round criteria, ball, shop)
- JSON export to a `Resources`-compatible path
- Runtime startup-only JSON loading/parsing via a global Singleton-based provider
- JSON DTO -> in-game config model mapping so runtime systems never work directly with JSON models
- Immediate cutover of company attribute runtime source-of-truth from embedded
  `CompanyCardDataScriptableObject.AttributeSet` data to Game Config data

The implementation should be modular so new config domains can be added without redesigning the
core editor/export/runtime pipeline. Runtime integrations should only be built for systems that
already exist in the current game codebase.

## Technical Context

**Language/Version**: C# (Unity project)  
**Primary Dependencies**: Unity, UniTask (runtime async init), MEC (gameplay sequences), existing EventBus, existing Singleton base  
**Storage**: JSON file exported to `Resources` (text asset loaded at runtime)  
**Testing**: Unity EditMode tests for serializer/parser/validation; targeted manual PlayMode verification for runtime wiring  
**Target Platform**: Unity runtime (desktop/mobile builds as supported by project) + Unity Editor tooling  
**Project Type**: Unity game project  
**Performance Goals**: Startup config load once; O(1) or efficient lookup by company ID/domain keys during gameplay; no per-frame polling  
**Constraints**: No `FindObjectOfType`, no static mutable state, no runtime `AddComponent`, no unnecessary `Update()`, Singleton-based global access, EventBus integration, prefab-first architecture  
**Scale/Scope**: Global config pipeline + schema/editor support for 5 initial domains (company, balance, round criteria, ball, shop), with runtime integrations only for existing systems and company attributes as first runtime migration target

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- ✅ Spec-first workflow: `spec.md` exists and captures migration + runtime constraints.
- ✅ Unity asset/editor integrity: editor tool and config assets/json generation are scoped; no `Library/`/generated files required.
- ✅ Runtime/editor separation: editor tool will live under an editor-only folder/assembly; runtime loader/provider remains runtime-safe.
- ✅ Validation before merge: plan includes EditMode parser/export tests and manual runtime validation.
- ✅ Observable failures: JSON parse/validation/runtime load failures will log explicit errors (no silent fallback).
- ✅ Project engineering rules: design uses Singleton provider, serialized refs, EventBus, no `FindObjectOfType`, no static mutable state, no `Update()`, no runtime `AddComponent`.
- ✅ Runtime model boundary: plan includes DTO-to-domain-model mapping before gameplay/UI consumption.
- ✅ Scope discipline: no runtime integration planned for domains without existing in-game systems.

## Project Structure

### Documentation (this feature)

```text
specs/001-game-config-system/
├── plan.md
├── spec.md
├── data-model.md          # To be added in design phase
├── quickstart.md          # To be added in design phase
└── tasks.md               # To be generated in task phase
```

### Source Code (repository root)

```text
Assets/
├── Scripts/
│   ├── GameConfig/
│   │   ├── Runtime/
│   │   │   ├── GameConfigManager.cs              # Singleton runtime provider/entry point
│   │   │   ├── GameConfigLoader.cs               # Resources JSON load + parse
│   │   │   ├── Json/GameConfigJsonDtos.cs        # JSON DTOs (serialization layer only)
│   │   │   ├── Models/GameConfigDomainModels.cs  # In-game config model classes
│   │   │   ├── Mapping/GameConfigMapper.cs       # DTO -> domain model mapping
│   │   │   ├── GameConfigLookup.cs               # Cached lookups (company IDs, keys)
│   │   │   ├── Domains/
│   │   │   │   ├── CompanyConfigRuntimeAdapter.cs
│   │   │   │   ├── BalanceConfigProvider.cs      # Only if existing consumer exists
│   │   │   │   ├── RoundCriteriaConfigProvider.cs# Defer runtime integration if no consumer exists
│   │   │   │   ├── BallConfigProvider.cs         # Candidate first non-company runtime integration
│   │   │   │   └── ShopConfigProvider.cs         # Defer runtime integration if no consumer exists
│   │   │   └── Events/                           # Optional config-loaded/failure events via EventBus
│   │   ├── Authoring/
│   │   │   ├── GameConfigAuthoringAsset.cs       # Canonical editable source data (SO or nested structs)
│   │   │   └── Domains/                          # Domain authoring models
│   │   └── Integration/
│   │       ├── CompanyAttributeConfigResolver.cs # Company attribute source-of-truth bridge
│   │       └── [consumer integrations]
│   └── [existing systems...]
├── Resources/
│   └── GameConfig/
│       └── game-config.json                      # Export target (generated/tracked policy to decide in tasks)
└── [Editor scripts location]
    └── Scripts/GameConfig/Editor/
        ├── GameConfigEditorWindow.cs             # Global editor tool UI
        ├── GameConfigExportService.cs            # JSON export + validation
        ├── GameConfigValidationService.cs
        └── DomainEditors/
            ├── CompanyConfigDomainEditor.cs
            ├── BalanceConfigDomainEditor.cs
            ├── RoundCriteriaConfigDomainEditor.cs
            ├── BallConfigDomainEditor.cs
            └── ShopConfigDomainEditor.cs
```

**Structure Decision**: Use a Unity-specific feature folder under `Assets/Scripts/GameConfig/`
split into `Runtime`, `Authoring`, `Integration`, and `Editor`. Editor code remains isolated from
runtime code. Runtime consumers read through a Singleton manager and domain-specific accessors.

## Phase Plan

### Phase 0 - Discovery and Data Mapping

Goals:
- Identify current data owners for company attributes and first consumers that must be migrated.
- Inventory existing balance/round/ball/shop values currently hardcoded or scattered.
- Define stable ID strategy for company records (compatible with `CompanyIdScriptableObject`).

Outputs:
- Mapping doc (can be `data-model.md` seed notes) of current source fields -> target config schema
- Initial list of consumers to rewire (company gameplay flow + one extra domain consumer)

Notes:
- `CompanyCardDataScriptableObject.AttributeSet` is confirmed current embedded source and must be cut over.

### Phase 1 - Core Data Model and Runtime Pipeline

Goals:
- Implement `GameConfigRoot` and domain model DTOs (company/balance/round/ball/shop).
- Implement JSON DTOs and separate in-game config model classes.
- Implement runtime `GameConfigManager` (Singleton) and `GameConfigLoader`.
- Load JSON from `Resources` at startup only.
- Parse DTOs, validate, map into in-game config models, build lookup caches, and expose read APIs.
- Emit explicit logs/errors and optional EventBus signals for load success/failure.

Design Constraints:
- No hot reload.
- No static mutable state. Singleton instance stores mutable runtime state.
- No `Update()`: runtime load triggered from initialization flow.
- Use `UniTask` for initialization/load flow.

### Phase 2 - Editor Authoring Tool and Export Pipeline

Goals:
- Implement a global Game Config editor window in Unity Editor.
- Support domain editing for:
  - Companies (attributes + company-specific values)
  - Balance
  - Round criteria
  - Ball data
  - Shop data
- Add validation (required fields, duplicate company IDs, schema consistency).
- Export JSON to `Assets/Resources/GameConfig/game-config.json` (or final agreed path under Resources).

Design Constraints:
- Editor-only code and APIs must stay in editor assembly/folder.
- Tool should be domain-modular so adding a domain does not require core tool redesign.

### Phase 3 - Company Attribute Immediate Cutover (First Runtime Integration)

Goals:
- Introduce a company attribute config resolver/adapter that reads config data from `GameConfigManager`.
- Rewire the current company attribute runtime flow so Game Config is source-of-truth.
- Remove runtime dependence on embedded `CompanyCardDataScriptableObject.AttributeSet` values for
  migrated behavior (embedded data may remain on assets temporarily but is not authoritative).

Implementation Focus:
- Resolve company config by stable company ID
- Bridge config values into the attribute runtime path expected by gameplay/UI consumers
- Add explicit failure logging when company ID/config entry is missing

Migration Policy:
- Immediate cutover, no runtime fallback to embedded `AttributeSet` data.

### Phase 4 - Additional Domain Consumer Integrations

Goals:
- Wire at least one non-company domain consumer (balance/round/ball/shop) to prove the global model.
- Prefer an existing concrete system (ball if easiest, due to visible `BallShooter` code presence).
- Ensure consumers read via `GameConfigManager` or domain providers, not hardcoded values.
- Do not create runtime consumers/providers for not-yet-existing systems; keep unsupported domains as
  schema/editor/export-ready only.

### Phase 5 - Validation, Tooling, and Documentation

Goals:
- Add EditMode tests for JSON serialization/parsing/validation and duplicate detection.
- Add targeted runtime verification steps (manual if no PlayMode test harness exists).
- Document editor usage, export steps, runtime initialization expectations, and migration guidance.

## Design Decisions (Planned)

### Editable Source of Truth (Authoring)

Planned approach:
- Use a dedicated Game Config authoring asset (ScriptableObject) as the editable source in the editor tool.
- Export that authoring data to JSON for runtime consumption.

Reasoning:
- Fits Unity editor workflows and prefab/asset-first practices.
- Enables structured editing and validation before export.
- Keeps runtime data format decoupled from editor authoring UI.

### Runtime Source of Truth

Planned approach:
- Exported JSON loaded from `Resources` at startup.
- `GameConfigManager` parses JSON DTOs, maps them into in-game config model classes, then caches
  model lookups for runtime access.

Reasoning:
- Matches your decision (`Resources`, no hot reload).
- Centralizes access while preserving the no-DI / Singleton rule.
- Prevents JSON schema types from leaking into gameplay/UI runtime code.

### Domain Extensibility

Planned approach:
- Keep one global root JSON schema with domain sections.
- Each domain has:
  - authoring model
  - editor UI drawer/module
  - runtime provider/accessor
  - validation hook

Reasoning:
- Avoids redesign when adding future config domains.
- Keeps global tool unified while containing domain complexity.

## Risks and Mitigations

- **Risk**: Immediate cutover breaks companies with missing config entries.
  - **Mitigation**: Pre-export validation + startup validation + explicit logs with company IDs.
- **Risk**: Mapping `AttributeSetScriptableObject` data into JSON schema is ambiguous.
  - **Mitigation**: Phase 0 mapping inventory and a clearly documented schema for company attributes.
- **Risk**: Editor tool scope expands too quickly across all domains.
  - **Mitigation**: Build modular shell first, implement companies + one additional domain end-to-end, then add remaining domain editors with shared patterns.
- **Risk**: Runtime init ordering issues if consumers read config before load completion.
  - **Mitigation**: Explicit startup load contract in `GameManager`/initialization flow and optional EventBus loaded event.

## Validation Strategy

- **EditMode Tests**
  - JSON export format generation for root + domain sections
  - JSON parse/deserialize validation
  - Duplicate company ID detection
  - Missing required field validation
  - Schema version validation behavior

- **Manual Runtime Validation**
  - Export config JSON from editor
  - Start game and verify config load success log
  - Verify company attribute behavior uses config values (change config to distinct values)
  - Verify one extra domain consumer (balance/round/ball/shop) uses config values
  - Verify invalid JSON shows actionable failure logs (no silent fallback)

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No planned constitution violations.
