# Pinvestor Ecosystem Audit Guide

Use this checklist before proposing any new company/booster/theme/news element.

## 1) Known Source Files

- Always refresh and read:
  - `references/content-catalog.md` (generated snapshot)
  - Refresh command: `./scripts/refresh-content-catalog.sh <repo-root>`
- Core design intent:
  - `docs/design/Pinvestor_Core_Gameplay_Document.md`
- Architecture/implementation boundaries:
  - `docs/design/Pinvestor_Implementation_Architecture.md`
- Runtime config snapshot:
  - `Assets/Resources/GameConfig/game-config.json`
- Current company authored assets:
  - `Assets/ScriptableObjects/Company/Companies/`

## 2) Current Known Company Set (verify each run)

Use `references/content-catalog.md` as the canonical session snapshot.
Do not trust static lists in this file over the generated catalog.

## 3) System Interaction Surfaces to Check

For every new element, explicitly evaluate impact on:
- Offer/placement/launch/resolution turn phases
- Revenue generation and operating cost pressure
- Health loss, collapse timing, and cashout decisions
- Adjacency and board-space constraints
- Booster slot economy and reroll/sell decisions
- Run theme and market news volatility

## 4) Combo/Synergy Mapping Minimum

For each proposal, provide:
- 3 positive synergies with existing or planned elements
- 2 anti-synergies or failure scenarios
- 1 degenerate-loop risk and mitigation

## 5) Behavior-First Guard

Reject proposals that can be summarized only as stat modifiers.
Good proposals should read as conditional behaviors, priority rules, timing twists, or system rewrites with constraints.

## 6) Practical Validation Questions

- What board state makes this element strongest?
- What board state makes it weak or awkward?
- What player expression does it unlock that was not possible before?
- Which tuning knobs can reduce power without deleting the core fantasy?
