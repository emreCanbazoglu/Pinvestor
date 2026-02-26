# Spec-Kit Workflow (Pinvestor)

`spec-kit` has been initialized for this repository and is the default workflow for non-trivial
engineering work.

## Setup Status

- Installed via `specify init --here --ai codex --force` on 2026-02-26
- Generated:
  - `/Users/emre/Desktop/MM-Projects/Pinvestor/.specify/`
  - `/Users/emre/Desktop/MM-Projects/Pinvestor/.codex/prompts/`
  - `/Users/emre/Desktop/MM-Projects/Pinvestor/.specify/memory/constitution.md`

## Standard Flow

1. `/speckit.constitution` (establish/update rules)
2. `/speckit.specify` (feature specification)
3. `/speckit.plan` (implementation plan)
4. `/speckit.tasks` (execution tasks)
5. Optional quality steps:
   - `/speckit.clarify`
   - `/speckit.checklist`
   - `/speckit.analyze`
6. `/speckit.implement` (implementation execution)

## Mandatory Cases

- New features
- Cross-system refactors
- Save/progression/economy changes
- Runtime-affecting package upgrades
- Multiplayer/network behavior changes

## Lightweight Exceptions

- Typo/docs-only changes
- Isolated null checks
- Small logging or text fixes

Even for lightweight changes, document scope and validation.

