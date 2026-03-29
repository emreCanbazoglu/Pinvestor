using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using AttributeSystem.Components;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Diagnostics;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// OneTap Butler — on the turn it is purchased/placed, copy the cheapest adjacent company's
    /// RPH value at 50% as a flat buff for 2 turns.
    ///
    /// First pass: copies RPH value at 50% as a flat buff (simplified from full ability cloning).
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/OneTapButler Ability",
        fileName = "Ability.Company.OneTapButler.asset")]
    public class OneTapButlerAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject CopiedRphBuffEffect { get; private set; } = null;
        [field: SerializeField] public float CopyRatio { get; private set; } = 0.5f;
        [field: SerializeField] public int BuffDurationTurns { get; private set; } = 2;

        protected override IEnumerable<GameplayEffectScriptableObject> GetDescriptiveGameplayEffects()
        {
            if (CopiedRphBuffEffect != null) yield return CopiedRphBuffEffect;
        }

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new OneTapButlerAbilitySpec(this, owner);
        }
    }

    public class OneTapButlerAbilitySpec : AbstractAbilitySpec
    {
        private OneTapButlerAbilityScriptableObject OneTapButlerAbility
            => (OneTapButlerAbilityScriptableObject)Ability;

        private BoardItemWrapper_Company _selfWrapper;
        private bool _copyApplied;
        private int _remainingTurns;
        private GameplayEffectContainer _buffContainer;

        private EventBinding<TurnResolutionStartedEvent> _turnBinding;

        public OneTapButlerAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _selfWrapper = owner.GetComponent<BoardItemWrapper_Company>();
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            // Apply the copy on placement (first activation)
            ApplyCopiedBuff();
            _remainingTurns = OneTapButlerAbility.BuffDurationTurns;

            _turnBinding = new EventBinding<TurnResolutionStartedEvent>(OnTurnResolution);
            EventBus<TurnResolutionStartedEvent>.Register(_turnBinding);

            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }

        public override void CancelAbility()
        {
            EventBus<TurnResolutionStartedEvent>.Deregister(_turnBinding);
            RemoveBuff();
            base.CancelAbility();
        }

        private void ApplyCopiedBuff()
        {
            if (_copyApplied || OneTapButlerAbility.CopiedRphBuffEffect == null)
                return;

            // Find the cheapest adjacent company by op-cost
            float lowestCost = float.MaxValue;
            float copiedRph = 0f;
            bool foundAdjacent = false;

            foreach (var item in GameManager.Instance.BoardWrapper.Board.BoardItems)
            {
                if (!(item is BoardItem_Company companyItem))
                    continue;

                if (companyItem.Wrapper == _selfWrapper)
                    continue;

                if (!IsAdjacentTo(companyItem))
                    continue;

                if (!(companyItem.Wrapper is BoardItemWrapper_Company adjacentWrapper))
                    continue;

                if (adjacentWrapper.AttributeSystemComponent == null)
                    continue;

                var attrSys = adjacentWrapper.AttributeSystemComponent;

                if (!attrSys.AttributeSet.TryGetAttributeByName("TurnlyCost", out var costAttr))
                    continue;

                if (!attrSys.AttributeSet.TryGetAttributeByName("RPH", out var rphAttr))
                    continue;

                attrSys.TryGetAttributeValue(costAttr, out var costVal);
                attrSys.TryGetAttributeValue(rphAttr, out var rphVal);

                if (costVal.CurrentValue < lowestCost)
                {
                    lowestCost = costVal.CurrentValue;
                    copiedRph = rphVal.CurrentValue * OneTapButlerAbility.CopyRatio;
                    foundAdjacent = true;
                }
            }

            if (!foundAdjacent)
            {
                GameEventLog.Add("ABILITY", "[OneTapButler] No adjacent company found to copy from", new UnityEngine.Color(0.7f, 0.7f, 0.7f));
                return;
            }

            // Apply the buff using the pre-authored effect (magnitude set by Multiplier in GE asset)
            var spec = Owner.MakeOutgoingSpec(this, OneTapButlerAbility.CopiedRphBuffEffect);
            _buffContainer = Owner.ApplyGameplayEffectSpecToSelf(spec);
            _copyApplied = true;

            GameEventLog.Add("ABILITY", $"[OneTapButler] Copied {copiedRph:F1} RPH from cheapest adjacent (×{OneTapButlerAbility.CopyRatio:F0%})", new UnityEngine.Color(0.4f, 1f, 0.7f));
        }

        private void OnTurnResolution(TurnResolutionStartedEvent _)
        {
            if (!_copyApplied)
                return;

            _remainingTurns--;
            if (_remainingTurns <= 0)
            {
                RemoveBuff();
            }
        }

        private void RemoveBuff()
        {
            if (!_copyApplied)
                return;

            Owner.RemoveGameplayEffectSpecFromSelf(_buffContainer);
            _copyApplied = false;
        }

        private bool IsAdjacentTo(BoardItem_Company other)
        {
            if (_selfWrapper?.BoardItem?.MainPiece?.Cell == null)
                return false;

            var otherCell = other.MainPiece?.Cell;
            if (otherCell == null)
                return false;

            return _selfWrapper.BoardItem.MainPiece.Cell.IsLinkedCell(otherCell);
        }
    }
}
