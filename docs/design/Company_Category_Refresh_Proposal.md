# Company Category Refresh Proposal

Catalog pre-step completed: `skills/game-element-designer/scripts/refresh-content-catalog.sh` run on 2026-03-01 (UTC) before designing.

## Category Replacement (Sector-First)

- `Entertainment` -> `SocialMedia`
- `TechToys` -> `ConsumerTech`
- New additional categories: `FinTech`, `EnterpriseTech`

Note: Enum IDs for existing authored cards stay stable (`1` and `2`) to preserve current serialized mapping.

## Why This Direction

Your constraint is correct: categories should define common business ground (industry/domain), not mechanical behavior.

This refresh uses technology market verticals as category identity. Mechanics are then derived from what those businesses plausibly do in that world.

## Category Background Stories

### 1) `SocialMedia`
A hyper-competitive attention economy where platforms weaponize trends, creators, and outrage cycles to monetize engagement. Companies here grow fast, crash publicly, and frequently convert chaos into revenue.

### 2) `ConsumerTech`
Apps and devices that promise convenience, wellness, and automation for daily life. These firms win by reducing friction for users, often at the cost of fragile operational complexity.

### 3) `FinTech`
Payments, credit, risk, and pseudo-banking products that turn financial flows into software. These companies profit from timing, leverage, and regulatory gray zones.

### 4) `EnterpriseTech`
B2B infrastructure providers selling reliability, compliance, and workflow optimization to other companies. They scale through contracts, integrations, and system lock-in.

## Proposed Companies (Story First, Then Behavior)

### `SocialMedia` Companies

| Company | Background Story | Behavior Spec (Rule Bent) | Guardrail |
|---|---|---|---|
| **CloutHub Live** | A live-stream platform that spikes whenever drama breaks and monetizes audience pile-ons in real time. | First hit each turn spawns an "Audience Echo" node on adjacent tile; passing through it grants +1 payout on that hit chain. | Max 2 echoes, expire end of turn. |
| **RageLoop Studio** | A short-video network optimized for outrage cycles; controversy is a growth strategy. | Every 3rd hit converts 1 self-HP loss into temporary +2 RPH for all `SocialMedia` this turn. | Max 2 procs/turn, cannot prevent collapse at 1 HP. |
| **TrendNecro Agency** | A creator-management firm that revives failed trends by remixing bankrupt brands. | When adjacent company collapses, gain one "Recycled Hype" stack: next cashout from this company is doubled. | Stack cap 1, consumed on cashout. |
| **CancelShield PR** | A crisis-PR startup that can spin disasters into either subscriber surges or legal invoices. | If this took 4+ hits this turn, resolve volatility at end turn: bonus revenue or fine payment. | Fine floor keeps early EV controlled. |

### `ConsumerTech` Companies

| Company | Background Story | Behavior Spec (Rule Bent) | Guardrail |
|---|---|---|---|
| **AutoPilot Pantry** | A smart-home grocery platform that auto-corrects user mistakes before they notice. | First time ball would miss all companies this turn, redirect once to nearest `ConsumerTech` company. | One redirect per turn per copy. |
| **SleepDebt SaaS** | A wearable-health app that gamifies productivity by selling sleep deprivation as optimization. | If it receives exactly 2 hits this turn, gain +1 permanent RPH; if over-hit, lose extra HP. | Permanent RPH cap +6. |
| **OneTap Butler** | A premium personal-assistant app that imitates competitor features instantly after launch. | On purchase turn, copy cheapest adjacent company's on-hit ability at 50% strength for 2 turns. | Cannot copy unique/legendary-only abilities. |
| **MoodFridge Cloud** | A connected appliance ecosystem that stabilizes household budgets through subscription bundles. | While at full HP, adjacent companies ignore first operational-cost deduction each round. | Breaks immediately when not full HP; no stacking. |

### `FinTech` Companies

| Company | Background Story | Behavior Spec (Rule Bent) | Guardrail |
|---|---|---|---|
| **Loophole Ledger** | A compliance-tech wallet that arbitrages jurisdiction differences and tax timing rules. | Once per turn, nullify first negative market/news modifier targeting this company. | Only first applicable penalty each turn. |
| **DeferredAlpha Capital** | A lending platform that postpones downside to show cleaner quarter-to-quarter growth. | On hit, may defer 1 damage to round end; deferred damage increases cashout value by +15%. | Deferred damage cap 3, always resolves at round end. |
| **CreditKaraoke** | A consumer-credit startup pricing risk from social behavior signals and peer clusters. | Gains temporary RPH when adjacent neighbors are from distinct categories. | Unique-neighbor bonus capped at 3 categories. |
| **AuditFog Exchange** | A pseudo-exchange that hides insolvency signals until reporting windows close. | First collapse each round is hidden until round end; hidden company still pays out during turn. | One hidden collapse per round. |

### `EnterpriseTech` Companies

| Company | Background Story | Behavior Spec (Rule Bent) | Guardrail |
|---|---|---|---|
| **PanicFulfillment OS** | A supply-chain SaaS suite that earns more when client operations are stressed. | Gains RPH per company currently below 50% HP. | Live bonus cap +8 RPH. |
| **LastMile Orchestrator** | A logistics routing engine that instantly reallocates assets when partners fail. | When adjacent company collapses, move into its tile and trigger one free hit payout. | Once per round move trigger. |
| **Shortage Oracle AI** | A forecasting vendor that monetizes prediction gaps in procurement markets. | At round start, predicts one category; if under-hit, gain payout based on shortfall. | Failed prediction applies self-cost. |
| **RepoReaper Systems** | A collateral-management platform that securitizes failing assets into profitable bundles. | Cashing out marks random adjacent company as Collateral: +RPH but extra HP loss on hit. | Mark lasts 2 turns, non-stacking. |

## Quick Synergy / Counterplay Read

- `SocialMedia`: strongest in volatility and collapse-adjacent boards; weak in low-hit control boards.
- `ConsumerTech`: best early stabilizers and consistency tools; weaker in high-chip-damage environments.
- `FinTech`: timing and risk conversion specialists; can underperform in short, low-volatility rounds.
- `EnterpriseTech`: board-state exploiters that scale with stress; weak when board remains healthy/stable.

## Implementation Notes

- Updated enum in [ECompanyCategory.cs](/Users/emre/Desktop/MM-Projects/Pinvestor/Assets/Scripts/Company/ECompanyCategory.cs).
- Existing assets mapped to IDs `1/2` will now read as `SocialMedia` and `ConsumerTech`.
- Before creating cards in `FinTech` and `EnterpriseTech`, add category color/icon entries in `CompanyCardSettings.asset` to avoid missing-setting errors in `Widget_CompanyCard`.

## Risks / Playtest Focus

- Ensure category fantasy is visible in UI text/iconography, not only in mechanics.
- Validate that each category has one clear early-game stabilizer and one late-game payoff.
- Specifically test collapse-chain exploits around `TrendNecro Agency` + `LastMile Orchestrator`.
