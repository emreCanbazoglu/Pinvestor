# Plan: Booster Shop

**Spec**: `specs/008-booster-shop/spec.md`
**Created**: 2026-03-24

## Technical Approach

`ShopPlaceholderRoundPhase` is replaced by `BoosterShopRoundPhase`. The shop phase is async and awaits the player closing the shop (same UniTaskCompletionSource pattern as offer phase). A `BoosterModifierFramework` defines `IBoosterEffect` with `Apply(context)` / `Remove(context)` — this is the shared modifier foundation that spec 009 (themes) and spec 010 (market news) will also use. Each of the 8 booster cards is a `BoosterDefinitionSO` (ScriptableObject) with an associated `IBoosterEffect` implementation. `PlayerBoosterState` tracks owned boosters and slot capacity.

## Architecture Decisions

### IBoosterEffect as the Core Modifier Contract
All booster effects implement `IBoosterEffect` with `Apply(IGameContext)` and `Remove(IGameContext)`. This contract is deliberately general — spec 009 theme modifiers and spec 010 temporary news modifiers will also implement it. The framework is built once here.

**Effect application**: `Apply` is called on purchase. `Remove` is called on sell. Effects hook into the relevant system through the `IGameContext` service locator (or direct service access via Singleton patterns already in use).

### BoosterDefinitionSO — Data Only
`BoosterDefinitionSO` is authoring data: name, description, cost, sell value, `effectType` enum (maps to a concrete `IBoosterEffect` implementation). It does NOT contain behavior logic. Effect implementations are plain C# classes instantiated by a `BoosterEffectFactory` keyed on `effectType`.

**Why not put logic in SO?** ScriptableObjects with logic are harder to test and can't be instantiated independently. Separating data (SO) from behavior (plain class) follows the project's constitution.

### PlayerBoosterState — Singleton-Accessible
Follows `GameConfigManager` / `PlayerEconomyState` pattern. Holds `List<OwnedBooster>` (definition + active effect instance) and `slotCapacity`. Read-only to consumers; only `BoosterShopService` writes to it.

### Shop UI Awaits Close Action
`BoosterShopRoundPhase` opens the shop panel and awaits a `UniTaskCompletionSource` resolved when the player taps "Close Shop". Buy/sell/reroll actions are synchronous within the open panel — they update state immediately, no async needed.

### Booster Pool Per Run
`RunBoosterPool` is initialized at run start from the full booster library. Already-owned boosters are filtered from future offers (no buying duplicates). Pool does not replenish — what's drawn is gone from the pool.

### The 8 Boosters — Effect Mapping

| Booster | Effect Class | Primary Hook |
|---|---|---|
| Overclock | `OverclockBoosterEffect` | RPH ×2, op-cost ×2 — modifies `EconomyService` multipliers |
| Dead Cat Bounce | `DeadCatBounceBoosterEffect` | Subscribes to `CompanyCollapsedEvent`, triggers free ball launch |
| Vulture Fund | `VultureFundBoosterEffect` | Subscribes to `CompanyCollapsedEvent`, credits 20% valuation |
| Skeleton Crew | `SkeletonCrewBoosterEffect` | Op-cost ×0.5, all new placements get -1 max health |
| Margin Call | `MarginCallBoosterEffect` | Allows 2 placements per turn, each at +50% op-cost |
| Diversification | `DiversificationBoosterEffect` | Disables industry RPH synergy; adds flat RPH bonus = unique industry count |
| Too Big To Fail | `TooBigToFailBoosterEffect` | Subscribes to collapse check; intercepts companies at health=1 |
| Hot Money | `HotMoneyBoosterEffect` | Cashout rate = 100%; adds 10% net worth deduction per resolution |

## Phase Breakdown

### Phase 1: Booster Modifier Framework
Define `IBoosterEffect`, `IGameContext` (or use existing service access patterns), `BoosterEffectFactory`. This is the shared foundation — build it cleanly as it will be extended by specs 009 and 010.

### Phase 2: Booster Authoring
Create `BoosterDefinitionSO` base. Author all 8 booster ScriptableObjects with name, description, cost, sell value, effect type.

### Phase 3: PlayerBoosterState & Slot System
`PlayerBoosterState`: owned boosters list, slot capacity (from GameConfig shop section), read API. `BoosterShopService`: buy/sell/reroll logic, writes to `PlayerBoosterState`.

### Phase 4: Shop Phase Replacement
Replace `ShopPlaceholderRoundPhase` with `BoosterShopRoundPhase`. Open panel, await close. Build panel with offer draw on open.

### Phase 5: Shop UI
`BoosterShopPanel` + `BoosterShopVM`: 3 offered booster cards, owned slots display, reroll button (with cost), close button. `BoosterCardWidget` for individual booster display.

### Phase 6: 8 Booster Implementations
Implement each booster's `IBoosterEffect` class. Test each in isolation. Hook into appropriate systems.

### Phase 7: Integration & Tests
Wire `PlayerBoosterState` initialization at run start. EditMode tests for buy/sell/reroll state, slot limits, booster effect application/removal.

## Key Risks

- **IBoosterEffect hook points**: Each effect needs access to specific runtime services. Define `IGameContext` carefully — it must provide access to `PlayerEconomyState`, `EconomyService`, EventBus, board, etc. without becoming a god object. Consider explicit constructor injection per effect class instead.
- **Dead Cat Bounce triggers a ball launch**: Launching a ball mid-resolution is a significant side effect. Ensure the resolution phase can accommodate a nested launch or defer the free ball to the next turn start. Decide early.
- **Diversification disables synergy**: This booster must interact with spec 007's `SynergyEvaluator`. The synergy system must check `PlayerBoosterState` for active Diversification, or the booster's `Apply` must register a flag the synergy system reads. Design the hook before implementing either system.
- **Modifier stacking order**: Define the multiplication order for RPH modifiers (synergy → booster → theme → news) before any modifier is built, so all specs use the same chain.
