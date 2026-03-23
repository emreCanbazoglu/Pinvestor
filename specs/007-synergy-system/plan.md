# Plan: Synergy System

**Spec**: `specs/007-synergy-system/spec.md`
**Created**: 2026-03-24

## Technical Approach

`BoardAdjacencyService` queries the board grid for orthogonal neighbors by industry tag. `SynergyEvaluator` reads the adjacency results and determines which companies qualify for RPH or health bonuses. RPH bonuses are applied as modifiers into the revenue accumulation pipeline (spec 004). Health bonuses are applied once on placement via the health state from spec 006. Synergy state is recalculated on board-change events — no polling.

## Architecture Decisions

### Adjacency as a Pure Query Service
`BoardAdjacencyService` is stateless — it computes adjacency on demand from the current board state. No caching of adjacency state. Fast enough for a small board.

**Why not cache?** Cache invalidation on collapse/cashout/placement events adds complexity. The board is small; recomputing is trivial.

### Synergy State as a Snapshot, Not Continuous
At the start of each Launch Phase, `SynergyEvaluator` computes the current `SynergyState` snapshot (which companies have RPH bonus). This snapshot is used by `TurnRevenueAccumulator` for the duration of that launch phase. No mid-launch recalculation.

**Why snapshot at launch start?** A company could collapse mid-launch, breaking a synergy pair. Recalculating mid-flight adds complexity for negligible gameplay difference. Snapshot at launch start is simpler and deterministic.

### Check Board/Property Before Building RphModifier
`Assets/Scripts/Board/Property/` may already have a modifier/property system for board items. If so, `SynergyRphModifier` should plug into that system rather than creating a parallel modifier stack. Read `Board/Property/` before implementing.

### Health Bonus as One-Time Grant on Placement
When a company is placed and completes or joins a 3+ cluster, it receives a one-time `+N health` grant via the health state (spec 006). This grant is not removed if the cluster later breaks. This is intentional — synergy rewards the decision to cluster, not the continuous maintenance of the cluster.

**Cluster detection**: BFS/DFS over board cells with matching industry tag. O(board size) — trivial.

### Recalculation Trigger via EventBus
Subscribe to `CompanyCollapsedEvent`, `CompanyCashedOutEvent`, and a new `CompanyPlacedEvent` (or the board's existing placement event). On any of these, the next Launch Phase will use a fresh adjacency snapshot. No immediate re-evaluation needed.

## Phase Breakdown

### Phase 1: BoardAdjacencyService
Implement orthogonal neighbor query on the board grid. Returns list of same-industry neighboring companies for a given cell. Read `Board.cs` and `Board/Cell/` first.

### Phase 2: SynergyEvaluator — RPH Bonus
Evaluate RPH bonus eligibility: company has >= 1 same-industry orthogonal neighbor → qualifies. Produce `SynergyState` snapshot.

### Phase 3: RPH Modifier Integration
`SynergyRphModifier`: applied to `TurnRevenueAccumulator` for qualifying companies. Bonus value from GameConfig balance section. Check `Board/Property/` for existing modifier pattern first.

### Phase 4: Cluster Detection (Health Bonus)
BFS/DFS cluster detection: connected same-industry group of size >= 3. Run on placement.

### Phase 5: Health Bonus Grant on Placement
On company placement, if it joins a cluster of size >= 3, grant `+N health` to all companies in cluster that haven't already received the grant. Uses health state from spec 006.

### Phase 6: Event-Driven Recalculation
Subscribe to placement/collapse/cashout events. No action needed beyond ensuring next snapshot is fresh — since snapshot is computed at each launch start, subscriptions may only be needed for the health bonus (grant on placement event).

### Phase 7: Tests & Validation
EditMode tests: adjacency queries, RPH bonus eligibility, cluster BFS correctness, health bonus grant. Manual smoke test.

## Key Risks

- **Board/Property modifier system**: If it exists, synergy RPH modifiers must use it. Building a parallel modifier causes conflicts with spec 008 (boosters) which will also use modifiers.
- **Cluster health bonus idempotency**: Track which companies have already received the cluster health grant to avoid re-granting when a new company joins an already-qualified cluster.
- **Industry tag equality**: Industry tags are compared by value — confirm whether `IndustryTag` is a string, enum, or ScriptableObject reference. Reference equality vs value equality matters for adjacency matching.
