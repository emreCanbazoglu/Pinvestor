using System;
using DG.Tweening;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.BoardSystem
{
    [CreateAssetMenu(
        fileName = "BoardItemProperty.Placable.Company.asset",
        menuName = "Pinvestor/Game/Board Item/Property/Placable/Company")]
    public class BoardItemProperty_PlacableCompany : BoardItemProperty_PlacableBase
    {
        [field: SerializeField] public float PlacementDuration { get; private set; } = 0.5f;
        [field: SerializeField] public Ease PlacementEase { get; private set; } = Ease.OutBack;
        
        public override BoardItemPropertySpecBase CreateSpec(BoardItemBase owner)
        {
            return new BoardItemPropertySpec_PlacableCompany(this, owner);
        }
    }

    public class BoardItemPropertySpec_PlacableCompany : BoardItemPropertySpec_PlacableBase
    {
        public BoardItemProperty_PlacableCompany CastedSO { get; private set; }
        
        private Tween _placementTween;

        public BoardItemPropertySpec_PlacableCompany(
            BoardItemPropertySOBase propertySO,
            BoardItemBase owner) : base(propertySO, owner)
        {
            CastedSO = (BoardItemProperty_PlacableCompany)propertySO;
        }

        protected override void PlaceCore(
            Cell cell, 
            Action onPlaced = null)
        {
            _placementTween?.Kill();
            
            GameManager.Instance.BoardWrapper.CellWrappers
                .TryGetValue(
                    cell,
                    out var cellWrapper);

            if (cellWrapper == null)
            {
                Debug.LogError($"CellWrapper not found for cell {cell}");
                return;
            }
            
            Vector3 targetPosition = cellWrapper.PlacementPivot.position;
            
            _placementTween = BoardItem.Wrapper.transform
                .DOMove(targetPosition, CastedSO.PlacementDuration)
                .SetEase(CastedSO.PlacementEase)
                .OnComplete(() =>
                {
                    _placementTween = null;
                    
                    onPlaced?.Invoke();
                });
        }
    }
    
    
}