# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Game Element Designer Skill

The `skills/game-element-designer/` directory is a design workflow for creating new Pinvestor gameplay elements (companies, boosters, run themes, market news). This is **not implementation code** — it is a specification and reasoning system for content authoring.

Skill file registered at `~/.claude/skills/game-element-designer.md`.

## Context Load Order (Before Designing)

Always load in this order:

1. `docs/design/Pinvestor_Core_Gameplay_Document.md` — game rules, turn structure, economy
2. `docs/design/Pinvestor_Implementation_Architecture.md` — system boundaries and entry flows
3. `Assets/Resources/GameConfig/game-config.json` — runtime config snapshot (authoritative)
4. `docs/design/Pinvestor_Game_Elements_Design.md` — all designed companies, boosters, synergy maps, and implementation tiers
5. `skills/game-element-designer/references/content-catalog.md` — current authored elements (runtime)
6. `skills/game-element-designer/references/pinvestor-ecosystem-audit.md` — combo anchors and dead design space
7. `skills/game-element-designer/references/humor-style-guide.md` — tone and naming conventions

**Refresh the content catalog before every design session** (it is auto-generated and goes stale):

```sh
./skills/game-element-designer/scripts/refresh-content-catalog.sh
```

## Design Workflow

1. **Ecosystem audit** — inventory companies/boosters, map which rules are touched, identify open combo space and dead zones
2. **Behavior fantasy** — one-line player power fantasy, the exact rule that bends, the comedic hook
3. **Mechanic spec** — trigger / condition / effect / duration / scope / limits; interactions across turn phases
4. **Synergy + combo map** — 3 combo lines (early stabilizer, mid scaler, late payoff) + 2 anti-synergies + 1 degenerate-loop risk with mitigation
5. **Balance constraints** — costs, timing gates, caps, decay, or slot pressure; preserve fantasy without dominant always-pick patterns
6. **Fun validation** — score against `references/fun-balance-rubric.md`; reject pure stat inflation

## Required Output (in order)

1. Element Concept
2. Behavior Spec (Rule Bent)
3. Synergy + Combo Matrix
4. Balance Guardrails
5. Comedy Hook + Name Ideas
6. Fun/Replayability Justification
7. Implementation Notes (Pinvestor Architecture)
8. Open Risks and Playtest Focus

## Design Pillars

- Behavior over stats; rule-bending over flat bonuses
- Engine-building and synergy webs over standalone one-off value
- Absurd dark comedy — humor must support gameplay clarity, not bury it
- "Wild but bounded" over "safe but forgettable"
- Never ship a purely numerical design without behavior identity
- Always reason across the existing ecosystem; never evaluate in isolation

## Reference Files

| File | Purpose |
|------|---------|
| `docs/design/Pinvestor_Game_Elements_Design.md` | Authoritative design doc — all companies, boosters, synergy maps, implementation tiers |
| `references/content-catalog.md` | Auto-generated snapshot of authored elements in the runtime (refresh before each session) |
| `references/fun-balance-rubric.md` | 6-axis scoring rubric (1–5 each); red flags that trigger reject/redesign |
| `references/humor-style-guide.md` | Tone, naming patterns, flavor text constraints |
| `references/pinvestor-ecosystem-audit.md` | Checklist for combo/synergy mapping and system interaction surfaces |
| `SKILL.md` | Full skill definition and quality bar |

## Known Inconsistencies to Watch

- Content catalog has a generation timestamp — if stale, re-run the refresh script before designing
- Legacy companies (`Cashnado`, `StackRabbit`, `VirtosoXR`) are retired — ignore any catalog references to them
