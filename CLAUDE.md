# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Game Element Designer Skill

The `skills/game-element-designer/` directory is a design workflow for creating new Pinvestor gameplay elements (companies, boosters, run themes, market news). This is **not implementation code** — it is a specification and reasoning system for content authoring.

Skill file registered at `~/.claude/skills/game-element-designer.md`.

## Context Load Order (Before Designing)

Always load in this order:

1. `docs/design/Pinvestor_Core_Gameplay_Document.md` — game rules, turn structure, economy
2. `docs/design/Pinvestor_Implementation_Architecture.md` — system boundaries and entry flows
3. `Assets/Resources/GameConfig/game-config.json` — runtime config snapshot (authoritative)
4. `docs/design/Pinvestor_Game_Elements_Design.md` — all designed companies, boosters, synergy maps, and implementation tiers
5. `skills/game-element-designer/references/content-catalog.md` — current authored elements (runtime)
6. `skills/game-element-designer/references/pinvestor-ecosystem-audit.md` — combo anchors and dead design space
7. `skills/game-element-designer/references/humor-style-guide.md` — tone and naming conventions

**Refresh the content catalog before every design session** (it is auto-generated and goes stale):

```sh
./skills/game-element-designer/scripts/refresh-content-catalog.sh
```

## Design Workflow

1. **Ecosystem audit** — inventory companies/boosters, map which rules are touched, identify open combo space and dead zones
2. **Behavior fantasy** — one-line player power fantasy, the exact rule that bends, the comedic hook
3. **Mechanic spec** — trigger / condition / effect / duration / scope / limits; interactions across turn phases
4. **Synergy + combo map** — 3 combo lines (early stabilizer, mid scaler, late payoff) + 2 anti-synergies + 1 degenerate-loop risk with mitigation
5. **Balance constraints** — costs, timing gates, caps, decay, or slot pressure; preserve fantasy without dominant always-pick patterns
6. **Fun validation** — score against `references/fun-balance-rubric.md`; reject pure stat inflation

## Required Output (in order)

1. Element Concept
2. Behavior Spec (Rule Bent)
3. Synergy + Combo Matrix
4. Balance Guardrails
5. Comedy Hook + Name Ideas
6. Fun/Replayability Justification
7. Implementation Notes (Pinvestor Architecture)
8. Open Risks and Playtest Focus

## Design Pillars

- Behavior over stats; rule-bending over flat bonuses
- Engine-building and synergy webs over standalone one-off value
- Absurd dark comedy — humor must support gameplay clarity, not bury it
- "Wild but bounded" over "safe but forgettable"
- Never ship a purely numerical design without behavior identity
- Always reason across the existing ecosystem; never evaluate in isolation

## Reference Files

| File | Purpose |
|------|---------|
| `docs/design/Pinvestor_Game_Elements_Design.md` | Authoritative design doc — all companies, boosters, synergy maps, implementation tiers |
| `references/content-catalog.md` | Auto-generated snapshot of authored elements in the runtime (refresh before each session) |
| `references/fun-balance-rubric.md` | 6-axis scoring rubric (1–5 each); red flags that trigger reject/redesign |
| `references/humor-style-guide.md` | Tone, naming patterns, flavor text constraints |
| `references/pinvestor-ecosystem-audit.md` | Checklist for combo/synergy mapping and system interaction surfaces |
| `SKILL.md` | Full skill definition and quality bar |

## Known Inconsistencies to Watch

- Content catalog has a generation timestamp — if stale, re-run the refresh script before designing
- Legacy companies (`Cashnado`, `StackRabbit`, `VirtosoXR`) are retired — ignore any catalog references to them

---

## UI Architecture & Conventions

### Framework: MMFramework MMUI (`Assets/MMFramework_2.0/MMUI/`)

All UI is built on a custom MVVM stack with UnityWeld data binding. **Never replace or bypass this stack.**

### Base Classes

| Base Class | Use For | Location |
|------------|---------|----------|
| `VMBase` | Root-level UI panels, screens, windows | `MMUI/ViewModels/VMBase.cs` |
| `WidgetBase` (extends `VMBase`) | Child UI elements within a panel (cards, bars, buttons) | `MMUI/ViewModels/Widgets/WidgetBase.cs` |
| `ButtonWidget` (extends `WidgetBase`) | Clickable buttons — auto-manages `onClick` listeners on activate/deactivate | `MMUI/ViewModels/Widgets/ButtonWidget.cs` |

