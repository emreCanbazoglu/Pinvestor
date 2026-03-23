# Pinvestor Game Elements Design

Catalog pre-step completed: `skills/game-element-designer/scripts/refresh-content-catalog.sh` run on 2026-03-22 (UTC) before designing.

## Category Replacement (Sector-First)

- `Entertainment` → `SocialMedia`
- `TechToys` → `ConsumerTech`
- New additional categories: `FinTech`, `EnterpriseTech`

> **Legacy retirement:** `Cashnado`, `StackRabbit`, and `VirtosoXR` are removed from the game. `CloutHub Live` is a rename+rework of the authored `CloutHub` asset — update the ScriptableObject in place rather than creating a new slot. All other new companies are net-new authored assets.
>
> **Enum note:** Enum IDs `1` and `2` are reassigned to `SocialMedia` and `ConsumerTech`. Legacy serialized references to the old category values must be cleared when retiring the old assets.

---

## Why This Direction

Categories define common business ground (industry/domain), not mechanical behavior. Mechanics are derived from what those businesses plausibly do in that world.

A company should suggest a play pattern the player can discover and feel clever for exploiting. Same-industry boards should create stronger, more legible combo lines. The best runs should be streamable: spectators can instantly understand "the scandal engine board," "the debt shell game board," or "the automation safety net board."

---

## Ecosystem Snapshot For This Proposal

**Known from current repo state:**
- Runtime config currently includes `Cashnado`, `CloutHub`, and `Stack Rabbit` — all three are being retired.
- Authored asset categories still reflect the older taxonomy (`Entertainment`, `Gaming`, `Lifestyle`, `TechToys`) — these are superseded by this proposal.
- Booster, run theme, and market news catalogs are still largely future-facing in authored content.

**Inferred for this proposal:**
- This document defines content direction and category identity before full asset implementation.
- Category design optimizes first for behavior space, combo readability, and future card ecosystem growth.

---

## Design Lens For Company Cards

- **Backstory first:** each company needs a business fantasy strong enough to explain why its rules are a little unfair.
- **Behavior second:** the ability should push the player toward a distinct layout, timing, or risk pattern.
- **Synergy visible on board:** the strongest combos should emerge from adjacency, collapse timing, hit routing, and industry clustering.
- **Smart-player moments:** cards should reward setup, sequencing, and reading the board, not just raw number inflation.
- **Fast joke read:** the company should sound funny in one sentence, not after a paragraph.

---

## Category Background Stories

### `SocialMedia`
A hyper-competitive attention economy where platforms weaponize trends, creators, and outrage cycles to monetize engagement. These companies turn attention spikes, scandals, and public meltdowns into business opportunities.

### `ConsumerTech`
Apps and devices that promise convenience, wellness, and automation for daily life. Their public promise is comfort and efficiency; under the hood they are fragile subscription machines held together by predictive UX and habit capture.

### `FinTech`
Payments, credit, risk, and pseudo-banking products that turn financial flows into software. These firms make money by reclassifying danger, postponing consequences, and convincing users that liquidity is the same thing as safety.

### `EnterpriseTech`
B2B infrastructure providers selling reliability, compliance, and workflow optimization to other companies. Their real moat is operational dependence: once a board leans on them, the whole machine starts routing through their systems.

---

## Run Identity Targets By Category

| Category | Run Feel | Player Rewarded For |
|---|---|---|
| `SocialMedia` | Explosive, messy, opportunistic | Creating collapses, volatility spikes, spectacle |
| `ConsumerTech` | Smooth, controlled, self-correcting | Exact placement, hit routing, polished board states |
| `FinTech` | Manipulative, greedy | Delayed risks, hiding weaknesses, oversized cashouts |
| `EnterpriseTech` | Orchestrating a machine | Dependency webs where every failure produces secondary value |

---

## Category Engine Roles

| Category | Roles |
|---|---|
| `SocialMedia` | Attention concentrator · Outrage scaler · Collapse scavenger · Volatility gambler |
| `ConsumerTech` | Miss-correction tool · Precision-timing scaler · Copy/flex tool · Pristine-board support |
| `FinTech` | Penalty dodger · Delayed-risk converter · Category-bridging shell · Hidden-failure exploiter |
| `EnterpriseTech` | Stress profiter · Reroute/reposition piece · Forecasting/timing piece · Collateralizer |

