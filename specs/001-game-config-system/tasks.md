# Tasks: Global Game Config System

**Input**: Design documents from `/specs/001-game-config-system/`
**Prerequisites**: `plan.md` (required), `spec.md` (required)

**Tests**: Include EditMode tests for export/parse/validation. Runtime validation can be manual if no
existing PlayMode harness is available for the affected flows.

**Organization**: Tasks are grouped by user story/domain outcomes so each slice can be validated
independently where possible. Foundational runtime/editor infrastructure is completed first.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., `US1`, `US2`)
- Include exact file paths in descriptions

## Path Conventions (Unity)

- Runtime feature code: `Assets/Scripts/GameConfig/...`
- Editor tooling: `Assets/Scripts/GameConfig/Editor/...`
- Runtime JSON export target: `Assets/Resources/GameConfig/game-config.json`
- Feature docs/specs: `specs/001-game-config-system/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the feature folders, baseline models, and integration points for a global config system

- [ ] T001 Create feature folders under `Assets/Scripts/GameConfig/Runtime`, `Assets/Scripts/GameConfig/Authoring`, `Assets/Scripts/GameConfig/Integration`, and `Assets/Scripts/GameConfig/Editor`
- [ ] T002 [P] Create `Assets/Scripts/GameConfig/Runtime/GameConfigManager.cs` as Singleton runtime entry point (no static mutable state beyond existing Singleton pattern)
- [ ] T003 [P] Create `Assets/Scripts/GameConfig/Runtime/GameConfigLoader.cs` for `Resources` JSON load + parse (startup-only, `UniTask`)
- [ ] T004 [P] Create `Assets/Scripts/GameConfig/Runtime/Json/GameConfigJsonDtos.cs` for JSON DTOs (`company`, `balance`, `roundCriteria`, `ball`, `shop`)
- [ ] T004a [P] Create `Assets/Scripts/GameConfig/Runtime/Models/GameConfigDomainModels.cs` for in-game config model classes used by runtime systems
- [ ] T004b [P] Create `Assets/Scripts/GameConfig/Runtime/Mapping/GameConfigMapper.cs` for DTO -> in-game model mapping
- [ ] T005 [P] Create `Assets/Scripts/GameConfig/Runtime/GameConfigLookup.cs` for cached dictionary lookups (e.g., company ID -> config entry)
- [ ] T006 [P] Create `Assets/Scripts/GameConfig/Authoring/GameConfigAuthoringAsset.cs` as editor authoring source-of-truth asset
- [ ] T007 [P] Create `Assets/Scripts/GameConfig/Editor/GameConfigEditorWindow.cs` (editor-only shell window)
- [ ] T008 [P] Create `Assets/Scripts/GameConfig/Editor/GameConfigExportService.cs` and `Assets/Scripts/GameConfig/Editor/GameConfigValidationService.cs`
- [ ] T009 [P] Create `Assets/Resources/GameConfig/` folder and initial `Assets/Resources/GameConfig/game-config.json` placeholder/export target

**Checkpoint**: Core folders and baseline classes exist; no runtime wiring yet

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement the shared global config pipeline used by all domains

**⚠️ CRITICAL**: No domain migration/integration work should start before this phase is complete

- [ ] T010 [US1] Implement JSON root DTO and shared metadata/schema version models in `Assets/Scripts/GameConfig/Runtime/Json/GameConfigJsonDtos.cs`
- [ ] T011 [P] [US1] Implement domain JSON DTOs in `Assets/Scripts/GameConfig/Runtime/Json/GameConfigJsonDtos.cs` for company/balance/round criteria/ball/shop sections
- [ ] T011a [P] [US3] Implement in-game domain model classes in `Assets/Scripts/GameConfig/Runtime/Models/GameConfigDomainModels.cs` (separate from DTOs)
- [ ] T012 [P] [US3] Implement JSON deserialization + validation entrypoint in `Assets/Scripts/GameConfig/Runtime/GameConfigLoader.cs` with explicit error logs
- [ ] T012a [US3] Implement DTO -> in-game model mapping in `Assets/Scripts/GameConfig/Runtime/Mapping/GameConfigMapper.cs`
- [ ] T013 [US3] Implement lookup cache build logic in `Assets/Scripts/GameConfig/Runtime/GameConfigLookup.cs` from in-game model classes (company ID duplicate detection, keyed access)
- [ ] T014 [US3] Implement `GameConfigManager` initialization flow in `Assets/Scripts/GameConfig/Runtime/GameConfigManager.cs` using `UniTask` (startup-only load, no `Update`)
- [ ] T015 [US3] Add read APIs on `Assets/Scripts/GameConfig/Runtime/GameConfigManager.cs` (company config + domain accessors)
- [ ] T016 [US3] Add optional config load success/failure events under `Assets/Scripts/GameConfig/Runtime/Events/` using existing EventBus system (if runtime ordering requires signaling)
- [ ] T017 [US1] Implement authoring asset model structure in `Assets/Scripts/GameConfig/Authoring/GameConfigAuthoringAsset.cs` matching runtime schema sections
- [ ] T018 [US1] Implement shared validation rules in `Assets/Scripts/GameConfig/Editor/GameConfigValidationService.cs` (required fields, duplicate IDs, domain validation aggregation)
- [ ] T019 [US1] Implement JSON export pipeline in `Assets/Scripts/GameConfig/Editor/GameConfigExportService.cs` from authoring asset -> `Assets/Resources/GameConfig/game-config.json`

**Checkpoint**: Editor authoring/export + runtime load/parse/map pipeline works end-to-end for the root schema

---

## Phase 3: User Story 1 - Edit Global Game Config Data in One Tool (Priority: P1) 🎯 MVP Shell

**Goal**: Provide a global editor tool shell with domain navigation and editable data for initial domains

**Independent Test**: Open editor window, load/create authoring asset, edit company and one non-company domain value, validate and export JSON

### Tests for User Story 1 ⚠️

- [ ] T020 [P] [US1] Add EditMode test for authoring validation + duplicate company ID detection in `Assets/Scripts/GameConfig/Editor/Tests/EditMode/GameConfigValidationServiceTests.cs`
- [ ] T021 [P] [US1] Add EditMode test for JSON export schema output in `Assets/Scripts/GameConfig/Editor/Tests/EditMode/GameConfigExportServiceTests.cs`

### Implementation for User Story 1

- [ ] T022 [US1] Implement editor window layout and domain navigation in `Assets/Scripts/GameConfig/Editor/GameConfigEditorWindow.cs`
- [ ] T023 [P] [US1] Create `Assets/Scripts/GameConfig/Editor/DomainEditors/CompanyConfigDomainEditor.cs`
- [ ] T024 [P] [US1] Create `Assets/Scripts/GameConfig/Editor/DomainEditors/BalanceConfigDomainEditor.cs`
- [ ] T025 [P] [US1] Create `Assets/Scripts/GameConfig/Editor/DomainEditors/RoundCriteriaConfigDomainEditor.cs`
- [ ] T026 [P] [US1] Create `Assets/Scripts/GameConfig/Editor/DomainEditors/BallConfigDomainEditor.cs`
- [ ] T027 [P] [US1] Create `Assets/Scripts/GameConfig/Editor/DomainEditors/ShopConfigDomainEditor.cs`
- [ ] T028 [US1] Wire domain editors into `Assets/Scripts/GameConfig/Editor/GameConfigEditorWindow.cs` with shared dirty-state + validation UI
- [ ] T029 [US1] Add export action in `Assets/Scripts/GameConfig/Editor/GameConfigEditorWindow.cs` invoking `GameConfigExportService`

**Checkpoint**: Designers can edit multiple config domains from one tool and export JSON

---

## Phase 4: User Story 2 - Define and Export Balance / Round / Ball / Shop Config Domains (Priority: P1)

**Goal**: Ensure non-company domains are fully represented in authoring, validation, and export

**Independent Test**: Modify values in balance, round criteria, ball, and shop domains; export JSON; confirm sections/keys exist and values persist

### Tests for User Story 2 ⚠️

- [ ] T030 [P] [US2] Add EditMode tests for balance/round/ball/shop domain serialization in `Assets/Scripts/GameConfig/Editor/Tests/EditMode/GameConfigDomainSerializationTests.cs`
- [ ] T031 [P] [US2] Add EditMode tests for domain-specific validation rules in `Assets/Scripts/GameConfig/Editor/Tests/EditMode/GameConfigDomainValidationTests.cs`

### Implementation for User Story 2

- [ ] T032 [US2] Finalize authoring domain models in `Assets/Scripts/GameConfig/Authoring/GameConfigAuthoringAsset.cs` for balance/round criteria/ball/shop
- [ ] T033 [P] [US2] Add validation rules for round criteria domain in `Assets/Scripts/GameConfig/Editor/GameConfigValidationService.cs`
- [ ] T034 [P] [US2] Add validation rules for ball domain in `Assets/Scripts/GameConfig/Editor/GameConfigValidationService.cs`
- [ ] T035 [P] [US2] Add validation rules for shop domain in `Assets/Scripts/GameConfig/Editor/GameConfigValidationService.cs`
- [ ] T036 [US2] Ensure export mapping for all non-company domains in `Assets/Scripts/GameConfig/Editor/GameConfigExportService.cs`

**Checkpoint**: Global tool reliably exports all initial non-company domains

---

## Phase 5: User Story 3 - Runtime Loads Global Config JSON and Serves Other Systems (Priority: P1)

**Goal**: Runtime can load config at startup, map JSON DTOs into in-game config models, and provide central access APIs to consumers

**Independent Test**: On startup, `GameConfigManager` loads JSON from `Resources`; consumers read company and one additional domain data through manager APIs

### Tests for User Story 3 ⚠️

- [ ] T037 [P] [US3] Add EditMode/runtime-safe tests for JSON parse + schema validation in `Assets/Scripts/GameConfig/Runtime/Tests/EditMode/GameConfigLoaderTests.cs`
- [ ] T037a [P] [US3] Add tests for DTO -> in-game model mapping in `Assets/Scripts/GameConfig/Runtime/Tests/EditMode/GameConfigMapperTests.cs`
- [ ] T038 [P] [US3] Add tests for lookup cache behavior and duplicate-key handling in `Assets/Scripts/GameConfig/Runtime/Tests/EditMode/GameConfigLookupTests.cs`

### Implementation for User Story 3

- [ ] T039 [US3] Implement `Resources` loading path and text parsing in `Assets/Scripts/GameConfig/Runtime/GameConfigLoader.cs`
- [ ] T039a [US3] Ensure `GameConfigManager` stores/exposes only in-game config model classes (not raw JSON DTOs)
- [ ] T040 [US3] Implement startup initialization call site (existing game initialization flow, likely `Assets/Scripts/Game/GameFSM/GameManager.cs` or equivalent) to initialize `GameConfigManager` via `UniTask`
- [ ] T041 [US3] Add domain-specific provider/access methods only for domains with existing consumers (candidate: company + ball) in `Assets/Scripts/GameConfig/Runtime/Domains/`
- [ ] T041a [US3] Defer runtime provider implementations for not-yet-existing systems (round/shop/etc.) while preserving schema/editor/export support
- [ ] T042 [US3] Add explicit load failure logging + guard behavior in `Assets/Scripts/GameConfig/Runtime/GameConfigManager.cs` (no silent fallback)

**Checkpoint**: Runtime global config system is loaded once and available to consumers

---

## Phase 6: User Story 4 - Migrate Existing Company Attribute Flow into the Global Config System (Priority: P1)

**Goal**: Company attribute runtime behavior uses Game Config as source-of-truth (immediate cutover)

**Independent Test**: Change a company attribute in exported config; start game; verify gameplay/UI uses config-backed value despite differing embedded `AttributeSet` data

### Tests for User Story 4 ⚠️

- [ ] T043 [P] [US4] Add integration-style test (EditMode or PlayMode, depending feasibility) for company ID -> config lookup and missing company failure behavior in `Assets/Scripts/GameConfig/Integration/Tests/CompanyAttributeConfigResolverTests.cs`

### Implementation for User Story 4

- [ ] T044 [US4] Create `Assets/Scripts/GameConfig/Integration/CompanyAttributeConfigResolver.cs` to resolve company attributes from `GameConfigManager` by `CompanyIdScriptableObject`
- [ ] T045 [US4] Identify and update the first runtime company attribute consumption path (starting from `Assets/Scripts/CardSystem/Cards/Company/CompanyCardDataScriptableObject.cs` usage chain) to read through resolver/manager instead of embedded `AttributeSet` source-of-truth
- [ ] T046 [US4] Update affected company gameplay/UI consumer(s) (e.g., company card/board item wrappers) to use config-backed values while preserving existing UI architecture (`VMBase`, `WidgetBase`, etc.)
- [ ] T047 [US4] Add explicit missing-company-config error logs with stable company identifier context in `Assets/Scripts/GameConfig/Integration/CompanyAttributeConfigResolver.cs`
- [ ] T048 [US4] Document immediate cutover behavior in code comments/docs where legacy embedded `AttributeSet` remains present but non-authoritative

**Checkpoint**: Company attributes are runtime-config-driven with no embedded fallback

---

## Phase 7: Additional Domain Consumer Integration (Priority: P1)

**Goal**: Prove the global system serves at least one non-company domain consumer that already exists in the game codebase (candidate: ball)

**Independent Test**: Change a non-company domain value in JSON; verify an existing system reads and applies it at runtime

### Implementation

- [ ] T049 [US3] Select the first concrete non-company consumer that exists in current code and document mapping in `specs/001-game-config-system/quickstart.md` (candidate: ball domain via `Assets/Scripts/Game/BallShooter/`)
- [ ] T050 [US3] Integrate chosen existing consumer (likely ball domain) with `GameConfigManager` domain accessor in the relevant runtime file(s)
- [ ] T051 [US3] Add validation/manual verification notes for the chosen consumer in `specs/001-game-config-system/quickstart.md`

**Checkpoint**: Global config proves multi-domain runtime consumption beyond companies

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Harden quality, docs, and workflow usability

- [ ] T052 [P] Update engineering docs if needed (`/Users/emre/Desktop/MM-Projects/Pinvestor/docs/engineering/SPEC_KIT_WORKFLOW.md` or config usage docs) with Game Config workflow references
- [ ] T053 [P] Add `specs/001-game-config-system/data-model.md` documenting JSON schema, IDs, and domain sections
- [ ] T054 [P] Add `specs/001-game-config-system/quickstart.md` with editor export steps and runtime verification procedure
- [ ] T055 Run end-to-end manual validation: export -> startup load -> company consumer -> non-company consumer -> invalid JSON failure path
- [ ] T056 Review code for constitution compliance (no `FindObjectOfType`, no runtime `AddComponent`, no new `Update`, no static mutable state, EventBus/Singleton usage only, no JSON DTO leakage into runtime consumers)

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1; blocks all domain integrations
- **Phase 3 (Global Editor Tool Shell)**: Depends on Phase 2
- **Phase 4 (Domain Export Completeness)**: Depends on Phase 3 (can overlap partially with Phase 5 after schema stabilizes)
- **Phase 5 (Runtime Load & Access)**: Depends on Phase 2
- **Phase 6 (Company Cutover)**: Depends on Phase 5 and company domain export/runtime schema readiness
- **Phase 7 (Additional Domain Consumer)**: Depends on Phase 5 and relevant domain data availability
- **Phase 8 (Polish)**: Depends on desired implementation phases completion

### User Story Dependencies

- **US1 (Global editor tool)**: Depends on foundational models/export/validation shell
- **US2 (Non-company domains export)**: Depends on US1 editor shell and shared export pipeline
- **US3 (Runtime load and access)**: Depends on foundational runtime pipeline
- **US4 (Company cutover)**: Depends on US3 runtime access and company domain schema availability

### Within Each Story

- Tests first where practical (validation/export/parser)
- Models before export/load logic
- Export/load before integration
- Runtime provider before consumer rewiring
- Company cutover before removing/ignoring legacy assumptions in gameplay paths

### Parallel Opportunities

- T002-T008 can mostly run in parallel after folder creation
- Domain editor modules (T023-T027) can run in parallel
- Domain validation rules (T033-T035) can run in parallel
- Runtime tests (T037-T038) can run in parallel
- Documentation tasks (T052-T054) can run in parallel late in the cycle

## Parallel Example: Domain Editor Modules

```bash
Task: "Create Assets/Scripts/GameConfig/Editor/DomainEditors/CompanyConfigDomainEditor.cs"
Task: "Create Assets/Scripts/GameConfig/Editor/DomainEditors/BalanceConfigDomainEditor.cs"
Task: "Create Assets/Scripts/GameConfig/Editor/DomainEditors/RoundCriteriaConfigDomainEditor.cs"
Task: "Create Assets/Scripts/GameConfig/Editor/DomainEditors/BallConfigDomainEditor.cs"
Task: "Create Assets/Scripts/GameConfig/Editor/DomainEditors/ShopConfigDomainEditor.cs"
```

## Implementation Strategy

### MVP First (Global Pipeline + Company Cutover)

1. Complete Setup + Foundational pipeline (Phases 1-2)
2. Build global editor shell and export path (Phase 3)
3. Implement runtime load/map/access (Phase 5)
4. Complete company immediate cutover (Phase 6)
5. **STOP and VALIDATE** company config-driven gameplay behavior

### Incremental Multi-Domain Delivery

1. Add/export non-company domains (Phase 4)
2. Integrate one existing non-company consumer (Phase 7)
3. Expand consumers domain-by-domain (balance, round, ball, shop)

### Risk-Control Strategy

1. Validate schema and duplicate IDs before runtime integration
2. Add explicit load/missing-entry logs before cutover
3. Keep legacy embedded company attribute data non-authoritative but present until migration is verified
