# Spec 007: Synergy System

**Status**: Planned
**Created**: 2026-03-24
**Dependencies**: Spec 004 (economy — RPH calculation hooks), Spec 005 (companies placed on board with industry tags), Spec 006 (health system — health bonuses)

## Overview

Implement adjacency-based synergy bonuses between companies on the board. When companies of the same industry are placed adjacent to each other, they gain RPH bonuses (2+ adjacent) and health bonuses (3+ cluster). This system encourages spatial clustering and specialization without requiring new phases.

## Codebase Audit (check before implementing)

Agents **must** read these files before writing any code:

- `Assets/Scripts/Board/Board.cs` — board grid and cell access; neighbor/adjacency queries may already exist
- `Assets/Scripts/Board/Cell/` — cell data, occupancy state
- `Assets/Scripts/Board/BoardItem/` — board item wrappers; adjacency may already be partially computed
- `Assets/Scripts/Board/Property/` — board item property system (RPH modifiers may plug in here)
- `Assets/Scripts/Board/Core/` — core board abstractions
- `Assets/Scripts/GameConfig/Runtime/Models/GameConfigDomainModels.cs` — `CompanyConfigModel` has industry tag
- `Assets/Scripts/GameConfig/Runtime/GameConfigServices.cs` — company config service
- `Assets/Scripts/RevenueGenerator/RevenueGenerator.cs` — revenue per hit; synergy RPH bonus must modify this calculation
- `Assets/Scripts/GameplayAbilitySystem/` — check if synergy bonuses can be expressed as ability effects

## User Stories

### US1 — Adjacency Detection (P1)

As the synergy system, I need to know which board cells are occupied by companies of the same industry and which of those are adjacent to each other, so synergy bonuses can be computed correctly.

**Acceptance criteria:**
- Adjacency = orthogonal neighbors only (up/down/left/right, no diagonals) — for now
- Adjacency query returns all same-industry neighbors for a given board position
- Adjacency state is recomputed whenever the board changes (company placed, collapsed, cashed out)
- Query is efficient for a small board (no optimization required for MVP)

### US2 — RPH Bonus for 2+ Same-Industry Adjacent (P1)

As the economy, when 2 or more same-industry companies are adjacent on the board, each of those companies gets a modified RPH during revenue calculation so the player is rewarded for clustering.

**Acceptance criteria:**
- Any company with at least 1 same-industry orthogonal neighbor receives an RPH bonus
- Bonus value is sourced from GameConfig balance section (not hardcoded)
- Bonus applies per-company per-hit during the Launch Phase
- If a company later loses its same-industry neighbor (via collapse or cashout), the bonus no longer applies next turn

### US3 — Health Bonus for 3+ Same-Industry Cluster (P1)

As the health system, when 3 or more same-industry companies form a connected cluster on the board, each company in the cluster gains bonus max health so dense industry clusters are more resilient.

**Acceptance criteria:**
- Cluster = connected group of same-industry companies (graph connectivity, not just pairs)
- Cluster size >= 3 → each company in the cluster gets +N bonus health (N from GameConfig balance)
- Bonus health is applied when a company joins or completes a cluster (on placement)
- Bonus health is NOT retroactively removed if a cluster breaks mid-run (health already granted stays)
- New companies placed into an existing cluster receive the bonus immediately on placement

### US4 — Synergy State Recalculation on Board Change (P2)

As the synergy system, synergy bonuses must reflect the current board state after any change so stale bonuses don't persist after a collapse or cashout.

**Acceptance criteria:**
- RPH synergy state is recalculated at the start of each Launch Phase (or each hit)
- Board change events (`CompanyCollapsedEvent`, `CompanyCashedOutEvent`, company placed) trigger adjacency recalculation
- Synergy state is read-only to consumers; only the synergy system writes it

## Scope

**In scope:**
- `BoardAdjacencyService`: computes adjacency and cluster state for the current board
- `SynergyEvaluator`: evaluates which companies qualify for RPH or health bonuses
- `SynergyState` runtime model: holds current bonuses per company
- RPH bonus applied in revenue calculation (hooks into spec 004's `TurnRevenueAccumulator`)
- Health bonus applied on placement (hooks into spec 006's health state)
- Synergy values (bonus amounts) in GameConfig balance section

**Out of scope:**
- Diagonal adjacency (future option)
- Penalty for mixed-industry adjacency (run theme modifier territory, spec 009)
- Booster modifiers on synergy (spec 008)
- Visual synergy indicators on board (can be added later, not required here)

## Key Entities

| Entity | Type | Purpose |
|---|---|---|
| `BoardAdjacencyService` | Runtime service | Queries the board for same-industry neighbors and clusters |
| `SynergyEvaluator` | Runtime service | Determines RPH bonus eligibility and cluster health bonus grants |
| `SynergyState` | Runtime model | Per-company synergy flags: hasRphBonus, clusterHealthGranted |
| `SynergyRphModifier` | RPH modifier | Applied during revenue accumulation for companies with synergy |

## Notes

- Check `Board/Property/` carefully — a property/modifier system for board items may already exist that can carry synergy RPH modifiers
- Synergy must not hardcode industry values; it reads `IndustryTag` from `CompanyConfigModel`
- Cluster health bonus is a one-time grant on placement, not a continuous buff — simpler to implement and avoids tracking removal
- Synergy recalculation should be event-driven (board change events), not polled every frame