### Naming Conventions

| Type | Pattern | Examples |
|------|---------|----------|
| Panel / Screen VM | `{Feature}UI` or `VM_{Feature}` | `CompanySelectionUI`, `VM_Game` |
| Widget | `Widget_{Feature}` or `{Feature}Widget` | `Widget_CompanyCard`, `Widget_Balance`, `Widget_HPBar`, `Widget_FloatingText`, `ButtonWidget` |
| Prefab | `P_UI_{Feature}` or `P_{WidgetName}` | `P_UI_CompanySelection`, `P_Button.ShowUI` |
| Events (show/hide) | `Show{Panel}Event` / `Hide{Panel}Event` | `ShowCompanyOfferPanelEvent`, `HideCompanyOfferPanelEvent` |
| Events (gameplay) | `{Action}Event` | `CompanyReadyForPlacementEvent`, `CompanyPlacedEvent`, `CompanyCollapsedEvent` |

### Lifecycle Hooks (Override These, Never Call Directly)

- `AwakeCustomActions()` — register EventBus bindings, cache references
- `OnDestroyCustomActions()` — deregister EventBus bindings, cleanup
- `ActivatingCustomActions()` — populate data, set initial visual state (fires during activation transition)
- `ActivatedCustomActions()` — wire runtime listeners (e.g., `Button.onClick`)
- `DeactivatingCustomActions()` — begin teardown
- `DeactivatedCustomActions()` — clear state, kill tweens, destroy dynamic children

### Activation / Deactivation

- Call `TryActivate()` / `TryDeactivate()` — **never** call `gameObject.SetActive()` directly
- `TryActivate` routes through `UIManager` (for windows) or `SequencableVMManager` (for sequenced panels) automatically
- `TrySleep()` / `TryWake()` — hide without full deactivation (preserves event subscriptions and state)

### Subwidget Discovery

- Annotate widget properties with `[SubWidget]` attribute for auto-discovery by `VMBaseResolver`
- `ActivateTogetherWithParent` / `DeactivateTogetherWithParent` flags control child lifecycle
- Alternatively, wire subwidgets via `_subWidgetInfoColl` in the inspector

### Data Binding (UnityWeld)

- Mark VM/Widget class with `[Binding]` attribute
- Mark bindable properties and command methods with `[Binding]`
- Call `OnPropertyChanged(nameof(PropertyName))` in property setters
- Prefab wires bindings via `viewModelMethodName: Namespace.ClassName.MethodName`

### Event-Driven UI Pattern

All UI panels are shown/hidden via EventBus events, **not** by direct method calls:

```csharp
// Turn.cs raises event → UI listens and responds
EventBus<ShowCompanyOfferPanelEvent>.Raise(new ShowCompanyOfferPanelEvent(context));

// In the VM's AwakeCustomActions:
_showBinding = new EventBinding<ShowCompanyOfferPanelEvent>(OnShowEvent);
EventBus<ShowCompanyOfferPanelEvent>.Register(_showBinding);
```

### DOTween Animation Conventions

- Use `DOScale` for show/hide card animations, `DOFade` for overlays
- Store active tweens in a dictionary keyed by target object
- Always `Kill()` existing tweens before starting new ones on the same target
- Clean up all tweens in `DeactivatedCustomActions()` or `OnDestroyCustomActions()`
- Tween parameters (`duration`, `ease`) should be `[SerializeField]` for designer tuning

### Critical Rules for Agents

1. **NEVER delete an existing UI script that has a prefab referencing it** — the prefab stores the script GUID in its `.meta` file; deleting the script creates a "Missing Script" error that breaks the game. Instead, adapt the existing class in-place.
2. **NEVER delete `.meta` files** — Unity uses GUIDs from `.meta` files to maintain all references. Losing a `.meta` means every prefab, scene, and ScriptableObject reference to that asset breaks.
3. **Extend, don't replace** — if a UI class exists, refactor it to support the new behavior. Change internal logic, add new serialized fields, but preserve the class name, namespace, and file path.
4. **Prefab wiring happens in Unity Editor** — code can create the VM/Widget scripts, but the actual prefab hookup (serialized references, transitions, layout) must be done in the editor. Don't assume a new script will automatically appear on a prefab.
5. **Check prefab references before renaming or moving UI scripts** — search `*.prefab` and `*.unity` files for the script's GUID before any rename/move/delete.