---

## Proposed Companies

### `SocialMedia`

> A live-stream platform that treats every board event like a monetizable breaking story. Its whole business model is "what if one thing happening became the only thing anyone could look at?"

**CloutHub Live** *(rename + rework of existing `CloutHub` authored asset)*

| Field | Detail |
|---|---|
| **Run Role** | Attention concentrator |
| **Trigger** | First hit each turn |
| **Effect** | Spawns an `Audience Echo` token on an adjacent empty tile. If the ball crosses that lane again, the next hit on any `SocialMedia` company gets a bonus payout. |
| **Guardrail** | Max 2 echoes active; all expire at turn end. |
| **Behavior Fantasy** | Build a visible "main character lane" that snowballs with `SocialMedia` neighbors. |

> ⚠️ `Audience Echo` is a new board token type — requires architecture support for temporary non-company tile occupants.

---

> A short-video network built on the proven theory that anger is just engagement with better retention.

**RageLoop Studio**

| Field | Detail |
|---|---|
| **Run Role** | Outrage scaler |
| **Trigger** | Every 3rd hit this turn |
| **Effect** | Converts 1 self-HP loss into +2 RPH for all `SocialMedia` companies this turn. Reckless exposure becomes a teamwide outrage spike. |
| **Guardrail** | Max 2 procs/turn; cannot prevent collapse at 1 HP. |
| **Behavior Fantasy** | Let it get battered — the worse it looks, the better the whole industry performs. |

---

> A creator-management firm that buys dead memes, disgraced influencers, and expired brands, then relaunches them as "self-aware."

**TrendNecro Agency**

| Field | Detail |
|---|---|
| **Run Role** | Collapse scavenger |
| **Trigger** | When an adjacent company collapses |
| **Effect** | Gain 1 `Recycled Hype` stack. The next `SocialMedia` cashout this turn consumes it for a large bonus. |
| **Guardrail** | Stack cap 1; consumed on cashout. |
| **Behavior Fantasy** | Stage tasteful disasters next door, then cash out on the drama. |

---

> A crisis-response startup whose pitch is simple: every scandal is either a comeback story or a premium invoice.

**CancelShield PR**

| Field | Detail |
|---|---|
| **Run Role** | Volatility gambler |
| **Trigger** | 4+ hits received this turn |
| **Effect** | Resolves `Spin Cycle` at turn end. Base outcome is 50/50. Each adjacent `SocialMedia` company shifts the result +15% toward revenue (visible to player as a percentage on the card). Revenue burst = +50% of this turn's total revenue. Fine = −5 coins. |
| **Guardrail** | Maximum revenue-bias cap: 90% (never a guaranteed win). Fine always possible below that cap. Trigger requires 4+ hits — weak early game, strong in dense `SocialMedia` clusters. |
| **Behavior Fantasy** | Build the cluster, read the odds, and decide whether to route the ball through it again. Skill is knowing when the board makes the bet worth taking. |

---

### `ConsumerTech`

> A smart-home grocery platform obsessed with one thing: the customer must never feel responsible for anything, including their own bad aim.

**AutoPilot Pantry**

| Field | Detail |
|---|---|
| **Run Role** | Miss-correction tool |
| **Trigger** | First time ball would miss all companies this turn |
| **Effect** | Redirects the ball once to the nearest `ConsumerTech` company. Creates a "the app fixed it" rescue lane. |
| **Guardrail** | One redirect per turn per copy. |
| **Behavior Fantasy** | Make careful placement feel smart, not lucky — misses become managed. |

---

> A wellness-productivity company that sells exhaustion as premium self-mastery. It only thrives when the system is managed with creepy precision.

**SleepDebt SaaS**

| Field | Detail |
|---|---|
| **Run Role** | Precision-timing scaler |
| **Trigger** | Turn end |
| **Effect** | If it received exactly 2 hits this turn: gain +1 permanent RPH. If over-hit: lose extra HP. |
| **Guardrail** | Permanent RPH cap +6. |
| **Behavior Fantasy** | Don't spam it — calibrate. A clear "don't touch the dial" mini-game. |

---

> A concierge app whose core competency is shameless feature theft delivered with luxury branding.

**OneTap Butler**

