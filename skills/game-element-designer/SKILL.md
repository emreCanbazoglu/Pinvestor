---
name: game-element-designer
description: Design and evaluate new Pinvestor gameplay elements (companies, boosters, run themes, market news, and supporting rules) with full-ecosystem reasoning and absurd-comedy flavor. Use when Codex must propose or refine game content while accounting for current card pool interactions, combo/synergy space, game balance, replayability, and the project’s core philosophy: rule-bending, behavior-first mechanics that feel fresh, creative, powerful, and darkly funny across repeated runs.
---

# Game Element Designer

## Overview

Design new Pinvestor elements as engine pieces, not isolated content. Optimize for behavior novelty, emergent combos, and replayable fun before numeric tuning.

## Non-Negotiable Design Pillars

- Behavior over stats.
- Rule-bending over flat bonuses.
- Engine-building over one-off value.
- Synergy webs over standalone power.
- Fun and expression over sterile efficiency.
- Absurd comedy tone over generic serious flavor.

Treat numbers as tuning knobs. Treat behavior as the identity.

## Mandatory Context Load (Before Designing)

Read these first:
- `docs/design/Pinvestor_Core_Gameplay_Document.md`
- `docs/design/Pinvestor_Implementation_Architecture.md`
- `Assets/Resources/GameConfig/game-config.json`
- `references/content-catalog.md`
- `references/pinvestor-ecosystem-audit.md`
- `references/humor-style-guide.md`

If data is missing (for example booster catalog not fully authored yet), state what is known, what is inferred, and what is assumed.

Before designing new elements, refresh `references/content-catalog.md` by running:
- `./scripts/refresh-content-catalog.sh`

## Element Design Workflow

1. Audit ecosystem surface area.
- Inventory active companies/cards and known systems.
- Map which rules are currently touched: economy, health/collapse, cashout, placement, ball interaction, adjacency, shop flow.
- Identify existing combo anchors and dead design space.

2. Define the behavior fantasy.
- Write a one-line fantasy: how the player feels powerful/creative because of this element.
- Specify the exact rule that bends.
- Explain why this behavior increases replayability.
- Add the comedic hook: what makes it laughably absurd while still strategically legible.

3. Specify mechanics in behavior terms.
- Define trigger, condition, effect, duration, scope, and limits.
- Show interaction with company archetypes, booster slots, run themes, and market news.
- Include failure-safe boundaries so the system bends without breaking.

4. Build the synergy and combo map.
- Use direct synergy matrix against known elements.
- Describe at least 3 combo lines: early-game stabilizer, mid-game scaler, late-game payoff.
- Include anti-synergies and counterplay.

5. Balance through constraints, not blandness.
- Add costs, timing gates, caps, decay, opportunity cost, or slot pressure.
- Preserve the fantasy while preventing dominant always-pick patterns.
- Keep clear knobs for later stat tuning.

6. Validate fun and uniqueness.
- Pass the fun rubric in `references/fun-balance-rubric.md`.
- Reject designs that are only "+X%" value without rule transformation.

## Output Contract

Return outputs in this order:
1. `Element Concept`
2. `Behavior Spec (Rule Bent)`
3. `Synergy + Combo Matrix`
4. `Balance Guardrails`
5. `Comedy Hook + Name Ideas`
6. `Fun/Replayability Justification`
7. `Implementation Notes (Pinvestor Architecture)`
8. `Open Risks and Playtest Focus`

## Quality Bar

- Do not ship purely numerical designs without behavior identity.
- Do not evaluate in isolation; always reason across existing ecosystem.
- Do not ignore emergent degenerate loops.
- Prefer "wild but bounded" over "safe but forgettable".
- Keep humor punchy and readable; jokes must support gameplay clarity, not bury it.
