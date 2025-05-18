using Pinvestor.BoardSystem.Base;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem
{
    [CreateAssetMenu(
        fileName = "AbilityTrigger.BoardItemPlacement.asset",
        menuName = "Pinvestor/Ability System/Ability Triggers/Board Item Placement")]
    public class BoardItemPlacementAbilityTrigger : AbilityTriggerScriptableObjectBase
    {
        public enum EPlacement
        {
            Self = 0,
            Any = 1,
        }
        
        [field: SerializeField] public EPlacement Placement { get; private set; }        
        
        public override AbilityTriggerSpecBase CreateSpec(
            AbilityController abilityController)
        {
            return new BoardItemPlacementAbilityTriggerSpec(
                abilityController, this);
        }
    }
    
    public class BoardItemPlacementAbilityTriggerSpec : AbilityTriggerSpecBase
    {
        private BoardItemPlacementAbilityTrigger BoardItemPlacementAbilityTrigger
            => (BoardItemPlacementAbilityTrigger) ScriptableObject;
        
        private readonly BoardItemBase _selfBoardItem;
        
        public BoardItemPlacementAbilityTriggerSpec(
            AbilityController abilityController,
            AbilityTriggerScriptableObjectBase scriptableObject) 
            : base(abilityController, scriptableObject)
        {
            _selfBoardItem
                = Controller.GetComponent<BoardItemWrapperBase>()
                    .BoardItem;
        }
        
        public override void Activate()
        {
            GameManager.Instance.BoardWrapper.Board.OnBoardItemAdded
                += OnBoardItemAdded;
        }

        public override void Deactivate()
        {
            if(GameManager.Instance 
               || GameManager.Instance.BoardWrapper == null
               || GameManager.Instance.BoardWrapper.Board == null)
                return;
            
            GameManager.Instance.BoardWrapper.Board.OnBoardItemAdded
                -= OnBoardItemAdded;
        }

        private void OnBoardItemAdded(
            BoardItemBase boardItem)
        {
            if (boardItem == null)
                return;
            
            if (BoardItemPlacementAbilityTrigger.Placement == 
                BoardItemPlacementAbilityTrigger.EPlacement.Self)
            {
                if (boardItem == _selfBoardItem)
                    OnTrigger?.Invoke();
            }
            else if (BoardItemPlacementAbilityTrigger.Placement == 
                     BoardItemPlacementAbilityTrigger.EPlacement.Any)
            {
                OnTrigger?.Invoke();
            }
        }
    }
}