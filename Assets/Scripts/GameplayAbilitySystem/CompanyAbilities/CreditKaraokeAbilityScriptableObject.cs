using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CompanySystem;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// CreditKaraoke — gains temporary RPH when adjacent neighbors are from distinct categories.
    /// Cap: 3 unique categories counted. Effect stacks per unique category found.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/CreditKaraoke Ability",
        fileName = "Ability.Company.CreditKaraoke.asset")]
    public class CreditKaraokeAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject RphPerCategoryEffect { get; private set; } = null;
        [field: SerializeField] public int MaxCategoryCount { get; private set; } = 3;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new CreditKaraokeAbilitySpec(this, owner);
        }
    }

    public class CreditKaraokeAbilitySpec : AbstractAbilitySpec
    {
        private CreditKaraokeAbilityScriptableObject CreditKaraokeAbility
            => (CreditKaraokeAbilityScriptableObject)Ability;

        private BoardItemWrapper_Company _selfWrapper;
        private int _currentStacks;
        private List<GameplayEffectContainer> _appliedEffects = new List<GameplayEffectContainer>();

        public CreditKaraokeAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _selfWrapper = owner.GetComponent<BoardItemWrapper_Company>();
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            GameManager.Instance.BoardWrapper.Board.OnBoardItemAdded += _ => RefreshBuff();
            GameManager.Instance.BoardWrapper.Board.OnBoardItemRemoved += _ => RefreshBuff();

            RefreshBuff();

            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }

        public override void CancelAbility()
        {
            ClearStacks();
            base.CancelAbility();
        }

        private void RefreshBuff()
        {
            int uniqueCategories = CountUniqueAdjacentCategories();
            int targetStacks = Mathf.Min(uniqueCategories, CreditKaraokeAbility.MaxCategoryCount);

            if (targetStacks == _currentStacks)
                return;

            ClearStacks();

            for (int i = 0; i < targetStacks; i++)
            {
                if (CreditKaraokeAbility.RphPerCategoryEffect == null)
                    break;

                var spec = Owner.MakeOutgoingSpec(this, CreditKaraokeAbility.RphPerCategoryEffect);
                _appliedEffects.Add(Owner.ApplyGameplayEffectSpecToSelf(spec));
            }

            _currentStacks = targetStacks;
        }

        private void ClearStacks()
        {
            foreach (var container in _appliedEffects)
                Owner.RemoveGameplayEffectSpecFromSelf(container);

            _appliedEffects.Clear();
            _currentStacks = 0;
        }

        private int CountUniqueAdjacentCategories()
        {
            if (_selfWrapper?.BoardItem?.MainPiece?.Cell == null)
                return 0;

            var selfCell = _selfWrapper.BoardItem.MainPiece.Cell;
            var categories = new HashSet<ECompanyCategory>();

            foreach (var item in GameManager.Instance.BoardWrapper.Board.BoardItems)
            {
                if (!(item is BoardItem_Company companyItem))
                    continue;

                var otherCell = companyItem.MainPiece?.Cell;
                if (otherCell == null)
                    continue;

                if (!selfCell.IsLinkedCell(otherCell))
                    continue;

                var companyId = companyItem.CompanyData?.RefCardId;
                var category = CompanyCategoryResolver.ResolveOrNone(companyId);
                if (category == ECompanyCategory.None)
                    continue;

                categories.Add(category);
            }

            return categories.Count;
        }
    }
}
