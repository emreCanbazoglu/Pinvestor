# Tasks: Synergy System

**Input**: `specs/007-synergy-system/spec.md`, `plan.md`
**Codebase audit required**: `Board.cs`, `Board/Cell/`, `Board/Property/`, `Board/Core/`, `RevenueGenerator.cs`, `GameConfigDomainModels.cs`

## Phase 1: Adjacency Service

- [ ] T001 Read `Assets/Scripts/Board/Board.cs`, `Assets/Scripts/Board/Cell/`, `Assets/Scripts/Board/Core/`, and `Assets/Scripts/Board/Property/` fully before writing any code — neighbor queries and property/modifier systems may already exist
- [ ] T002 Create `Assets/Scripts/Game/Synergy/BoardAdjacencyService.cs` — stateless service with `GetSameIndustryNeighbors(boardPosition, industryTag)` returning all orthogonal neighbors matching the given industry. Use board's existing cell/occupancy API.
- [ ] T003 Confirm `IndustryTag` equality method (string compare, enum, or reference) — document the comparison approach in a code comment

**Checkpoint**: Adjacency queries return correct orthogonal same-industry neighbors for any board position

---

## Phase 2: Synergy Evaluator — RPH Bonus

- [ ] T004 Create `Assets/Scripts/Game/Synergy/SynergyState.cs` — per-company flags: `HasRphBonus` (bool). Computed fresh per launch phase, not persisted.
- [ ] T005 Create `Assets/Scripts/Game/Synergy/SynergyEvaluator.cs` — `ComputeSynergySnapshot(IEnumerable<PlacedCompany>)`: for each placed company, use `BoardAdjacencyService` to check if it has >= 1 same-industry orthogonal neighbor. Returns a `Dictionary<companyInstanceId, SynergyState>`.
- [ ] T006 Call `SynergyEvaluator.ComputeSynergySnapshot()` at the start of `RunLaunchPhase()` in `Assets/Scripts/Game/Turn.cs` — store snapshot in turn context for use during revenue accumulation

**Checkpoint**: Snapshot correctly identifies companies with same-industry neighbors before each launch

---

## Phase 3: RPH Modifier Integration

- [ ] T007 Read `Assets/Scripts/Board/Property/` to check for an existing board item modifier/property system before implementing — if one exists, plug `SynergyRphModifier` into it
- [ ] T008 Add synergy RPH bonus value to GameConfig balance section (field: `SynergyRphBonus`) — update config DTO, mapper, and export service
- [ ] T009 Create `Assets/Scripts/Game/Synergy/SynergyRphModifier.cs` — a modifier applied during revenue accumulation; adds `SynergyRphBonus` flat to RPH for qualifying companies
- [ ] T010 Integrate `SynergyRphModifier` into `Assets/Scripts/Game/Economy/TurnRevenueAccumulator.cs` — apply modifier per-company when `SynergyState.HasRphBonus` is true for that company

**Checkpoint**: Companies with synergy earn increased RPH; non-qualifying companies are unaffected

---

## Phase 4: Cluster Detection (Health Bonus)

- [ ] T011 Add cluster detection to `BoardAdjacencyService`: `GetCluster(boardPosition, industryTag)` — BFS/DFS returning all connected same-industry companies. Returns list of board positions.
- [ ] T012 Add cluster health bonus value to GameConfig balance section (field: `SynergyClusterHealthBonus`) — update config DTO, mapper, and export service

**Checkpoint**: Cluster detection correctly identifies connected groups of 3+ same-industry companies

---

## Phase 5: Health Bonus Grant on Placement

- [ ] T013 Subscribe to a company-placed event (or call directly from the placement phase) in `SynergyEvaluator`: after each placement, run cluster detection on the newly placed company
- [ ] T014 If the newly placed company is part of a cluster of size >= 3, grant `+SynergyClusterHealthBonus` health to each company in the cluster that has not yet received the grant — use `CompanyHealthState.GrantBonusHealth(n)` (add this method to spec 006's health state if not present)
- [ ] T015 Track which company instances have already received the cluster health grant to prevent double-granting when a new company joins an already-qualified cluster

**Checkpoint**: Companies in 3+ clusters receive health bonus once on placement; bonus is not repeated

---

## Phase 6: Tests & Validation

- [ ] T016 [P] Add EditMode test for `BoardAdjacencyService`: orthogonal neighbors only, same-industry filtering, edge cells (corners, borders)
- [ ] T017 [P] Add EditMode test for `SynergyEvaluator` RPH snapshot: company with 0 same-industry neighbors → no bonus; company with 1+ → bonus
- [ ] T018 [P] Add EditMode test for cluster BFS: cluster of 2 → no health bonus; cluster of 3 → all receive grant; adding 4th to existing cluster → only new company gets grant (others already granted)
- [ ] T019 Manual smoke test: place 2 same-industry companies adjacent → verify RPH bonus in next launch; place 3 same-industry in a cluster → verify health bonus granted
