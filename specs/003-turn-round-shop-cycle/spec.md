# Feature Specification: Turn-Round-Shop Core Cycle

**Feature Branch**: `003-turn-round-shop-cycle`  
**Created**: 2026-02-27  
**Status**: Draft  
**Input**: User description: "Complete turn-round-shop core cycle first, define required worth per round and total turn counts, keep shop as placeholder, use existing systems (GameManager/Turn, MMFSM optional) and do not create a new FSM system."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Deterministic Turn Cycle with Explicit Phases (Priority: P1)

As a player/developer, each turn follows explicit phases in order:

Offer -> Placement -> Launch -> Resolution -> Shop (placeholder)

so the core loop is stable and extensible.

**Why this priority**: The game loop structure is foundational and blocks all gameplay iteration.

**Independent Test**: Start game and run one turn; verify phase order through logs/events and no phase is skipped.

**Acceptance Scenarios**:

1. **Given** a running game, **When** a turn starts, **Then** offer and placement occur before launch.
2. **Given** launch finishes, **When** resolution executes, **Then** shop placeholder phase executes after resolution.

---

### User Story 2 - Round-Based Progression Targets (Priority: P1)

As a designer, I can define round count, per-round turn counts, and required worth thresholds so run
progression has explicit goals.

**Why this priority**: Round goals are required for win/loss pacing and progression checks.

**Independent Test**: Configure at least 2 rounds with different turn counts/required worth values; verify runtime advances rounds and evaluates round target checks.

**Acceptance Scenarios**:

1. **Given** round settings with turn counts, **When** turns are consumed, **Then** runtime advances to the next round.
2. **Given** a required worth threshold, **When** round ends, **Then** threshold check runs and result is logged/exposed.

---

### User Story 3 - Shop Phase Placeholder Contract (Priority: P2)

As an engineer, I need a placeholder shop phase contract in the cycle so later shop implementation can
plug in without changing the loop structure.

**Why this priority**: Shop should be structurally present now, implementation can arrive later.

**Independent Test**: Run multiple turns and confirm shop placeholder executes every turn after resolution.

**Acceptance Scenarios**:

1. **Given** a completed resolution phase, **When** turn flow continues, **Then** shop placeholder callback/log runs.
2. **Given** shop is placeholder only, **When** cycle runs, **Then** gameplay does not block or throw due to missing shop logic.

---

### Edge Cases

- Missing cycle/round config asset
- Round with zero/negative turn count
- Missing net worth source for required worth checks
- Empty round list
- Round threshold check performed but no provider exists (must degrade safely with diagnostics)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Turn execution MUST be structured into explicit ordered phases: Offer, Placement, Launch, Resolution, ShopPlaceholder.
- **FR-002**: The cycle runner MUST support multiple rounds.
- **FR-003**: Each round MUST define `turnCount` and `requiredWorth`.
- **FR-004**: Runtime MUST evaluate required worth at round boundaries.
- **FR-005**: Shop phase MUST exist in cycle as placeholder and be invoked each turn.
- **FR-006**: Missing/invalid round config MUST fail safely with clear logs, not hard crashes.
- **FR-007**: Implementation MUST reuse existing architecture (GameManager/Turn/EventBus/Singleton/MMFramework systems) and MUST NOT create a custom FSM framework.
- **FR-008**: If state machine orchestration is introduced, it MUST use existing MMFSM/MMFSMController only.
- **FR-009**: Existing gameplay flow (company selection + ball launch) MUST remain functional after cycle refactor.

### Key Entities *(include if feature involves data)*

- **RunCycleSettings**: Top-level cycle settings containing ordered rounds.
- **RoundSettings**: Per-round config with `turnCount` and `requiredWorth`.
- **TurnPhase**: Explicit phase enum/contract for per-turn sequencing.
- **TurnCycleRuntimeState**: Current round index, turn index, and phase context.
- **ShopPhasePlaceholder**: Placeholder runtime hook invoked post-resolution.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A run executes turns in explicit phase order with no missing phase transitions.
- **SC-002**: Round boundaries occur exactly when configured turn counts are exhausted.
- **SC-003**: Required worth checks trigger on every round end and produce deterministic pass/fail outcomes or explicit "provider unavailable" diagnostics.
- **SC-004**: Shop placeholder is executed every turn after resolution without blocking the loop.

