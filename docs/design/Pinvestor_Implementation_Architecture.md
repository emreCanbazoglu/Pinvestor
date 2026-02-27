# Pinvestor – Implementation Architecture Companion

This document translates the core gameplay design into implementation modules based on the current
project architecture and codebase conventions.

## Purpose

Use this as the engineering bridge between:

- `/Users/emre/Desktop/MM-Projects/Pinvestor/docs/design/Pinvestor_Core_Gameplay_Document.md`
- Runtime systems in `Assets/Scripts/...`
- Current architecture constraints (Singleton, EventBus, VM/UI stack, prefab-first, no runtime
  `AddComponent`, no `FindObjectOfType` in gameplay flow)

## Architectural Principles (Project-Specific)

- Managers are orchestrators/containers, not domain logic owners.
- Domain logic lives in domain services/resolvers/adapters.
- Runtime systems consume typed config models, not raw JSON DTO fields/keys.
- Existing subsystems are extended in place (GAS, UI VM stack, EventBus), not replaced.

## Current Runtime Entry Flow

### Startup

- `GameManager` initializes game flow:
  - `Assets/Scripts/Game/GameFSM/GameManager.cs`
- GameConfig initialization happens before table/board setup:
  - `GameConfigManager.InitializeAsync()`

### Turn Loop

- `Turn.StartAsync()`:
  - offer/select company
  - wait placement event
  - launch ball
  - `Assets/Scripts/Game/Turn.cs`

### Company Placement and Runtime Company Wrapper

- Company board wrapper setup:
  - `Assets/Scripts/Board/BoardItem/Item/CompanyItem/BoardItemWrapper_Company.cs`
- Attribute initialization currently uses existing GAS `AttributeSet`, with config override resolver:
  - `AttributeSystemComponent.Initialize(AttributeSetScriptableObject)`
  - Base value override hook in `InitializeAttributeValues()`

## Existing Core Systems and Their Roles

## Turn / Match Orchestration

- `GameManager` (`Singleton`): startup, table creation, high-level loop
- `Turn`: phase sequencing within a turn
- `EventBus<T>`: turn/selection/placement signaling

## Board / Company Runtime

- `BoardItemWrapper_Company`: company wrapper lifecycle and interactions
- `CompanyFactory`: company prefab instantiation
- `AbilitySystemCharacter` + `AttributeSystemComponent`: company stat/ability execution

## Ball System

- `BallShooter` and ball components:
  - `Assets/Scripts/Game/BallShooter/...`
- First non-company GameConfig consumer is wired here.

## UI Architecture (Must Stay)

- `VMBase`, `WidgetBase`, `VMCreator*`, `UIManager`:
  - `Assets/MMFramework_2.0/MMUI/...`
- Company selection UI already event-driven:
  - `Assets/Scripts/UI/CompanySelection/CompanySelectionUI.cs`

## GameConfig System Architecture (Current + Direction)

## Layer 1: JSON DTOs (Serialization Boundary Only)

- `GameConfigRootJsonDto`
- Module DTOs (for example `BallConfigJsonDto`)
- File:
  - `Assets/Scripts/GameConfig/Runtime/Json/GameConfigJsonDtos.cs`

## Layer 2: Module Parsers

- One parser per module:
  - `CompanyConfigModuleParser`
  - `BallConfigModuleParser`
  - etc.
- File:
  - `Assets/Scripts/GameConfig/Runtime/Mapping/GameConfigModuleParsers.cs`

## Layer 3: Runtime Config Models (Typed)

- `GameConfigRootModel`
- `CompanyConfigModel`
- `BallConfigModel`
- and other typed module models
- File:
  - `Assets/Scripts/GameConfig/Runtime/Models/GameConfigDomainModels.cs`

## Layer 4: Services per Module

- `CompanyConfigService`
- `BallConfigService`
- etc.
- Registered through `GameConfigServiceRegistry`
- File:
  - `Assets/Scripts/GameConfig/Runtime/GameConfigServices.cs`

## Layer 5: Runtime Adapters/Resolvers

