using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// RepoReaper Systems — on cashout, mark a random adjacent company as Collateral:
    /// +RPH for 2 turns but +1 HP loss per hit during that period.
    ///
    /// Cashout hook is called by the cashout system (spec-006 compatible).
    /// The CollateralEffect applies the dual buff/debuff to the target.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/RepoReaper Ability",
        fileName = "Ability.Company.RepoReaper.asset")]
    public class RepoReaperAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject CollateralRphBonusEffect { get; private set; } = null;
        [field: SerializeField] public GameplayEffectScriptableObject CollateralHpDebuffEffect { get; private set; } = null;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new RepoReaperAbilitySpec(this, owner);
        }
    }

    public class RepoReaperAbilitySpec : AbstractAbilitySpec
    {
        private RepoReaperAbilityScriptableObject RepoReaperAbility
            => (RepoReaperAbilityScriptableObject)Ability;

        private BoardItemWrapper_Company _selfWrapper;

        public RepoReaperAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _selfWrapper = owner.GetComponent<BoardItemWrapper_Company>();
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }

        /// <summary>
        /// Called by the cashout system when this company cashes out.
        /// Marks a random adjacent company as Collateral.
        /// </summary>
        public void OnCashout()
        {
            var adjacent = GetRandomAdjacentCompany();
            if (adjacent == null)
            {
                Debug.Log("[RepoReaper] No adjacent company to mark as Collateral.");
                return;
            }

            if (!adjacent.TryGetComponent(out AbilitySystemCharacter asc))
                return;

            if (RepoReaperAbility.CollateralRphBonusEffect != null)
            {
                var rphSpec = Owner.MakeOutgoingSpec(this, RepoReaperAbility.CollateralRphBonusEffect);
                asc.ApplyGameplayEffectSpecToSelf(rphSpec);
            }

            if (RepoReaperAbility.CollateralHpDebuffEffect != null)
            {
                var debuffSpec = Owner.MakeOutgoingSpec(this, RepoReaperAbility.CollateralHpDebuffEffect);
                asc.ApplyGameplayEffectSpecToSelf(debuffSpec);
            }

            Debug.Log($"[RepoReaper] Marked {adjacent.gameObject.name} as Collateral (+RPH, +HP loss per hit for 2 turns).");
        }

        private BoardItemWrapper_Company GetRandomAdjacentCompany()
        {
            if (_selfWrapper?.BoardItem?.MainPiece?.Cell == null)
                return null;

            var selfCell = _selfWrapper.BoardItem.MainPiece.Cell;
            var candidates = new List<BoardItemWrapper_Company>();

            foreach (var item in GameManager.Instance.BoardWrapper.Board.BoardItems)
            {
                if (!(item is BoardItem_Company companyItem))
                    continue;

                if (companyItem.Wrapper == _selfWrapper)
                    continue;

                var otherCell = companyItem.MainPiece?.Cell;
                if (otherCell == null)
                    continue;

                if (!selfCell.IsLinkedCell(otherCell))
                    continue;

                if (!(companyItem.Wrapper is BoardItemWrapper_Company wrapper))
                    continue;

                candidates.Add(wrapper);
            }

            if (candidates.Count == 0)
                return null;

            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
