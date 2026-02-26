<!--
Sync Impact Report
- Version change: 1.0.0 -> 1.1.0
- Modified principles:
  - I. Spec-First for Non-Trivial Changes (clarified config/data implications)
  - III. Runtime-Editor Separation and Deterministic Gameplay (expanded architecture constraints)
  - IV. Validation Before Merge (unchanged semantics, retained)
- Added sections: Project Constraints, Project Engineering Rules, Workflow & Quality Gates
- Removed sections: None
- Templates requiring updates:
  - ✅ /Users/emre/Desktop/MM-Projects/Pinvestor/.specify/templates/plan-template.md (reviewed; generic Constitution Check remains compatible)
  - ✅ /Users/emre/Desktop/MM-Projects/Pinvestor/.specify/templates/spec-template.md (reviewed; no constitution-specific changes required)
  - ✅ /Users/emre/Desktop/MM-Projects/Pinvestor/.specify/templates/tasks-template.md (reviewed; validation/testing guidance aligns)
- Follow-up TODOs: None
-->

# Pinvestor Constitution

## Core Principles

### I. Spec-First for Non-Trivial Changes
Any feature, system refactor, economy change, save-data change, or multiplayer-affecting change
MUST start with the `spec-kit` flow (`/speckit.specify`, `/speckit.plan`, `/speckit.tasks`)
before implementation. Small fixes (typos, isolated null guards, logging improvements, localized
UI text updates) MAY skip full specs, but the scope and validation plan MUST still be documented.
Implementation MUST match the approved spec, or the spec MUST be updated first. New gameplay
values, tuning parameters, and content data MUST be planned as config/data entries, not embedded
constants.

### II. Unity Asset Safety and Editor Integrity
Engineers MUST preserve Unity asset/database integrity. Do not move/delete assets outside Unity
when `.meta` pairing matters. Do not commit `Library/`, `Temp/`, `Logs/`, `obj/`, or generated
`.csproj` files unless explicitly required for a justified tooling change. Scene/prefab edits MUST
be scoped, reviewed, and checked for unintended serialized overrides.

### III. Runtime-Editor Separation and Deterministic Gameplay
Runtime code MUST NOT depend on `UnityEditor` APIs or editor-only assemblies. Editor tooling MUST
reside in editor-only folders/assemblies. Gameplay, economy, progression, and save logic MUST use
explicit state transitions and deterministic inputs where feasible; hidden global mutations and
frame-order assumptions MUST be treated as defects unless justified and documented. Static mutable
state is prohibited. Global managers MUST use the existing project Singleton system (no DI
introduction unless separately approved by spec and architecture review).

### IV. Validation Before Merge (Non-Negotiable)
Every change MUST be validated at the cheapest reliable level first (unit/edit-mode tests, then
play-mode/manual verification). Each change requires one of:
- Automated test coverage for the affected behavior, or
- A documented manual verification checklist with exact scenes/steps and expected outcomes

Changes touching save data, economy, purchases, or multiplayer flows MUST include explicit risk
notes and validation evidence in review notes.

### V. Observable Failures, No Silent Recovery
Failures MUST be visible and diagnosable. Exceptions MUST NOT be swallowed without logging/context.
Fallbacks/retries MUST log enough state to support reproduction. Unity MCP and editor tooling MAY
assist diagnosis, but MUST NOT be used as a substitute for source changes, tests, or documented
verification.

## Project Constraints

- Primary implementation stack is Unity + C#.
- Package/config changes MUST be compatible with the project Unity version and existing packages.
- `spec-kit` artifacts are the source of truth for scope, acceptance criteria, and task breakdown
  on non-trivial work.
- Unity MCP is approved for inspection/debugging automation assistance only.
- Large third-party imports or package upgrades MUST include impact notes (runtime/editor risk,
  size, licensing) before merge.

## Project Engineering Rules

- No hardcoded numbers/config/game data in gameplay systems. Use the Game Config system for tuning,
  balance values, and configurable content parameters.
- No magic numbers in game logic. Named constants are acceptable only for true invariants; tunable
  values MUST live in config/data.
- `FindObjectOfType` / `FindAnyObjectByType` style runtime object discovery is prohibited in
  production gameplay code.
- Use serialized fields or serialized properties for component references to avoid lookup cost and
  improve prefab integrity.
- Do not use `AddComponent` at runtime for normal gameplay architecture. Components MUST be
  arranged on prefabs in edit mode (prefab-first architecture).
- Prefer existing prefab hierarchies and edit-time composition over runtime scene mutation.
- Use the existing EventBus system for game events; do not introduce parallel event systems.
- Do not use `Update()` unless there is a justified continuous-frame requirement. Prefer event-
  driven flows, timers, or coroutine-driven sequences.
- Use MEC Coroutines for in-game sequences and gameplay flow orchestration.
- Use `UniTask` (`async`/`await`) for async operations outside core gameplay loops (e.g.,
  initialization, loading, service calls).
- Use the existing UI architecture (`VMBase`, `VMCreators`, `WidgetBase`, etc.) and preserve the
  existing UI prefab hierarchy.
- Do not introduce DI frameworks/patterns for global manager access. Use the existing Singleton
  system consistently.
- System managers (global managers/runtime managers) MUST be treated as containers/orchestrators,
  not domain-logic owners. When integrating new subsystems/services, prefer extending via
  subservices/adapters/registries instead of adding system-specific behavior directly into manager
  classes.

## Workflow & Quality Gates

- Investigate affected systems first; identify serialization, save-data, and package risks early.
- Create/update spec artifacts before implementation for non-trivial changes.
- Keep implementation changes scoped; separate refactors from feature behavior changes.
- Validate with targeted tests when available, otherwise document exact manual test steps/results.
- Reviewers MUST check constitution compliance, especially asset safety and validation evidence.
- Do not merge known failing tests, undocumented serialization-impact changes, or unreviewed
  package upgrades.

## Governance

This constitution supersedes conflicting informal practices. Amendments require documented
rationale, updates to this file, and a quick alignment check of `spec-kit` templates and project
engineering guides. Versioning follows semantic versioning: MAJOR for incompatible principle
changes, MINOR for new principles/sections or materially stronger requirements, PATCH for
clarifications only. Constitution compliance is checked during planning and code review.

**Version**: 1.1.0 | **Ratified**: 2026-02-26 | **Last Amended**: 2026-02-26
