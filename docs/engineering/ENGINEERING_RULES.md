# Engineering Rules (Strict)

These are operational rules for this repository. Follow them unless the spec or reviewer explicitly
approves an exception.

## Do

- Use `spec-kit` for any non-trivial feature, refactor, or systems change.
- Keep changes scoped to one problem.
- Put gameplay tuning values and configurable game data into the Game Config system.
- Preserve Unity `.meta` file pairing and review scene/prefab diffs for accidental overrides.
- Keep runtime and editor code separated.
- Wire component references via serialized fields/properties.
- Prefer prefab-first composition and arrange components in edit mode.
- Use the existing EventBus system for game events.
- Use MEC Coroutines for in-game sequences and gameplay orchestration.
- Use `UniTask` for async initialization/loading/service operations.
- Use the existing UI architecture (`VMBase`, `VMCreators`, `WidgetBase`) and UI prefab hierarchy.
- Use the existing Singleton system for global managers.
- Write or document validation for every change.
- Add logs/error context that makes failures reproducible.
- Use Unity MCP to inspect state, hierarchy, logs, and runtime behavior during debugging.
- Document risk when touching save data, economy, purchases, or multiplayer flows.

## Do Not

- Do not commit `Library/`, `Temp/`, `Logs/`, `obj/`, or generated `.csproj` files.
- Do not change gameplay/economy/save behavior without a spec and acceptance criteria.
- Do not hardcode game config/tuning/content values in gameplay code.
- Do not leave magic numbers in game logic (except true invariants).
- Do not mix refactors with feature behavior changes in one change set.
- Do not reference `UnityEditor` from runtime code.
- Do not use `FindObjectOfType` / `FindAnyObjectByType` in production gameplay code.
- Do not use static mutable state.
- Do not use `Update()` unless continuous per-frame behavior is required and justified.
- Do not use `AddComponent` at runtime for normal game architecture.
- Do not swallow exceptions or fake success responses.
- Do not introduce DI frameworks/patterns for global managers.
- Do not bypass the existing UI architecture/prefab hierarchy.
- Do not introduce parallel event systems when EventBus covers the use case.
- Do not rely on manual editor/MCP tweaks as undocumented fixes.
- Do not upgrade packages or import large assets without impact notes and review.

## Required Change Notes (PR / Review)

- Scope summary
- Systems/files touched
- Validation evidence (tests or manual steps)
- Risk notes (serialization/save/economy/purchases/multiplayer)
- Deferred follow-ups (if any)
