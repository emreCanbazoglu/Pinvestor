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

        [field: Tooltip("Ease of the slide across the board plane. Overshoot is safe here.")]
        [field: SerializeField] public Ease PlacementEase { get; private set; } = Ease.OutBack;

        [field: Tooltip("Ease of the drop onto the board surface. Must not overshoot or the item clips through the board.")]
        [field: SerializeField] public Ease DropEase { get; private set; } = Ease.OutQuad;

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

            var boardWrapper = GameManager.Instance.BoardWrapper;

            boardWrapper.CellWrappers
                .TryGetValue(
                    cell,
                    out var cellWrapper);

            if (cellWrapper == null)
            {
                Debug.LogError($"CellWrapper not found for cell {cell}");
                return;
            }

            Vector3 targetPosition = cellWrapper.PlacementPivot.position;

            Transform wrapperTransform = BoardItem.Wrapper.transform;

            // The item is dragged above the board, so the placement move has to
            // travel down onto the surface. Splitting it lets the slide across the
            // board keep its overshoot ease while the drop stays monotonic —
            // a single eased DOMove would overshoot downwards and punch the item
            // through the board surface.
            Vector3 up = boardWrapper.SurfaceNormal;
            Vector3 startPosition = wrapperTransform.position;

            float startHeight = Vector3.Dot(startPosition - targetPosition, up);
            Vector3 startPlanarPosition = startPosition - up * startHeight;

            float planarProgress = 0f;
            float height = startHeight;

            _placementTween = DOTween.Sequence()
                .Join(DOTween.To(
                        () => planarProgress,
                        value =>
                        {
                            planarProgress = value;
                            applyPosition();
                        },
                        1f,
                        CastedSO.PlacementDuration)
                    .SetEase(CastedSO.PlacementEase))
                .Join(DOTween.To(
                        () => height,
                        value =>
                        {
                            height = value;
                            applyPosition();
                        },
                        0f,
                        CastedSO.PlacementDuration)
                    .SetEase(CastedSO.DropEase))
                .OnComplete(() =>
                {
                    _placementTween = null;

                    wrapperTransform.position = targetPosition;

                    onPlaced?.Invoke();
                });

            void applyPosition()
            {
                wrapperTransform.position
                    = Vector3.LerpUnclamped(
                          startPlanarPosition,
                          targetPosition,
                          planarProgress)
                      + up * height;
            }
        }
    }
    
    
}