# Feature Specification: Global Game Config System

**Feature Branch**: `001-game-config-system`  
**Created**: 2026-02-26  
**Status**: Draft  
**Input**: User description: "Game Config System will be the main global editor/system that manages in-game configs. It must support company attributes, game balance, round criteria, ball data, and shop data, export JSON, runtime load/parse, and migration from company ScriptableObject embedded attributes."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Edit Global Game Config Data in One Tool (Priority: P1)

As a designer/developer, I can edit global game configuration data from a single Game Config editor
tool, including company attributes, game balance, round criteria, ball data, and shop data, instead
of scattering data across ScriptableObjects and code.

**Why this priority**: This establishes the central tool and data ownership model for future config
domains, while solving the immediate company/balance migration need.

**Independent Test**: Open the Game Config editor, modify a company's attribute values and at least
one non-company domain value (e.g., balance/ball/shop), save/export, and verify the exported config
contains the updated entries.

**Acceptance Scenarios**:

1. **Given** existing game config domains (starting with companies, balance, round criteria, ball,
   and shop), **When** I open the Game Config editor, **Then** I can view and edit them from a
   central tool.
2. **Given** I update company attributes in the editor, **When** I export JSON, **Then** the JSON
   contains the updated company payload in the expected schema.

---

### User Story 2 - Define and Export Balance / Round / Ball / Shop Config Domains (Priority: P1)

As a designer/developer, I can define game balance values and other gameplay config domains
(including round criteria, ball data, and shop data) in the Game Config system and export them to
JSON for runtime consumption.

**Why this priority**: Prevents hardcoded tuning values and proves the system supports multiple
config domains beyond company attributes.

**Independent Test**: Modify one balance parameter and one value from round/ball/shop domains in the
editor, export JSON, and verify the values are present and changed in the exported file.

**Acceptance Scenarios**:

1. **Given** balance, round criteria, ball, and shop parameters are defined in the Game Config
   system, **When** I export config, **Then** all configured domain sections are serialized into
   JSON.
2. **Given** a new field is added to a supported config domain model, **When** the editor reloads,
   **Then** it appears in the domain editing flow without requiring a redesign of the core tool.

---

### User Story 3 - Runtime Loads Global Config JSON and Serves Other Systems (Priority: P1)

As the game runtime, I can load exported Game Config JSON at startup and expose parsed global config
data to other systems so gameplay systems consume a central config source.

**Why this priority**: A global config tool is incomplete unless runtime consumption is centralized.

**Independent Test**: Start the game with exported JSON present; verify runtime parses config and
multiple systems (company attributes + one balance consumer) obtain values from the Game Config
provider path.

**Acceptance Scenarios**:

1. **Given** a valid exported Game Config JSON file, **When** the game initializes config loading,
   **Then** the Game Config system parses and stores the available config domains (including company,
   balance, round criteria, ball, and shop data).
2. **Given** another system requests company data, **When** the Game Config runtime is initialized,
   **Then** it returns config-backed values through the central Game Config access path.
3. **Given** JSON is missing or invalid, **When** runtime attempts to load config, **Then** failure
   is surfaced with actionable logging (no silent fallback).

---

### User Story 4 - Migrate Existing Company Attribute Flow into the Global Config System (Priority: P1)

As an engineer, I can wire the current company attribute flow (currently embedded on
`CompanyCardDataScriptableObject.AttributeSet`) to use Game Config data so existing gameplay systems
continue working while data ownership moves to the new config system.

**Why this priority**: Company attributes are the first concrete migration target and validate the
global system against a real production flow.

**Independent Test**: With config JSON loaded, create/use a company card and confirm the effective
attributes used by company gameplay/UI reflect Game Config values instead of stale embedded values.

**Acceptance Scenarios**:

1. **Given** `CompanyCardDataScriptableObject` currently references an `AttributeSetScriptableObject`,
   **When** migration wiring is implemented, **Then** company attribute resolution uses Game Config
   as the source of truth at runtime.
2. **Given** legacy embedded attribute values differ from exported config values, **When** gameplay
   initializes, **Then** runtime behavior uses config JSON values and logs migration mismatches if
   applicable.

---

### Edge Cases

- What happens when a company exists in the project but has no matching Game Config entry?
- What happens when exported JSON contains a company ID unknown to the current build?
- How does the editor/runtime handle schema version mismatch between exported JSON and parser?
- How are invalid/missing attribute values validated before export (range/type/required fields)?
- How are duplicate company IDs prevented in the editor?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a global Game Config editor tool inside Unity Editor for editing
  game configuration data across multiple config domains.
