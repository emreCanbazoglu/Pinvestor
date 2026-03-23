# Plan: Company Content Refresh

**Spec**: `specs/002-company-refresh/spec.md`
**Created**: 2026-03-24

## Technical Approach

Work in three sequential passes: (1) clean up old companies and fix category settings, (2) author all 16 new companies as assets and GameConfig entries, (3) implement abilities tier by tier. The cleanup pass must happen first to avoid reference conflicts. GameConfig re-export replaces old entries. Ability implementation follows the existing pattern in `Assets/ScriptableObjects/GameplayAbilitySystem/Abilities/`.

## Architecture Decisions

### Asset Structure — Mirror Existing Company Pattern
Each new company follows the exact folder/asset structure of the existing companies. Use `Entertainment/CloutHub/` as the reference template:
```
Companies/[Category]/[CompanyName]/
  CompanyId.[CompanyName].asset
  CardData.Company.[CompanyName].asset
  Abilities/
    [AbilityName]/
      AbilityTriggerDefinition.Company.[CompanyName].[AbilityName].asset
      Ability.Company.[CompanyName].[AbilityName].asset
      GE.Company.[CompanyName].[AbilityName].[EffectName].asset
```

### GameConfig Entry Per Company
Each company gets a GameConfig entry (authored via the GameConfig editor, exported to `game-config.json`) with at minimum: companyId, health, RPH, and op-cost attribute values. The old 3 entries (Cashnado, CloutHub, Stack Rabbit) are deleted from the authoring asset before re-export.

### Ability Implementation — Tier-First Ordering
Tier 1 abilities (simple stat modifiers) are implemented first using only existing ability effect types — no new code required. Tier 2 abilities (event-driven, conditional) are implemented second — some will require new `GameplayEffect` configurations or new ability trigger conditions. Tier 3 abilities (CloutHub Live's echo node, AutoPilot Pantry's ball redirect) require new ability mechanics and are a dedicated final pass.

### Tier 3 Abilities — New Mechanics Required
- **CloutHub Live** needs a runtime "echo node" board entity that intercepts the ball's trajectory. This is a new board item type or a temporary collision trigger. Scope this separately.
- **AutoPilot Pantry** needs a "miss detection + redirect" hook on the ball physics system. Scope this separately after the ball system is better understood.

Both Tier 3 abilities should have placeholder ability assets created (with stub implementations and a clear `// TODO` marker) so the company card is functional even before the mechanic is built.

### CompanyContainer Update
`CompanyContainer.asset` is a ScriptableObject that lists all registered companies. After deleting old companies and creating new ones, update it in the Unity editor — it's a reference list, not generated code.

### No Code Changes for Category Enum
`ECompanyCategory.cs` is already correct. Do not touch it.

## Phase Breakdown

### Phase 1: Cleanup & Category Settings
- Delete 4 old company asset folders
- Remove old company references from `CompanyContainer.asset`
- Add FinTech and EnterpriseTech entries to `CompanyCardSettings.asset`
- Delete old company entries from GameConfig authoring asset

### Phase 2: Author 16 New Companies
- Create folder structure for all 16 companies
- Create `CompanyId` and `CardData` assets for each
- Register all 16 in `CompanyContainer.asset`
- Author all 16 in GameConfig authoring asset with initial attribute values
- Export `game-config.json`

### Phase 3: Tier 1 Abilities (5 companies)
Implement using existing ability effect types. No new code needed.
RageLoop, MoodFridgeCloud, Loophole Ledger, PanicFulfillment OS, CreditKaraoke.

### Phase 4: Tier 2 Abilities (9 companies)
Implement event-driven and conditional effects. May require new GE configurations.
TrendNecro, CancelShield, SleepDebt, OneTap Butler, DeferredAlpha, AuditFog, LastMile, RepoReaper, Shortage Oracle.

### Phase 5: Tier 3 Abilities (2 companies)
New mechanics. Placeholder assets first; real implementation after new mechanics are built.
CloutHub Live, AutoPilot Pantry.

### Phase 6: Validation
- Compile clean
- Play-mode smoke test: place one company from each category, verify ability triggers

## Key Risks

- **CompanyContainer reference breakage**: Deleting old companies before removing them from `CompanyContainer.asset` will leave broken references. Always remove from container first, then delete the asset folder.
- **GameConfig attribute keys**: The existing `game-config.json` uses GUID-based attribute keys (`f9426c64...`). New companies must use the same key schema. Read `GameConfigAuthoringAsset.cs` to understand how keys are generated/assigned before authoring.
- **OneTap Butler ability cloning**: Runtime ability cloning is complex. Evaluate the ability system's support for dynamic ability grant before committing to the full mechanic. A first-pass simplification (copy RPH value at 50% as a flat buff) is explicitly acceptable.
- **AuditFog Exchange hidden collapse**: Requires spec 006's collapse handler to be in place. If implementing before spec 006, create a placeholder ability that logs the intent without affecting game state.
- **Tier 3 ball redirect**: Ball physics are in `Assets/Scripts/Game/BallShooter/`. Read the ball movement system before scoping AutoPilot Pantry's redirect mechanic.
