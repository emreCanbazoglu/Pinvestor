using System;
using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using MEC;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Board Item Abilities/Buff Board Item Ability",
        fileName = "Ability.BoardItem.Buff.asset")]
    public class BuffBoardItemAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject BuffGameplayEffect { get; private set; } = null;

        [field: SerializeField]
        public BoardItemFilterBaseScriptableObject[] TargetFilters { get; private set; }
            = Array.Empty<BoardItemFilterBaseScriptableObject>();
        
        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new BuffBoardItemAbilitySpec(this, owner);
        }

        protected override IEnumerable<GameplayEffectScriptableObject> GetDescriptiveGameplayEffects()
        {
            return new[] { BuffGameplayEffect };
        }
    }

    public class BuffBoardItemAbilitySpec : AbstractAbilitySpec
    {
        public BuffBoardItemAbilityScriptableObject BuffBoardItemAbility 
            => (BuffBoardItemAbilityScriptableObject)Ability;
        
        private readonly BoardItemWrapperBase _boardItemWrapper;
        
        private Dictionary<BoardItemWrapperBase,GameplayEffectContainer> _buffedBoardItems
            = new Dictionary<BoardItemWrapperBase,GameplayEffectContainer>();
        
        public BuffBoardItemAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _boardItemWrapper = owner
                .GetComponent<BoardItemWrapperBase>();
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            var allBoardItems
                = GameManager.Instance.BoardWrapper.Board.BoardItems;

            foreach (var boardItem in allBoardItems)
                OnBoardItemAdded(boardItem);
            
            GameManager.Instance.BoardWrapper.Board.OnBoardItemAdded
                += OnBoardItemAdded;
            
            GameManager.Instance.BoardWrapper.Board.OnBoardItemRemoved
                += OnBoardItemRemoved;
            
            while (true)
            {
                yield return Timing.WaitForOneFrame;
            }
        }
        
        public override void CancelAbility()
        {
            foreach (var kvp in _buffedBoardItems)
            {
                var boardItem = kvp.Key;
                
                if (boardItem == null)
                    continue;
                
                boardItem
                    .GetComponent<AbilitySystemCharacter>()
                    .RemoveGameplayEffectSpecFromSelf(
                        kvp.Value);
            }
            
            _buffedBoardItems.Clear();
            
            GameManager.Instance.BoardWrapper.Board.OnBoardItemAdded
                -= OnBoardItemAdded;
            
            GameManager.Instance.BoardWrapper.Board.OnBoardItemRemoved
                -= OnBoardItemRemoved;
            
            base.CancelAbility();
        }
        
        private void OnBoardItemAdded(BoardItemBase boardItem)
        {
            if(_buffedBoardItems.ContainsKey(
                   boardItem.Wrapper))
                return;
            
            if (IsValidTarget(
                    _boardItemWrapper.BoardItem, boardItem))
            {
                BuffBoardItem(boardItem);
            }
        }
        
        private void OnBoardItemRemoved(BoardItemBase boardItem)
        {
            if (!_buffedBoardItems.ContainsKey(
                    boardItem.Wrapper))
                return;

            var geContainer = _buffedBoardItems[boardItem.Wrapper];
            
            boardItem.Wrapper
                .GetComponent<AbilitySystemCharacter>()
                .RemoveGameplayEffectSpecFromSelf(geContainer);
            
            _buffedBoardItems.Remove(boardItem.Wrapper);
        }

        private void BuffBoardItem(
            BoardItemBase targetBoardItem)
        {
            if(!targetBoardItem.Wrapper.TryGetComponent(
                   out AbilitySystemCharacter asc))
                return;

            var buffSpec
                = Owner.MakeOutgoingSpec(
                    this,
                    BuffBoardItemAbility.BuffGameplayEffect);

            var geContainer 
                = asc.ApplyGameplayEffectSpecToSelf(buffSpec);

            _buffedBoardItems.Add(targetBoardItem.Wrapper, geContainer);
        }

        private bool IsValidTarget(BoardItemBase source, BoardItemBase target)
        {
            if(source == null || target == null)
                return false;
            
            if (source == target)
                return false;
            
            foreach (var filter in BuffBoardItemAbility.TargetFilters)
            {
                if (!filter.IsValid(source, target))
                    return false;
            }

            return true;
        }
    }
}