- Company integration:
  - `CompanyAttributeConfigResolver`
  - `CompanyAttributeBaseValueOverrideResolver`
  - `CompanyConfigRuntimeAdapter`
- Ball integration:
  - `BallConfigProvider`

## Layer 6: Editor Authoring and Export

- Canonical authoring asset:
  - `Assets/ScriptableObjects/GameConfig/GameConfigAuthoring.asset`
- Editor window:
  - `Assets/Scripts/GameConfig/Editor/GameConfigEditorWindow.cs`
- Export to runtime JSON:
  - `Assets/Resources/GameConfig/game-config.json`

## Design-System Mapping

## Offer/Placement/Launch/Resolution

- Offer and placement currently map to card + board wrappers.
- Launch maps to `BallShooter`.
- Resolution effects are distributed across company wrappers, GAS effects, and turn flow.

Target evolution:
- Keep orchestration in `Turn`/`GameManager`.
- Move per-domain rules into dedicated services/systems (cost deduction, collapse checks, market news,
  boosters, run themes) without bloating manager classes.

## Economy (Revenue, Cost, Net Worth)

Current:
- Revenue generation tied to hit events and abilities.

Target:
- Typed config modules for:
  - global economy tuning
  - cost application policy
  - valuation/cashout formula knobs
- Runtime access via dedicated services (no string lookups in gameplay code).

## Company Attributes and Health

Current:
- Attribute schema from `AttributeSetScriptableObject`
- Config values can override base values via resolver at initialization
- `HP` derived/backed from authored `MaxHP` policy for config import flow

Target:
- Keep GAS attribute schema intact
- Keep config as value authority for configured fields
- Maintain fallback behavior for non-company or unmapped usages

## Run Theme / Booster / Market News Hierarchy

Implementation intent:

- Run Theme:
  - pre-run selected macro modifier service
  - deterministic, run-long
- Boosters:
  - persistent player-owned modifier services
  - slot-limited and composable
- Market News:
  - temporary volatility service/events
  - independent of booster ownership

These should be separate modules/services with explicit precedence rules and composition order.

## Event Topology (Current Pattern)

- User and system events use existing `EventBus<T>`.
- UI state changes and turn/company actions already emit/listen through bus bindings.

Rule for new systems:
- Add event types near domain boundaries (for example `RunThemeAppliedEvent`,
  `MarketNewsTriggeredEvent`) and avoid direct cross-system coupling where EventBus fits.

## Data Contracts and Config Module Contracts

## Company Config Contract (Current)

- Keyed by `CompanyId`
- Contains typed/referenced attribute values
- Resolved through company services/resolvers during attribute initialization

## Ball Config Contract (Current)

- Typed fields:
  - `ShootSpeed`
  - `PreviewLength`
- Consumed by `BallShooter` through `BallConfigProvider`

## Future Modules (When System Exists in Game)

- Round criteria config
- Shop config
- Booster config
- Run theme config
- Market news config

Constraint:
- Do not implement runtime consumers for modules without active game systems yet.
- It is acceptable to keep schema/editor/export support ahead of runtime consumption.

## Engineering Workflow for Feature Additions

For each new gameplay module:

1. Define DTO in JSON layer.
2. Add dedicated module parser.
3. Add typed runtime model.
4. Add module service and register in `GameConfigServiceRegistry`.
5. Add resolver/provider adapter for runtime consumer code.
6. Wire existing subsystem to adapter, not directly to manager internals.
7. Add editor authoring + validation + export support.
8. Validate in Unity using MCP (compile + targeted play smoke).

## Immediate Next Implementation Targets

- Company runtime pass:
  - identify all remaining company runtime paths still using embedded values without resolver bridge
  - migrate critical paths to config-backed values while preserving GAS structure
- Ball pass:
  - expand typed ball config usage to additional parameters only when needed
- Economy pass:
  - define first typed economy module and one concrete runtime consumer

## Out of Scope for This Document

- Exact content balancing numbers
- Specific booster card definitions
- Specific startup card catalog tuning

Those belong to module content docs and balancing docs, not architecture mapping.

