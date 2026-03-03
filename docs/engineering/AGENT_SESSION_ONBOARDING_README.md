# Agent Session Onboarding (Pinvestor)

Use this guide at the start of a new coding session to understand the project quickly and begin implementation safely.

## 1) Read First (Order Matters)

1. Constitution (source of truth):  
`/Users/emre/Desktop/MM-Projects/Pinvestor/.specify/memory/constitution.md`
2. Engineering rules (strict do/don't):  
`/Users/emre/Desktop/MM-Projects/Pinvestor/docs/engineering/ENGINEERING_RULES.md`
3. Game design context:  
`/Users/emre/Desktop/MM-Projects/Pinvestor/docs/design/Pinvestor_Core_Gameplay_Document.md`  
`/Users/emre/Desktop/MM-Projects/Pinvestor/docs/design/Pinvestor_Implementation_Architecture.md`
4. Turn-round-shop cycle spec docs:  
`/Users/emre/Desktop/MM-Projects/Pinvestor/specs/003-turn-round-shop-cycle/spec.md`  
`/Users/emre/Desktop/MM-Projects/Pinvestor/specs/003-turn-round-shop-cycle/plan.md`  
`/Users/emre/Desktop/MM-Projects/Pinvestor/specs/003-turn-round-shop-cycle/tasks.md`
5. GameConfig system spec docs:  
`/Users/emre/Desktop/MM-Projects/Pinvestor/specs/001-game-config-system/spec.md`  
`/Users/emre/Desktop/MM-Projects/Pinvestor/specs/001-game-config-system/plan.md`  
`/Users/emre/Desktop/MM-Projects/Pinvestor/specs/001-game-config-system/tasks.md`

## 2) Project Rules You Must Follow

- No hardcoded gameplay numbers/config/content values.
- No magic numbers in gameplay logic.
- No `FindObjectOfType` / `FindAnyObjectByType` for production gameplay code.
- No static mutable state.
- No runtime `AddComponent` in normal architecture.
- Avoid `Update()` unless there is a clear continuous-frame requirement.
- Use MEC coroutines for gameplay sequencing.
- Use UniTask for async initialization/loading/service operations.
- Use existing EventBus system for events.
- Preserve prefab-first architecture and existing prefab hierarchy.
- Use existing UI architecture (`VMBase`, `VMCreators`, `WidgetBase`, etc.).
- Use existing Singleton system for global managers (no DI introduction).
- Managers are orchestrators/containers, not domain-logic owners.
- Coding style preference:
  - keep lines short and meaningful
  - prefer guard clauses
  - omit braces for one-line conditionals

## 3) Current Key System Locations

- GameConfig runtime/editor:  
`/Users/emre/Desktop/MM-Projects/Pinvestor/Assets/Scripts/GameConfig/`
- Run cycle runtime:  
`/Users/emre/Desktop/MM-Projects/Pinvestor/Assets/Scripts/Game/RunCycle/`
- Main orchestrator:  
`/Users/emre/Desktop/MM-Projects/Pinvestor/Assets/Scripts/Game/GameFSM/GameManager.cs`
- Exported runtime config JSON:  
`/Users/emre/Desktop/MM-Projects/Pinvestor/Assets/Resources/GameConfig/game-config.json`
- Authoring asset:  
`/Users/emre/Desktop/MM-Projects/Pinvestor/Assets/ScriptableObjects/GameConfig/GameConfigAuthoring.asset`

## 4) Session Startup Checklist

1. Check working tree:
`git status --short`
2. Inspect relevant code with `rg` and focused file reads.
3. Validate touched scripts via Unity MCP.
4. Compile + console check via Unity MCP (`refresh_unity`, `read_console`).

## 5) Change Workflow

1. For non-trivial changes, align with spec-kit artifacts first.
2. Keep changes scoped by concern.
3. Implement using existing architecture patterns.
4. Validate (script validation, compile, console, smoke flow).
5. Report:
   - files changed
   - behavior impact
   - validation evidence
   - risks/follow-ups

## 6) Notes for New Agents

- Prefer extending existing services/modules over editing central managers for each new subsystem.
- Keep runtime consumers on typed config models, not raw JSON models.
- When uncertain, follow constitution + engineering rules over assumptions.
