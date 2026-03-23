# Spec 010: Market News System

**Status**: Planned
**Created**: 2026-03-24
**Dependencies**: Spec 004 (economy — RPH/cost effects), Spec 006 (health — temporary health effects), Spec 008 (booster modifier framework), Spec 009 (run theme — modifier targeting model)

## Overview

Implement Market News: automatic, temporary, unpredictable events that trigger during resolution and last 1–2 turns. They can target the global economy or specific industries, introducing volatility that stress-tests the player's build. Unlike boosters and themes, Market News is not player-controlled.

## Codebase Audit (check before implementing)

Agents **must** read these files before writing any code:

- `Assets/Scripts/Game/Turn.cs` — `RunResolutionPhase()` is where market news triggers and expires
- `Assets/Scripts/Game/RunCycle/Round.cs` — round-level timing for news events
- `Assets/Scripts/GameConfig/Runtime/Models/GameConfigDomainModels.cs` — check for any existing market news / event models
- `Assets/Scripts/GameConfig/Editor/DomainEditors/` — existing domain editors; a `MarketNewsConfigDomainEditor` will be needed
- Modifier framework from Spec 008/009 — news effects reuse this, not a new stack
- `Assets/Scripts/Game/Events/` — EventBus events; `MarketNewsStartedEvent`, `MarketNewsExpiredEvent` will be added here

## User Stories

### US1 — News Triggers During Resolution (P1)

As the game, Market News events must trigger automatically during the Resolution Phase based on defined conditions so the player cannot predict or prevent them.

**Acceptance criteria:**
- News events are evaluated at the end of each Resolution Phase
- Trigger modes supported: random chance (configurable %), fixed turn interval, board condition (e.g., >= N companies of one industry)
- At most 1 active news event at a time (for now)
- When a news event triggers, a `MarketNewsStartedEvent` is emitted on EventBus with event type and duration

### US2 — Temporary Duration (1–2 Turns) (P1)

As the game, Market News effects must expire automatically after their defined duration so disruptions are bounded and the player can adapt.

**Acceptance criteria:**
- Each news event has a `durationTurns` value (1 or 2, from config)
- Duration counts down at the start of each Resolution Phase
- At 0, the event expires: effects are removed, `MarketNewsExpiredEvent` is emitted
- Expired events can be replaced by a new event on the same turn if conditions are met

### US3 — Industry-Specific and Global Targeting (P1)

As the game, Market News events can target either all companies globally or only companies of a specific industry tag so the system reinforces the investment fantasy of sector-specific disruption.

**Acceptance criteria:**
- Each news event has a `targetType`: Global or SpecificIndustry
- For SpecificIndustry, the affected tag is either fixed in config or randomly selected at trigger time
- Effects apply only to companies matching the target
- Industry targeting uses the same `IndustryTag` field as the synergy system

### US4 — News Effect Types (P1)

Market News events apply temporary modifiers to the economy, health, or operational costs of affected companies.

**Required event types (minimum 6):**

| Name | Target | Effect | Duration |
|---|---|---|---|
| **Industry Surge** | Specific industry | RPH ×2 for targeted industry | 2 turns |
| **Regulatory Investigation** | Specific industry | RPH ×0.5 for targeted industry | 2 turns |
| **Supply Chain Disruption** | Global | All operational costs +50% | 1 turn |
| **Market Crash** | Global | All RPH ×0.5 | 2 turns |
| **Funding Boom** | Specific industry | All companies in industry gain +1 health | 1 turn |
| **Hostile Acquisition** | Specific industry | One random company in the targeted industry loses 2 health immediately | 1 turn |

**Acceptance criteria:**
- Each event type applies and removes its modifier correctly
- Values are config-driven (not hardcoded)
- Effects stack correctly with booster and theme modifiers (no overwrite conflicts)

### US5 — News Reveal UI (P1)

As the player, I need to see when a Market News event triggers and what it does so I can adapt my next turn's strategy.

**Acceptance criteria:**
- A UI banner/modal appears when news triggers showing: event name, description, affected target, and duration
- Banner also appears when the event expires
- Banner is non-blocking (auto-dismisses after N seconds or on player tap)

### US6 — News Data in GameConfig (P2)

As the config system, Market News events must be authored through the GameConfig pipeline so designers can add events without code changes.

**Acceptance criteria:**
- News event pool is part of `GameConfigRoot`
- Editor domain panel for Market News in `GameConfigEditorWindow`
- Event pool exports to `game-config.json`
- `GameConfigManager` provides a market news pool accessor

## Scope

**In scope:**
- `MarketNewsEvent` config model: type, target, duration, effect values
- `ActiveMarketNewsState` runtime model: current event, turns remaining
- `MarketNewsResolver` service: evaluates trigger conditions, selects event, manages duration countdown
- Temporary modifier application/removal (reuses booster/theme modifier framework)
- `MarketNewsStartedEvent` and `MarketNewsExpiredEvent` on EventBus
- News reveal and expiry UI banner (non-blocking)
- 6 news event types implemented
- Market News config domain editor

**Out of scope:**
- Multiple simultaneous news events (future iteration)
- Player ability to mitigate/block news (booster territory)
- News events requiring systems beyond spec 009 (e.g., full bankruptcy)
- Animated news banners

## Key Entities

| Entity | Type | Purpose |
|---|---|---|
| `MarketNewsEventConfig` | Config domain model | Event type, target type, duration, modifier values |
| `ActiveMarketNewsState` | Runtime model | Currently active event + turns remaining |
| `MarketNewsResolver` | Runtime service | Trigger evaluation, event selection, duration management |
| `MarketNewsModifierApplicator` | Runtime service | Applies/removes news modifiers via shared modifier framework |
| `MarketNewsStartedEvent` | EventBus event | Emitted when a news event activates |
| `MarketNewsExpiredEvent` | EventBus event | Emitted when a news event expires |
| `MarketNewsBannerPanel` | UI View | Non-blocking banner displayed on trigger and expiry |

## Notes

- News must NOT use a dedicated `Update` loop for duration tracking — it counts down via Resolution Phase hooks only
- The modifier framework must support temporary modifiers with an expiry callback; if spec 008/009 built permanent-only modifiers, this spec should extend it to support temporary ones
- Random news selection should use a seeded source consistent with the rest of the game's RNG
- "Hostile Acquisition" targets a random company — if there are no companies of the targeted industry, the event skips gracefully (no crash)