| Field | Detail |
|---|---|
| **Run Role** | Copy/flex tool |
| **Trigger** | Round start |
| **Effect** | Copies the current cheapest adjacent company's on-hit ability at 50% strength for the first turn of that round. Re-evaluates each round — whoever is cheapest now gets cloned. |
| **Guardrail** | Cannot copy unique/legendary-only abilities. Copy expires after 1 turn; does not persist between rounds. |
| **Behavior Fantasy** | Rearrange your board each round to feed it a different victim. Shameless feature theft that updates its subscription every cycle. |

---

> A connected appliance ecosystem that somehow turned "your fridge seems worried about you" into recurring revenue.

**MoodFridge Cloud**

| Field | Detail |
|---|---|
| **Run Role** | Pristine-board support |
| **Trigger** | Continuous while at full HP |
| **Effect** | Adjacent companies ignore the first operational-cost deduction each round. |
| **Guardrail** | Breaks immediately when not at full HP; no stacking. |
| **Behavior Fantasy** | Protect one immaculate support piece at the center of a clean board. |

---

### `FinTech`

> A compliance-tech wallet built by people who hear "illegal" and immediately ask "in which jurisdiction?"

**Loophole Ledger**

| Field | Detail |
|---|---|
| **Run Role** | Penalty dodger |
| **Trigger** | First negative market/news modifier targeting this company each turn |
| **Effect** | Nullifies that modifier. If adjacent to another `FinTech`, also gain a minor payout when this triggers. |
| **Guardrail** | Only the first applicable penalty each turn. |
| **Behavior Fantasy** | Build a same-industry shell structure that quietly ignores the rules. |

---

> A lending platform designed to move pain out of the current reporting window and into someone else's future.

**DeferredAlpha Capital**

| Field | Detail |
|---|---|
| **Run Role** | Delayed-risk converter |
| **Trigger** | On hit |
| **Effect** | May defer 1 damage to round end. Each deferred damage increases cashout value by +15%. |
| **Guardrail** | Deferred damage cap 3; always resolves at round end. |
| **Behavior Fantasy** | Make a bad decision at the perfect time and get paid for the precision. |

---

> A consumer-credit startup scoring users from contacts, habits, and social spillover, then pretending correlation is a product breakthrough.

**CreditKaraoke**

| Field | Detail |
|---|---|
| **Run Role** | Category-bridging shell |
| **Trigger** | Turn start (player targets one adjacent non-`FinTech` company) |
| **Effect** | Issues a `Credit Line` to the target: that company's RPH is doubled this turn, but it takes 2 HP loss per hit instead of 1. Player extracts amplified value from a neighbor while accelerating their collapse on a known schedule. |
| **Guardrail** | One target per turn; cannot target `FinTech` companies; effect expires at turn end. |
| **Behavior Fantasy** | Juice a neighbor on the turn you plan to collapse them, then pocket the difference. `FinTech` stays `FinTech` — it just borrows other people's work. |

---

> A pseudo-exchange whose true product is delayed discovery. The business is not solvency; it is buying a few more unbelievably profitable minutes.

**AuditFog Exchange**

| Field | Detail |
|---|---|
| **Run Role** | Hidden-failure exploiter |
| **Trigger** | First collapse each round |
| **Effect** | That company's collapse is hidden until round end. The hidden company still pays out during the turn. |
| **Guardrail** | One hidden collapse per round. |
| **Behavior Fantasy** | It's dead, but not officially dead yet — squeeze one more monster turn out of it. |

> ⚠️ Requires a "hidden collapse" game-state flag. The hidden company must still register as "effectively collapsed" for downstream collapse-chain checks (TrendNecro, LastMile, etc.) to prevent double exploitation.

---

### `EnterpriseTech`

> A supply-chain SaaS stack that only feels magical when the client dashboard is glowing red. Calm operations are bad for business.

**PanicFulfillment OS**

| Field | Detail |
|---|---|
| **Run Role** | Stress profiter |
| **Trigger** | Continuous |
| **Effect** | Gains +RPH for each company currently below 50% HP on the board. |
| **Guardrail** | Live bonus cap +8 RPH. |
| **Behavior Fantasy** | Maintain a wounded machine instead of fixing it — damage is your dividend. |

---

> A logistics routing engine that keeps calling itself "resilience infrastructure" even though it only really shines when something next to it dies.

