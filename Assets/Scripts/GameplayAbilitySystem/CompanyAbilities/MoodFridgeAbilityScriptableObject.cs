using System;
using System.Collections.Generic;
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
    /// MoodFridge Cloud — while at full HP, adjacent companies ignore first op-cost deduction
    /// each round. Buff is removed immediately when this company drops below max HP.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/MoodFridge Ability",
        fileName = "Ability.Company.MoodFridge.asset")]
    public class MoodFridgeAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject OpCostShieldEffect { get; private set; } = null;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new MoodFridgeAbilitySpec(this, owner);
        }
    }

    public class MoodFridgeAbilitySpec : AbstractAbilitySpec
    {
        private MoodFridgeAbilityScriptableObject MoodFridgeAbility
            => (MoodFridgeAbilityScriptableObject)Ability;

        private BoardItemWrapper_Company _selfWrapper;
        private Dictionary<BoardItemWrapper_Company, GameplayEffectContainer> _buffed
            = new Dictionary<BoardItemWrapper_Company, GameplayEffectContainer>();

        private bool _wasFullHp = true;

        public MoodFridgeAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _selfWrapper = owner.GetComponent<BoardItemWrapper_Company>();
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            GameManager.Instance.BoardWrapper.Board.OnBoardItemAdded += OnBoardItemAdded;
            GameManager.Instance.BoardWrapper.Board.OnBoardItemRemoved += OnBoardItemRemoved;

            while (true)
            {
                UpdateBuffState();
                yield return MEC.Timing.WaitForSeconds(0.2f);
            }
        }

        public override void CancelAbility()
        {
            RemoveAllBuffs();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.BoardWrapper.Board.OnBoardItemAdded -= OnBoardItemAdded;
                GameManager.Instance.BoardWrapper.Board.OnBoardItemRemoved -= OnBoardItemRemoved;
            }
            base.CancelAbility();
        }

        private void UpdateBuffState()
        {
            bool isFullHp = CheckFullHp();

            if (isFullHp && !_wasFullHp)
            {
                ApplyBuffsToNeighbors();
            }
            else if (!isFullHp && _wasFullHp)
            {
                RemoveAllBuffs();
            }

            _wasFullHp = isFullHp;
        }

        private bool CheckFullHp()
        {
            if (_selfWrapper == null || _selfWrapper.AttributeSystemComponent == null)
                return false;

            var attrSys = _selfWrapper.AttributeSystemComponent;
            if (!attrSys.AttributeSet.TryGetAttributeByName("HP", out var hpAttr))
                return false;
            if (!attrSys.AttributeSet.TryGetAttributeByName("MaxHP", out var maxHpAttr))
                return false;

            attrSys.TryGetAttributeValue(hpAttr, out var hpVal);
            attrSys.TryGetAttributeValue(maxHpAttr, out var maxHpVal);

            return Mathf.Approximately(hpVal.CurrentValue, maxHpVal.CurrentValue)
                   && maxHpVal.CurrentValue > 0f;
        }

        private void ApplyBuffsToNeighbors()
        {
            if (MoodFridgeAbility.OpCostShieldEffect == null)
                return;

            foreach (var neighbor in GetAdjacentCompanyWrappers())
            {
                if (_buffed.ContainsKey(neighbor))
                    continue;

                if (!neighbor.TryGetComponent(out AbilitySystemCharacter asc))
                    continue;

                var spec = Owner.MakeOutgoingSpec(this, MoodFridgeAbility.OpCostShieldEffect);
                var container = asc.ApplyGameplayEffectSpecToSelf(spec);
                _buffed[neighbor] = container;
            }
        }

        private void RemoveAllBuffs()
        {
            foreach (var kvp in _buffed)
            {
                if (kvp.Key == null)
                    continue;
                if (!kvp.Key.TryGetComponent(out AbilitySystemCharacter asc))
                    continue;
                asc.RemoveGameplayEffectSpecFromSelf(kvp.Value);
            }
            _buffed.Clear();
        }

        private void OnBoardItemAdded(BoardItemBase boardItem)
        {
            if (!_wasFullHp)
                return;
            if (!(boardItem is BoardItem_Company companyItem))
                return;

            if (!(companyItem.Wrapper is BoardItemWrapper_Company wrapper))
                return;

            if (!IsAdjacentTo(companyItem))
                return;

            if (_buffed.ContainsKey(wrapper))
                return;

            if (!wrapper.TryGetComponent(out AbilitySystemCharacter asc))
                return;

            var spec = Owner.MakeOutgoingSpec(this, MoodFridgeAbility.OpCostShieldEffect);
            var container = asc.ApplyGameplayEffectSpecToSelf(spec);
            _buffed[wrapper] = container;
        }

        private void OnBoardItemRemoved(BoardItemBase boardItem)
        {
            if (!(boardItem.Wrapper is BoardItemWrapper_Company wrapper))
                return;

            if (!_buffed.TryGetValue(wrapper, out var container))
                return;

            if (wrapper.TryGetComponent(out AbilitySystemCharacter asc))
                asc.RemoveGameplayEffectSpecFromSelf(container);

            _buffed.Remove(wrapper);
        }

        private bool IsAdjacentTo(BoardItem_Company other)
        {
            if (_selfWrapper == null)
                return false;

            var selfItem = _selfWrapper.BoardItem;
            if (selfItem?.MainPiece?.Cell == null)
                return false;

            var otherCell = other.MainPiece?.Cell;
            if (otherCell == null)
                return false;

            return selfItem.MainPiece.Cell.IsLinkedCell(otherCell);
        }

        private IEnumerable<BoardItemWrapper_Company> GetAdjacentCompanyWrappers()
        {
            if (_selfWrapper == null)
                yield break;

            var selfItem = _selfWrapper.BoardItem;
            if (selfItem?.MainPiece?.Cell == null)
                yield break;

            foreach (var boardItem in GameManager.Instance.BoardWrapper.Board.BoardItems)
            {
                if (!(boardItem is BoardItem_Company companyItem))
                    continue;

                if (!IsAdjacentTo(companyItem))
                    continue;

                if (!(companyItem.Wrapper is BoardItemWrapper_Company wrapper))
                    continue;

                yield return wrapper;
            }
        }
    }
}
