using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// CloutHub Live — first hit each turn spawns "Audience Echo" on an adjacent empty tile;
    /// ball passing through the echo node grants +1 payout on that hit chain (max 2 echo nodes,
    /// expire at turn end).
    ///
    /// TODO: requires echo node board entity mechanic — a temporary trigger node type on the
    /// board that intercepts ball trajectory and grants a payout bonus. This entity does not
    /// exist in the current board system. Implement after the echo node board entity is designed.
    /// Company is fully playable without this ability firing.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/CloutHubLive Ability",
        fileName = "Ability.Company.CloutHubLive.AudienceEcho.asset")]
    public class CloutHubLiveAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new CloutHubLiveAbilitySpec(this, owner);
        }
    }

    public class CloutHubLiveAbilitySpec : AbstractAbilitySpec
    {
        public CloutHubLiveAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            // TODO: requires echo node board entity — not yet implemented.
            // When implemented:
            //   1. Subscribe to BallTarget.OnBallCollided on this company.
            //   2. On first hit each turn, find adjacent empty cells.
            //   3. Spawn an echo node BoardItem on a random adjacent empty tile.
            //   4. Echo node intercepts ball and grants +1 payout to the hit chain.
            //   5. Cap at 2 echo nodes per turn; expire all echo nodes at TurnResolutionStarted.
            Debug.Log("[CloutHub Live] Ability stub active — echo node mechanic not yet implemented.");

            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }
    }
}