**LastMile Orchestrator**

| Field | Detail |
|---|---|
| **Run Role** | Reroute/reposition piece |
| **Trigger** | When an adjacent company collapses |
| **Effect** | Moves into the collapsed company's tile and triggers one free hit payout. The board visibly reroutes around failure. |
| **Guardrail** | Once per round; tile movement requires adjacency. |
| **Behavior Fantasy** | Turn a destroyed company into a new routing opportunity — live the logistics fantasy. |

> ⚠️ Dynamic tile reassignment is the architecturally heaviest mechanic in this document. Requires board to support mid-round company repositioning. Implement after tile movement primitives are validated.

---

> A forecasting vendor that mostly sells confidence, decks, and expensive certainty theater. Accuracy is useful but optional.

**Shortage Oracle AI**

| Field | Detail |
|---|---|
| **Run Role** | Forecasting/timing piece |
| **Trigger** | Round start |
| **Effect** | Predict one category. If that category is under-hit this round, gain a payout based on the shortfall. |
| **Guardrail** | Failed prediction applies a self-cost. |
| **Behavior Fantasy** | Plan an intentional cold zone on the board — reward players who think a round ahead. |

---

> A collateral-management platform that looks at a struggling neighbor and sees a monetization opportunity with paperwork.

**RepoReaper Systems**

| Field | Detail |
|---|---|
| **Run Role** | Collateralizer |
| **Trigger** | On cashout |
| **Effect** | Choose one adjacent company to mark as `Collateral`: it gains +RPH but takes extra HP loss on hit. |
| **Guardrail** | Mark lasts 2 turns; non-stacking. |
| **Behavior Fantasy** | Squeeze it, then route around the corpse. |

---

## Quick Reference: All Companies

| Category | Company | Run Role | Trigger | Effect Summary | Guardrail |
|---|---|---|---|---|---|
| `SocialMedia` | CloutHub Live | Attention concentrator | First hit/turn | Spawns `Audience Echo`; lane re-cross → `SocialMedia` bonus payout | Max 2 echoes, expire turn end |
| `SocialMedia` | RageLoop Studio | Outrage scaler | Every 3rd hit this turn | 1 self-HP loss → +2 RPH for all `SocialMedia` this turn | Max 2 procs/turn; no collapse prevention |
| `SocialMedia` | TrendNecro Agency | Collapse scavenger | Adjacent collapse | Gain `Recycled Hype`; next `SocialMedia` cashout consumes for large bonus | Stack cap 1; consumed on cashout |
| `SocialMedia` | CancelShield PR | Volatility gambler | 4+ hits received | Resolve `Spin Cycle`: base 50/50; each adjacent `SocialMedia` shifts +15% toward revenue (visible). Revenue burst = +50% turn revenue; fine = −5 coins | Bias cap 90%; 4-hit trigger weak early game |
| `ConsumerTech` | AutoPilot Pantry | Miss-correction | Ball would miss all | Redirect once to nearest `ConsumerTech` | One redirect/turn per copy |
| `ConsumerTech` | SleepDebt SaaS | Precision-timing scaler | Turn end | Exactly 2 hits → +1 permanent RPH; over-hit → extra HP loss | Permanent RPH cap +6 |
| `ConsumerTech` | OneTap Butler | Copy/flex | Round start | Copy current cheapest adjacent on-hit ability at 50% strength for first turn of that round; re-evaluates each round | No unique/legendary copy; expires after 1 turn |
| `ConsumerTech` | MoodFridge Cloud | Pristine-board support | Continuous (full HP) | Adjacent companies ignore first op-cost deduction/round | Breaks at any HP loss; no stack |
| `FinTech` | Loophole Ledger | Penalty dodger | First negative modifier/turn | Nullify it; adjacent `FinTech` → minor payout on trigger | First penalty only |
| `FinTech` | DeferredAlpha Capital | Delayed-risk converter | On hit | Defer 1 damage to round end; each deferred damage → +15% cashout value | Cap 3 deferred; resolves round end |
| `FinTech` | CreditKaraoke | Category-bridging shell | Turn start (player targets) | Target non-`FinTech` neighbor: doubles their RPH this turn, doubles their HP loss per hit | One target/turn; no `FinTech` targets; expires turn end |
| `FinTech` | AuditFog Exchange | Hidden-failure exploiter | First collapse/round | Collapse hidden until round end; company still pays out | One hidden collapse/round |
| `EnterpriseTech` | PanicFulfillment OS | Stress profiter | Continuous | +RPH per company below 50% HP on board | Cap +8 RPH |
| `EnterpriseTech` | LastMile Orchestrator | Reroute/reposition | Adjacent collapse | Move into collapsed tile; trigger one free hit payout | Once/round; adjacency required |
| `EnterpriseTech` | Shortage Oracle AI | Forecasting/timing | Round start | Predict category; under-hit → payout by shortfall | Failed prediction → self-cost |
| `EnterpriseTech` | RepoReaper Systems | Collateralizer | On cashout | **Choose** one adjacent company as `Collateral`: +RPH but extra HP loss on hit | 2-turn mark; non-stacking |