- **FR-002**: The Game Config editor MUST allow editing company-specific attributes currently stored
  in company-related ScriptableObject authoring data.
- **FR-003**: The Game Config editor MUST allow editing game balance parameters (tunable values).
- **FR-003b**: The Game Config editor MUST support config domains for round criteria, ball data, and
  shop data (initially editable and exportable through the same global tool).
- **FR-003a**: The Game Config system architecture MUST support adding additional config domains in
  the future without redesigning the core editor/runtime pipeline.
- **FR-004**: The Game Config system MUST export the authored config into JSON.
- **FR-005**: Runtime MUST load and parse the exported Game Config JSON during initialization.
- **FR-006**: Runtime MUST expose parsed config data through a central Game Config access API/service
  so other systems can read domain-specific data from one global config system.
- **FR-006a**: Runtime systems MUST NOT consume raw JSON/DTO parse models directly after load.
  Parsed data MUST be mapped into in-game config model classes (domain model representations) and
  runtime systems MUST consume those model classes.
- **FR-007**: Company attribute resolution in gameplay/runtime MUST be wired to Game Config data
  instead of relying on embedded company ScriptableObject attribute values as the source of truth.
- **FR-008**: System MUST identify company records by a stable identifier compatible with existing
  company IDs (`CompanyIdScriptableObject` or a derived stable ID).
- **FR-009**: Export and runtime load MUST validate required fields and surface descriptive errors.
- **FR-010**: The editor tool MUST prevent or clearly report duplicate company identifiers.
- **FR-011**: The implementation MUST follow project architecture rules: no runtime `AddComponent`,
  no `FindObjectOfType`, no static mutable state, no unnecessary `Update()`.
- **FR-012**: In-game systems consuming config MUST access it through the existing Singleton/global
  manager conventions and existing EventBus integration patterns where events are needed.
- **FR-012a**: The implementation MUST NOT build runtime integrations for domains/systems that do
  not yet exist in the game codebase. For not-yet-implemented domains, only schema/editor/export
  support is allowed (or they may remain deferred placeholders in the authoring model).

- **FR-013**: System MUST export the runtime JSON to a `Resources`-compatible location and runtime
  loading MUST read from `Resources`.
- **FR-014**: Runtime config loading MUST be startup-only; hot reload is not required.
- **FR-015**: Migration from legacy embedded `AttributeSetScriptableObject` company attribute data
  MUST use immediate cutover (Game Config JSON is the runtime source of truth with no fallback).

### Key Entities *(include if feature involves data)*

- **GameConfigRoot**: Top-level config model containing multiple config domains (starting with
  company, balance, round criteria, ball, and shop configs), schema version, and metadata.
- **CompanyConfigEntry**: Config record keyed by company identifier; contains company attributes and
  any company-specific tunable values used by gameplay/UI.
- **BalanceConfigSection**: Group of global tunable values for gameplay balance systems.
- **RoundCriteriaConfigSection**: Config data defining round rules/criteria/thresholds used by round
  progression/scoring logic.
- **BallConfigSection**: Config data defining ball-related gameplay parameters used by ball systems.
- **ShopConfigSection**: Config data defining shop-related values, entries, or tuning parameters.
- **ConfigDomainModule**: A domain-specific config module/section contract (e.g., companies,
  balance, future systems) that plugs into the shared editor/export/runtime pipeline.
- **GameConfigEditorState**: Editor-side representation and validation state used by the Unity editor
  tool (selection, dirty state, validation errors).
- **GameConfigRuntimeProvider**: Runtime access point/service that loads parsed JSON and serves data
  to other systems.
- **GameConfigJsonDto**: JSON serialization/deserialization DTO layer used only during load/export.
- **GameConfigDomainModel**: Runtime-safe in-game config model layer used by gameplay/UI systems
  after JSON parsing/mapping.
- **CompanyAttributeMapping**: Wiring/mapping layer connecting existing company runtime flows to
  config-backed values (especially replacing embedded `AttributeSet` source ownership).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A designer/developer can change a company attribute in the Game Config editor and
  export JSON in under 2 minutes without editing code or ScriptableObject fields directly.
- **SC-002**: Runtime initializes successfully from exported JSON and serves config data to at least
  one existing company gameplay flow and one additional domain consumer (balance/round/ball/shop).
- **SC-003**: Company attribute behavior for migrated companies matches exported config values in
  validation tests/manual checks, even when embedded legacy values differ.
- **SC-004**: Invalid JSON or invalid config schema produces explicit logs/errors with enough detail
  to identify the failing section/key (no silent fallback to embedded company attribute data).