### Existing UI Panels & Prefabs

| Script | Prefab | Purpose |
|--------|--------|---------|
| `CompanySelectionUI` (`Pinvestor.UI`) | `P_UI_CompanySelection` | Company offer/selection panel — shows offered companies, handles selection, show/hide toggle |
| `VM_Game` (`Pinvestor.UI`) | *(scene-embedded)* | Root game HUD — parent for in-game widgets |
| `Widget_Balance` | *(child of VM_Game)* | Displays player balance |
| `Widget_HPBar` | *(child of company board item)* | Company HP bar |
| `BoardItemInfoUI` | *(scene-embedded)* | Board item tooltip/info overlay |

### Company Placement Pipeline (Config-Driven)

The placement flow bypasses the card system entirely. The pipeline is:

1. **Offer phase**: `Turn.RunNewOfferPhase()` → `CompanyOfferDrawer` draws from `RunCompanyPool` → `CompanySelectionUI` shows 3 cards → player selects → `SelectedCompany` set on `Turn`
2. **Placement phase**: `Turn.RunPlacementPhase()` → `BoardItemData_Company(companyId)` → `BoardItemFactory.CreateBoardItem()` → `boardItem.CreateWrapper()` → raises `CompanyReadyForPlacementEvent`
3. **Input controller**: `CompanySelectionInputController` listens for `CompanyReadyForPlacementEvent` (registered in `Awake`, always active) → starts drag-to-place coroutine → on valid placement raises `CompanyPlacedEvent` → `Turn.OnCompanyPlaced()` marks pool and sets `_isCompanyPlaced = true`

Key points:
- `CompanySelectionPile` / `CardFactory` / `CardContainer` are **not** used in the placement flow
- The `P_BoardItemWrapper.Company.prefab` must have `AttributeSet.Company.asset` wired to `_attributeSet` — without it, `WrapCore()` → `Initialize()` will NullRef
- Cancel during direct placement re-raises `CompanyReadyForPlacementEvent` to restart drag (no card selection to return to)

---

## Parallel Implementation Workflow (Codex Agents + Claude Review)

Specs are implemented by spawning parallel OpenAI Codex agents via CLI, one per spec (or per phase). Claude Code reviews each PR, agents iterate on feedback, and all approved PRs are merged to `main`.

### Step 0 — Codebase Audit Before Implementation

**Every agent must run this before writing a single line of implementation code.**

Each `tasks.md` lists a "Codebase audit required" set of files. That is the minimum. Agents must also do a broader existence check:

1. **Search for the feature by concept** — grep for class names, interface names, and key terms from the spec before assuming nothing exists:
   ```sh
   # Example for spec 005 (Company Offer Selection)
   grep -r "CompanyOffer\|OfferPhase\|RunCompanyPool\|CompanyOfferDrawer" Assets/Scripts/ --include="*.cs" -l
   grep -r "offer\|selection\|company pool" Assets/Scripts/ --include="*.cs" -li
   ```

2. **Inspect the task list's declared audit files in full** — every file listed under "Codebase audit required" in `tasks.md` must be read completely, not skimmed. Look for:
   - Existing classes that overlap with what the spec wants to create
   - Existing events, interfaces, or services that the spec's new code should extend rather than duplicate
   - Existing UI panels or widgets that should be modified rather than recreated

3. **Check `CompanyContainer.asset`, `Turn.cs`, `GameManager.cs`, and `Board.cs` for every spec** — these four files are touched by nearly every feature. Read the relevant sections before any modification.

4. **Document findings before implementing** — at the top of the first commit message, note what was already present:
   ```
   T001 [spec-005] Audit: CompanySelectionPile.cs exists and covers pool draw logic —
   RunCompanyPool will wrap it. OfferPhaseContext is new. No existing offer UI found.
   ```

5. **If a feature is already substantially implemented**, the agent must:
   - Stop, comment on the PR description with findings
   - Mark affected tasks as `ALREADY IMPLEMENTED — verified` with a code reference
   - Only implement the delta (missing pieces), not re-implement what exists
   - Flag the discovery to Claude Code review so the spec's task list can be updated

### Step 1 — Spawn Agents in Parallel

Run one agent per spec target. Each agent works in its own branch:

