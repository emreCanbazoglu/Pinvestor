using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// AutoPilot Pantry — if the ball would miss all companies this turn, redirect it once
    /// to the nearest ConsumerTech company (one redirect per turn per copy).
    ///
    /// TODO: requires ball miss detection hook — the ball physics system (BallShooter/Ball.cs)
    /// does not currently expose a "miss" event (ball exits bounds without hitting any company).
    /// Implement after reading BallShooter.cs and Ball.cs and adding a miss detection callback
    /// to the ball movement system.
    /// Company is fully playable without this ability firing.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/AutoPilotPantry Ability",
        fileName = "Ability.Company.AutoPilotPantry.BallRedirect.asset")]
    public class AutoPilotPantryAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new AutoPilotPantryAbilitySpec(this, owner);
        }
    }

    public class AutoPilotPantryAbilitySpec : AbstractAbilitySpec
    {
        public AutoPilotPantryAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            // TODO: requires ball miss detection hook — not yet implemented.
            // When implemented:
            //   1. Subscribe to a ball-miss event from the ball physics system.
            //   2. On miss, find the nearest ConsumerTech company on the board.
            //   3. Redirect the ball (modify trajectory) toward that company.
            //   4. Allow only one redirect per turn per AutoPilot Pantry instance.
            //   5. Read Assets/Scripts/Game/BallShooter/BallShooter.cs and Ball.cs
            //      to understand miss detection before implementing.
            Debug.Log("[AutoPilot Pantry] Ability stub active — ball miss detection hook not yet implemented.");

            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }
    }
}