---

## Synergy Patterns To Lean Into

- `SocialMedia` synergy revolves around **spectacle loops**: one company creates attention, another converts overexposure into cash, a third harvests collapse aftermath.
- `ConsumerTech` synergy revolves around **control loops**: reroute misses, preserve full-HP support pieces, reward exact hit counts.
- `FinTech` synergy revolves around **concealment loops**: defer damage, nullify penalties, exploit hidden collapse windows for one oversized extraction turn.
- `EnterpriseTech` synergy revolves around **dependency loops**: one company stresses the board, another profits from low HP, another relocates when things break.

---

## Example Combo Lines

### `SocialMedia`
- **Early stabilizer:** `CloutHub Live` + any adjacent `SocialMedia` → predictable bonus lane instead of raw bounce luck.
- **Mid-game scaler:** `CloutHub Live` + `RageLoop Studio` → repeated lane traffic turns into a visible outrage engine.
- **Late-game payoff:** `RageLoop Studio` or fragile adjacent collapses beside `TrendNecro Agency`; `CancelShield PR` + next `SocialMedia` cashout convert the disaster into money.

### `ConsumerTech`
- **Early stabilizer:** `AutoPilot Pantry` prevents dead launches and makes the board forgiving in a skill-expressive way.
- **Mid-game scaler:** `AutoPilot Pantry` feeds exact hits into `SleepDebt SaaS`, rewarding controlled routing.
- **Late-game payoff:** `MoodFridge Cloud` anchors a polished cluster while `OneTap Butler` cycles through cheapest neighbors each round — the Butler's target changes as the board evolves, creating a continuously shifting "premium ecosystem" read.

### `FinTech`
- **Early stabilizer:** `Loophole Ledger` softens bad news and creates a reliable shell for riskier same-industry neighbors.
- **Mid-game scaler:** `Loophole Ledger` + `DeferredAlpha Capital` → "problems are for later" pair that rewards greed with structure.
- **Late-game payoff:** `CreditKaraoke` doubles a neighbor's output on the turn you plan to collapse them, `TrendNecro Agency` harvests the aftermath, and `AuditFog Exchange` delays the official reckoning — a full extraction loop across one orchestrated turn.

### `EnterpriseTech`
- **Early stabilizer:** `Shortage Oracle AI` rewards deliberate under-support of one category — planning puzzle from turn 1.
- **Mid-game scaler:** `RepoReaper Systems` marks a neighbor, increasing stress and setting up value for `PanicFulfillment OS`.
- **Late-game payoff:** Marked/weakened adjacent collapses; `LastMile Orchestrator` slides into the gap; board converts failure into routing value.

---

## High-Value Cross-Category Combos

