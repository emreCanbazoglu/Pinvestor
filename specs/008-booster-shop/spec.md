# Spec 008: Booster Shop

**Status**: Planned
**Created**: 2026-03-24
**Dependencies**: Spec 004 (economy — player money), Spec 003 (shop placeholder phase), Spec 007 (synergy — modifier framework context)

## Overview

Replace the `ShopPlaceholderRoundPhase` with a real Booster Shop. After each turn's resolution, the player enters a shop where they can buy boosters from a pool of 3 offered cards, sell existing boosters, and reroll the offer for a cost. Boosters persist for the run and actively modify game rules — they are the primary build-expression system.

This spec includes both the shop mechanics and a first set of implemented booster cards to demonstrate the rule-bending system.

## Codebase Audit (check before implementing)

Agents **must** read these files before writing any code:

- `Assets/Scripts/Game/RunCycle/RoundPhases.cs` — `ShopPlaceholderRoundPhase` is the replacement target
- `Assets/Scripts/Game/RunCycle/RoundContext.cs` — shop phase receives this context
- `Assets/Scripts/Game/GameFSM/GameManager.cs` — shop phase is wired here via `BuildRoundPhases()`
- `Assets/Scripts/CardSystem/` — card system patterns for booster cards (authoring, piles, core)
- `Assets/Scripts/GameConfig/Runtime/Models/GameConfigDomainModels.cs` — shop config section (slots, reroll cost, etc.)
- `Assets/Scripts/GameConfig/Editor/DomainEditors/ShopConfigDomainEditor.cs` — existing shop domain editor
- `Assets/Scripts/UI/` — UIBase, VMBase, WidgetBase patterns
- `Assets/Scripts/RevenueGenerator/RevenueGenerator.cs` — booster economy modifiers hook here
- `Assets/Scripts/GameplayAbilitySystem/` — rule-bending boosters may use the ability system

## User Stories

### US1 — Shop Phase Replaces Placeholder (P1)

As the game loop, after each turn's resolution, the player must enter a real Booster Shop so they can build their engine over time.

**Acceptance criteria:**
- `ShopPlaceholderRoundPhase` is replaced by `BoosterShopRoundPhase`
- Shop phase is async and waits for the player to close/leave the shop
- Shop state (offered boosters, player's owned boosters, money) is correctly initialized each turn
- Closing the shop advances to the next turn/round

### US2 — Buy a Booster (P1)

As the player, I want to buy a booster from the 3 offered options so I can modify how my run plays.

**Acceptance criteria:**
- 3 boosters are drawn from the booster pool and displayed each shop phase
- Each booster shows: name, description, cost, and effect summary
- Player can buy a booster if they have sufficient net worth and a free slot
- Buying deducts the booster's cost from net worth and adds it to the player's active booster slots
- Booster is immediately active after purchase

### US3 — Sell a Booster (P1)

As the player, I want to sell an owned booster to reclaim some capital and free a slot so I can adapt my build mid-run.

**Acceptance criteria:**
- Player can sell any owned booster during the shop phase
- Sell returns `sell_value` (e.g., 50% of purchase cost, from config) to net worth
- Sold booster is removed from active slots immediately
- Its effects no longer apply from the next turn

### US4 — Reroll Offer (P1)

As the player, I want to reroll the 3 offered boosters for a cost so I can hunt for specific synergies.

**Acceptance criteria:**
- Reroll costs a fixed amount from net worth (from GameConfig shop section)
- Rerolling draws 3 new boosters from the pool (not duplicating already-owned boosters)
- Multiple rerolls per shop phase are allowed (as long as player can afford them)

### US5 — Booster Slot Limit (P1)

As the game, the player's active booster count must be limited by slots so boosters feel scarce and build choices are meaningful.

**Acceptance criteria:**
- Default slot count = 3 (from GameConfig shop section, expandable by specific boosters)
- Player cannot buy a booster when all slots are full (buy button disabled with explanation)
- Slot count is visible in the shop UI

### US6 — Implemented Booster Set (P1)

To demonstrate the system, a first set of boosters must be implemented and available in the pool. These should cover diverse modifier categories and show the rule-bending philosophy.

**Required booster cards (minimum 8):**

| Name | Category | Effect |
|---|---|---|
| **Overclock** | Economy | Each company's RPH is doubled, but operational cost is also doubled |
| **Dead Cat Bounce** | Collapse | When a company collapses, it fires one free ball before being removed |
| **Vulture Fund** | Economy | Each time any company collapses, gain 20% of its remaining valuation |
| **Skeleton Crew** | Op-Cost | All operational costs are reduced by 50%, but all companies start with -1 max health |
| **Margin Call** | Placement | You may place 2 companies per turn instead of 1, but each costs +50% op-cost |
| **Diversification** | Synergy | Same-industry RPH synergy bonus is removed; instead every company gains a flat RPH bonus equal to the number of different industries on the board |
| **Too Big To Fail** | Health | Companies with health = 1 cannot collapse — they survive at 1 health indefinitely (but still deduct op-costs) |
| **Hot Money** | Cashout | Cashout rate increased to 100%, but you lose 10% of net worth each turn as "heat tax" |

**Acceptance criteria for each booster:**
- Booster effect is active immediately after purchase
- Effect correctly modifies the relevant system (RPH calc, collapse logic, cashout, etc.)
- Booster description in UI is clear and matches actual behavior
- Booster is authored as a ScriptableObject or config entry (not hardcoded)

## Scope

**In scope:**
- `BoosterShopRoundPhase` replacing `ShopPlaceholderRoundPhase`
- Booster pool and draw logic (similar to company pool in spec 005)
- `PlayerBoosterState`: active boosters, slot count, slot capacity
- Buy/sell/reroll mechanics
- Booster Shop UI: offered cards, owned booster slots, reroll button, close button
- Booster modifier framework: how booster effects plug into existing systems (RPH, collapse, cashout, health, placement)
- 8 booster cards implemented and functional (see table above)
- Booster authoring asset format (ScriptableObject or config entry)

**Out of scope:**
- Booster effects that require systems not yet implemented (e.g., Market News interaction)
- Booster slot expansion mechanic (can be a booster itself, but the slot capacity modifier is in scope as part of the slot system)
- Animated booster transitions

## Key Entities

| Entity | Type | Purpose |
|---|---|---|
| `BoosterShopRoundPhase` | Round phase | Replaces placeholder; async, awaits player close |
| `RunBoosterPool` | Runtime model | Available booster cards for the run |
| `BoosterOfferDrawer` | Runtime service | Draws 3 boosters from pool per shop open |
| `PlayerBoosterState` | Runtime model | Active boosters, slot count, capacity |
| `BoosterBase` | ScriptableObject base | Authoring base for all boosters (name, cost, sell value, description) |
| `IBoosterEffect` | Interface | Contract for booster runtime effect (Apply/Remove) |
| `BoosterShopVM` / `BoosterShopPanel` | UI VM + View | Shop panel with offered cards, owned slots, reroll/close actions |
| `BoosterCardWidget` | UI Widget | Displays a single booster card |

## Notes

- Booster effects should use the same modifier/event pattern already in the ability system where possible — check `GameplayAbilitySystem/` before building a parallel modifier stack
- `PlayerBoosterState` must NOT use static mutable state; it should be accessible via a runtime service or Singleton
- Booster costs and reroll cost come from GameConfig `ShopConfigSection` — do not hardcode
- The 8 required boosters are a minimum; additional boosters can be designed using the game-element-designer skill
