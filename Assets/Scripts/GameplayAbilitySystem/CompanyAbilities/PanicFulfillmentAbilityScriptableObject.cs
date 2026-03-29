using System.Collections.Generic;
using System.Linq;
using AbilitySystem;
using AbilitySystem.Authoring;
using AttributeSystem.Components;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// PanicFulfillment OS — gains +RPH per company currently below 50% HP.
    /// Refreshes every frame poll. Cap: +8 RPH live bonus.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/PanicFulfillment Ability",
        fileName = "Ability.Company.PanicFulfillment.asset")]
    public class PanicFulfillmentAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject RphBonusPerCompanyEffect { get; private set; } = null;
        [field: SerializeField] public int MaxBonusStacks { get; private set; } = 8;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new PanicFulfillmentAbilitySpec(this, owner);
        }

        protected override IEnumerable<GameplayEffectScriptableObject> GetDescriptiveGameplayEffects()
        {
            if (RphBonusPerCompanyEffect != null) yield return RphBonusPerCompanyEffect;
        }
    }

    public class PanicFulfillmentAbilitySpec : AbstractAbilitySpec
    {
        private PanicFulfillmentAbilityScriptableObject PanicFulfillmentAbility
            => (PanicFulfillmentAbilityScriptableObject)Ability;

        private int _currentStacks;
        private List<GameplayEffectContainer> _appliedEffects = new List<GameplayEffectContainer>();

        public PanicFulfillmentAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            while (true)
            {
                UpdateStacks();
                yield return MEC.Timing.WaitForSeconds(0.5f);
            }
        }

        public override void CancelAbility()
        {
            ClearAllStacks();
            base.CancelAbility();
        }

        private void UpdateStacks()
        {
            int companiesBelow50 = CountCompaniesBelowHalfHp();
            int targetStacks = Mathf.Min(companiesBelow50, PanicFulfillmentAbility.MaxBonusStacks);

            if (targetStacks == _currentStacks)
                return;

            ClearAllStacks();

            for (int i = 0; i < targetStacks; i++)
            {
                if (PanicFulfillmentAbility.RphBonusPerCompanyEffect == null)
                    break;

                var spec = Owner.MakeOutgoingSpec(this, PanicFulfillmentAbility.RphBonusPerCompanyEffect);
                var container = Owner.ApplyGameplayEffectSpecToSelf(spec);
                _appliedEffects.Add(container);
            }

            _currentStacks = targetStacks;
        }

        private void ClearAllStacks()
        {
            foreach (var container in _appliedEffects)
                Owner.RemoveGameplayEffectSpecFromSelf(container);

            _appliedEffects.Clear();
            _currentStacks = 0;
        }

        private int CountCompaniesBelowHalfHp()
        {
            if (GameManager.Instance?.BoardWrapper?.Board == null)
                return 0;

            int count = 0;
            foreach (var item in GameManager.Instance.BoardWrapper.Board.BoardItems)
            {
                if (!(item is BoardItem_Company companyItem))
                    continue;

                if (!(companyItem.Wrapper is BoardItemWrapper_Company wrapper))
                    continue;

                if (wrapper.AttributeSystemComponent == null)
                    continue;

                var attrSys = wrapper.AttributeSystemComponent;

                if (!attrSys.AttributeSet.TryGetAttributeByName("HP", out var hpAttr))
                    continue;

                if (!attrSys.AttributeSet.TryGetAttributeByName("MaxHP", out var maxHpAttr))
                    continue;

                attrSys.TryGetAttributeValue(hpAttr, out var hpVal);
                attrSys.TryGetAttributeValue(maxHpAttr, out var maxHpVal);

                if (maxHpVal.CurrentValue > 0f && hpVal.CurrentValue < maxHpVal.CurrentValue * 0.5f)
                    count++;
            }

            return count;
        }
    }
}