| Combo | Payoff |
|---|---|
| `CloutHub Live` + `RageLoop Studio` | Lane traffic → temporary all-`SocialMedia` surge |
| `RageLoop Studio` + `TrendNecro Agency` | Self-damage + staged collapse → burn hot, monetize the wreckage |
| `CancelShield PR` + dense `SocialMedia` | Crisis clustering turns board into a scandal farm |
| `AutoPilot Pantry` + `SleepDebt SaaS` | Rerouted misses help hit precise 2-hit counts |
| `MoodFridge Cloud` + any `ConsumerTech` cluster | Polished centerpiece support with visible failure states |
| `OneTap Butler` + rotating best neighbor | Re-evaluate cheapest neighbor each round; incentivizes active board reshuffling for maximum theft value |
| `CreditKaraoke` + `TrendNecro Agency` | Double a neighbor's RPH, accelerate their collapse, harvest the wreckage — full extraction loop |
| `CreditKaraoke` + `DeferredAlpha Capital` | Juice the neighbor's RPH, defer the doubled HP damage to round end, stretch the extraction window |
| `DeferredAlpha Capital` + `AuditFog Exchange` | Hide collapse, delay pain, cash out before the books catch up |
| `Loophole Ledger` + `DeferredAlpha Capital` | "Nothing bad is real yet" — rewards players who understand timing windows |
| `PanicFulfillment OS` + `RepoReaper Systems` | Collateralized stress creates exactly the wounded ecosystem PanicFulfillment wants |
| `LastMile Orchestrator` + collapse-adjacent engines | Destroyed company becomes new routing opportunity — streamable highlight moment |

---

## Quick Synergy / Counterplay Read

| Category | Strongest When | Weakest When |
|---|---|---|
| `SocialMedia` | Volatility and collapse-adjacent boards | Low-hit control boards |
| `ConsumerTech` | Clean boards with routing control | High chip-damage environments |
| `FinTech` | Long rounds with deferred risk windows | Short, low-volatility rounds |
| `EnterpriseTech` | Board is stressed and full of routing decisions | Board remains healthy and stable |

---

## Failure Cases / Anti-Synergies

- `SocialMedia` underperforms on low-collision boards where the player cannot create repeated traffic or controlled collapses.
- `ConsumerTech` loses identity if the board cannot support exact routing; random hit spam makes this category underexpressive.
- `FinTech` can feel too clever-by-half in short rounds if there is not enough time to exploit deferred pain or hidden collapse windows.
- `EnterpriseTech` can become visually impressive but strategically flat if "stressed board" just means universal low HP with no meaningful reroute decisions.

---

## Degenerate Loop Risks

| Risk | Mitigation |
|---|---|
| `AuditFog Exchange` + collapse payoffs → "free extra life" perception | First collapse per round only; hidden companies still register as collapsed for `TrendNecro`, `LastMile`, and any other downstream collapse checks |
| `AutoPilot Pantry` redirects erase too much routing skill | Low redirect count; requires nearest valid `ConsumerTech` target only |
| `LastMile Orchestrator` + collapse engines → mandatory in any sacrificial board | Once-per-round move trigger; strict adjacency requirement |

---

## Booster Designs (Paper Only — Pre-Implementation)

Four boosters designed to stress-test rule-bending space not occupied by company cards. These are design targets, not implementation targets — build the company loop first.

---

### Exit Interview
*Ball behavior rule surface*

> The ball always exits the board. Until it doesn't. One more thing before you go.

| Field | Detail |
|---|---|
| **Rule Bent** | Ball exits the board |
| **Trigger** | Ball would exit the board |
| **Effect** | The ball makes one final hit on the company closest to the exit point. That company takes double HP loss from this hit. |
| **Guardrail** | Once per ball per turn. No trigger if only one company is on the board. Double HP loss applies to exit-hit only, not normal hits. |
| **Behavior Fantasy** | Route the board so the exit hit lands exactly where you want it — a staged collapse, a TrendNecro harvest, a DeferredAlpha deferral. |

**Key synergies:** TrendNecro Agency (exit-accelerated collapses on schedule) · DeferredAlpha Capital (defer the doubled exit damage while stacking cashout value) · RageLoop Studio (exit hit counts toward 3rd-hit proc)

**Anti-synergy:** MoodFridge Cloud (exit double damage immediately breaks full-HP condition)

**Implementation:** Hook into ball exit detection in the ball component layer. On exit event, find closest company to exit vector, apply a double-damage hit via the normal attribute modifier path, then despawn ball. Exit vector calculation must be deterministic.

---

### Bailout Clause
*Collapse timing rule surface*

> Once per round, the government shows up. The paperwork comes through remarkably fast when you're systemically important.

| Field | Detail |
|---|---|
| **Rule Bent** | Company collapse is permanent |
| **Trigger** | First company to reach 0 HP each round |
| **Effect** | That company survives at exactly 1 HP instead of collapsing. It cannot take HP loss for the remainder of the round. Collapses normally on the next hit it receives in a subsequent turn. |
| **Guardrail** | Once per round — second collapse proceeds normally. Cannot bail out a company already at 1 HP before the hit. Immunity expires at round end. |
| **Behavior Fantasy** | Over-stress your board knowing one collapse is blocked. Play greedier than the board should allow. |