```sh
# Spawn agents for independent specs (example: 004 and 002 can run in parallel)
codex --branch feat/spec-004-economy    --task "specs/004-economy-resolution/tasks.md"   &
codex --branch feat/spec-002-companies  --task "specs/002-company-refresh/tasks.md"      &
wait
```

- Branch naming: `feat/spec-NNN-short-name`
- Each agent reads its spec's `spec.md`, `plan.md`, and `tasks.md` before writing any code
- Agents must not touch files outside their spec's declared scope (see each `tasks.md` for path conventions)
- Agent commits should reference the task ID: `T004 [spec-004] implement EconomyService shell`

### Step 2 — Agent Opens PR

Each agent opens a PR to `main` when its phase or full spec is complete:

```sh
gh pr create \
  --base main \
  --title "feat(spec-004): Economy & Resolution Phase" \
  --body "Implements tasks T001–T017 from specs/004-economy-resolution/tasks.md"
```

PR body must include:
- Which spec and phase(s) are covered
- Checklist of task IDs completed
- Any stubs or deferred items (with spec dependency noted)
- Manual test results or smoke test log excerpt

### Step 3 — Claude Code Reviews the PR

Claude Code reviews each PR against:

1. **Task checklist** — every non-deferred task ID is implemented
2. **Constitution compliance** — no `FindObjectOfType`, no runtime `AddComponent`, no new `Update` loops, no static mutable state, EventBus/Singleton usage only, no JSON DTO leakage into runtime consumers
3. **Scope containment** — agent did not modify files outside the spec's declared paths
4. **Spec fidelity** — implementation matches the behavior described in `spec.md` (not just compiles)
5. **Test coverage** — EditMode tests written for tasks marked with ⚠️ in `tasks.md`

To trigger a review:

```sh
# Claude Code reviews a PR by number
gh pr view <PR_NUMBER> --json headRefName,body,files | claude "Review this PR against its spec tasks.md and constitution rules. List any violations or missing tasks."
```

### Step 4 — Agent Iterates

If Claude Code leaves review comments:

- Agent reads all unresolved comments before pushing any fix
- Agent pushes a follow-up commit to the same branch — do not force-push; preserve review history
- Agent re-requests review after each fix cycle
- If a comment references a dependency from another spec (e.g., spec 006 not yet merged), the agent adds a `// TODO(spec-NNN):` stub and marks the task as deferred in the PR body

### Step 5 — Merge After All Approvals

Once Claude Code approves a PR (no unresolved comments, all required tasks checked off):

```sh
gh pr merge <PR_NUMBER> --squash --delete-branch
```

Merge order must respect the dependency chain:

```
003 (done) → 004 → 005 → 006 → 007 → 008 → 009
002 (independent)                          ↑
010 ← depends on 004, 006, 008 ───────────┘
```

Never merge a downstream spec before its upstream dependencies are on `main`.

### Step 6 — Update tasks.md Post-Merge

After every PR merges, update the spec's `tasks.md`:

1. Add a status header at the top:
   ```
   Status: MERGED
   PR: #N
   Merged: YYYY-MM-DD
   Merge commit: <SHA>
   ```
2. Mark all completed tasks `[x]`
3. Mark editor-only tasks `[ ]` with `(requires Unity editor)` note
4. List all deferred items with their target spec
5. Add post-merge notes for any architectural decisions made during implementation

This makes the spec folder self-contained — no need to check GitHub to know what was done.

### Parallel Execution Matrix

| Wave | Specs | Can run in parallel? |
|------|-------|----------------------|
| 1 | 002, 004 | Yes — no shared dependencies |
| 2 | 005 | After 004 merges |
| 3 | 006 | After 005 merges |
| 4 | 007 | After 006 merges |
| 5 | 008 | After 007 merges |
| 6 | 009, 010 | Yes — both after 008 merges |

### Rules for Agents

- **Run Step 0 (codebase audit) before writing any code** — never assume the codebase is a blank slate
- Read the full `tasks.md` before writing any code
- One branch per spec; never mix spec scopes in one branch
- Every commit message must start with the task ID (e.g., `T005 [spec-004]`)
- Do not edit `CLAUDE.md`, `specs/`, or `docs/` — design docs are read-only for agents
- If a required upstream file doesn't exist yet (because its spec isn't merged), create a stub interface/class with a `// TODO(spec-NNN): implement when spec NNN merges` comment and proceed
- Do not mark a task `[x]` in `tasks.md` — task tracking lives in the PR checklist, not the file