**Key synergies:** PanicFulfillment OS (bailed-out company sits at 1 HP all round = maximum stress output) · AuditFog Exchange (one collapse hidden + one collapse blocked = two collapse-manipulation effects in one round) · RepoReaper Systems (Collateral-marked company survives one extra round at 1 HP)

**Anti-synergy:** TrendNecro Agency (bail-out blocks the collapse TrendNecro wants to harvest — running both creates direct conflict)

**Implementation:** Intercept collapse event in resolution phase before company removal. Check if round's bail-out slot is unused. If unused: set HP to 1, apply HP-immune flag for remainder of round, consume slot. Sits cleanly in `Turn.cs` resolution modifier pass.

---

### Consulting Agreement
*Shop/economy rule surface*

> They're technically gone. They just haven't left yet. Still in the office. Still updating their LinkedIn from your desk.

| Field | Detail |
|---|---|
| **Rule Bent** | Cashing out removes the company immediately |
| **Trigger** | On cashout |
| **Effect** | The cashed-out company remains on the board as a `Consultant` for one more turn. It contributes to all adjacency synergy checks but generates no RPH, cannot be hit by the ball, and cannot be targeted by active abilities. Disappears permanently at that turn's end. |
| **Guardrail** | One extra turn only, no extensions. Ball passes through Consultant tiles. Consultant invisible to hit-count tracking and active ability targeting. |
| **Behavior Fantasy** | Cash out at the right moment and keep the adjacency bonus one more turn while you find a replacement. No dead-turn gap after cashouts. |

**Key synergies:** CancelShield PR (cashed-out SocialMedia Consultant still counts toward bias percentage) · Loophole Ledger (cashed-out FinTech Consultant still triggers adjacent-FinTech payout) · MoodFridge Cloud (cashed-out MoodFridge Consultant still provides full-HP aura — physical adjacency still required but the company now "survives" its cashout for one aura tick)

**Anti-synergy:** Shortage Oracle AI (Consultant occupies a tile and appears in the category visually, but contributes no hits — creates confusing tracking if not clearly visualized)

**Implementation:** Add `isConsultant` flag to `BoardItemWrapper_Company`. On cashout: transition to Consultant state instead of removing. Ball physics skips Consultant tiles. Active ability targeting excludes `isConsultant == true`. Passive adjacency queries include Consultants. Remove at next resolution phase. Visual must be immediately distinct — greyed out or spectral — to prevent confusion with live companies.

> ⚠️ Shortage Oracle AI interaction requires explicit rule: Consultants are invisible to the category hit-count tracker.

---

### Vibes-Based Adjacency
*Adjacency rule surface*

> Adjacency is a lie you tell yourself so the org chart makes sense. If you're in the same industry, you're basically neighbors. Have you tried Slack?

| Field | Detail |
|---|---|
| **Rule Bent** | Adjacency is defined by board position |
| **Trigger** | Persistent (always active) |
| **Effect** | Companies of the same category count as adjacent to each other for all **category synergy checks**, regardless of physical board position. |
| **Guardrail** | Physical proximity mechanics are explicitly unaffected: MoodFridge Cloud aura, CloutHub Live echo token placement, CreditKaraoke target selection, RepoReaper mark target, LastMile tile movement — all still require physical adjacency. |
| **Behavior Fantasy** | Spread companies across the board for maximum ball coverage without sacrificing category synergy. Wide builds become viable. |

**Key synergies:** CancelShield PR (every SocialMedia on the board counts for bias — no clustering required) · Loophole Ledger (any FinTech anywhere triggers the adjacent-FinTech payout) · RageLoop Studio (all-SocialMedia RPH boost applies to a spread fleet)

**Anti-synergy:** MoodFridge Cloud (aura is explicitly physical — this booster does nothing for MoodFridge, partially wasting a slot if both are active)

**Implementation:** All category synergy adjacency checks should route through a shared query (e.g., `GetAdjacentCompaniesOfCategory()`). When this booster is active, that query returns all board companies of the matching category instead of only physically adjacent ones. One flag, one query function, zero changes to individual company mechanics. Recommend a visual indicator on the board showing virtual adjacency links when the booster is active.

---

## Booster Quick Reference

| Booster | Rule Surface | Trigger | Effect Summary | Guardrail |
|---|---|---|---|---|
| **Exit Interview** | Ball behavior | Ball exits board | Extra hit on exit-nearest company at double HP loss | Once per ball; no trigger on solo board |
| **Bailout Clause** | Collapse timing | First collapse/round | Company survives at 1 HP, immune to HP loss rest of round | Once per round; second collapse proceeds normally |
| **Consulting Agreement** | Shop/economy | On cashout | Company stays as `Consultant` 1 turn: adjacency synergy active, no RPH, unhittable | One turn only; disappears at turn end |
| **Vibes-Based Adjacency** | Adjacency rules | Persistent | Same-category companies count as synergy-adjacent regardless of position | Physical proximity mechanics unaffected |

---

## Implementation Notes

- Updated enum in `Assets/Scripts/Company/ECompanyCategory.cs`.
- Existing assets mapped to IDs `1/2` will read as `SocialMedia` and `ConsumerTech` after enum update.
- Before creating `FinTech` and `EnterpriseTech` cards, add category color/icon entries in `CompanyCardSettings.asset` to avoid missing-setting errors in `Widget_CompanyCard`.
- `CloutHub Live` is a rename+rework of the existing `CloutHub` authored asset — update the ScriptableObject in place rather than creating a new slot.
- Delete authored assets for `Cashnado`, `StackRabbit`, and `VirtosoXR`. Remove their entries from runtime config and any ScriptableObject references.

### Architecture Prerequisites (Before Implementing These Companies)

| Mechanic | Required By | Complexity |
|---|---|---|
| Temporary board tokens (`Audience Echo`) | CloutHub Live | Medium — new tile occupant type |
| Hidden collapse state | AuditFog Exchange | Medium — new resolution flag |
| Dynamic tile repositioning | LastMile Orchestrator | High — validate board movement primitives first |

### Architecture Prerequisites (Before Implementing Boosters)

| Mechanic | Required By | Complexity |
|---|---|---|
| Ball exit event hook + exit vector query | Exit Interview | Medium — ball component layer |
| Collapse event interception + HP-immune round flag | Bailout Clause | Medium — resolution phase modifier pass |
| `isConsultant` state on `BoardItemWrapper_Company` | Consulting Agreement | Medium — wrapper state + ball/ability exclusion |
| Shared category adjacency query (`GetAdjacentCompaniesOfCategory`) | Vibes-Based Adjacency | Low — one query flag; requires synergy checks already route through a shared function |

---

## Risks / Playtest Focus

- Ensure category fantasy is visible in UI text/iconography, not only in mechanics.
- Validate that each category has at least one company that changes routing, one that changes timing, and one that changes risk appetite.
- Watch for same-industry clusters becoming passive math piles; if a category's best play is still "just stack raw value," its cards need a more legible pattern hook.
- Specifically test collapse-chain exploits around `TrendNecro Agency` + `AuditFog Exchange` + `LastMile Orchestrator`.
- Test whether spectators can understand the combo story from board state alone; if not, the backstory/mechanic link is not strong enough yet.
- Verify humor readability in mock card frames: if the joke only lands in prose paragraphs and not in short card text, the concept still needs compression.
- Confirm `AuditFog Exchange` hidden-collapse flag does not propagate to `TrendNecro Agency` or `LastMile Orchestrator` — hidden companies should be dead for downstream checks, not hidden from them.
- **Exit Interview:** Validate that "closest to exit point" is deterministic and legible to the player. If they can't predict which company takes the exit hit, the routing decision disappears.
- **Bailout Clause:** Watch whether it triggers every round or rarely. If every round, collapse timing feels homogenized. If rarely, it fades into background noise. Target: meaningful roughly every 2–3 rounds.
- **Consulting Agreement:** Consultant visual must be immediately distinct from a live company. Playtest whether players notice Consultants contributing to synergy checks or whether the effect is invisible and therefore unfun.
- **Vibes-Based Adjacency:** Test whether players understand the physical vs. category adjacency distinction without reading the rulebook. If MoodFridge not benefiting feels like a bug, add an in-game tooltip explaining the two adjacency types.
