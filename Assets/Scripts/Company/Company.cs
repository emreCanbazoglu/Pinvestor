using DG.Tweening;
using Pinvestor.BoardSystem.Authoring;
using UnityEngine;

namespace Pinvestor.CompanySystem
{
    public class Company : MonoBehaviour,
        IComponentProvider<BoardItemWrapper_Company>
    {
        [field: SerializeField] public CompanyIdScriptableObject CompanyId { get; private set; } = null;

        [SerializeField] private float _shakeDuration = 0.5f;
        [SerializeField] private float _shakeStrength = 0.2f;
        [SerializeField] private int _shakeVibrato = 10;
        [SerializeField] private float _shakeRandomness = 90f;
        [SerializeField] private Ease _shakeEase = Ease.OutBack;
        
        private BoardItemWrapper_Company _boardItemWrapper = null;
        
        private Tween _shakeTween = null;
        private Tween _scaleTween = null;
        
        public void SetBoardItemWrapper(
            BoardItemWrapper_Company boardItemWrapper)
        {
            _boardItemWrapper = boardItemWrapper;
        }

        public void Shake()
        {
            _shakeTween?.Kill();
            
            transform.localPosition = Vector3.zero;
            
            _shakeTween 
                = transform
                    .DOShakePosition(
                        _shakeDuration,
                        _shakeStrength,
                        _shakeVibrato,
                        _shakeRandomness)
                .SetEase(_shakeEase);
        }

        public void SetSelectedVisual(bool selected)
        {
            _scaleTween?.Kill();
            transform.localScale = Vector3.one;
            _scaleTween = transform
                .DOScale(selected ? 1.08f : 1f, 0.16f)
                .SetEase(Ease.OutBack);
        }

        public void PlayPlacementFeedback()
        {
            _scaleTween?.Kill();
            transform.localScale = Vector3.one;
            _scaleTween = transform
                .DOPunchScale(Vector3.one * 0.16f, 0.28f, 6, 0.55f);
        }

        public void PlayHitFeedback(bool critical = false)
        {
            _scaleTween?.Kill();
            transform.localScale = Vector3.one;
            float strength = critical ? 0.22f : 0.12f;
            _scaleTween = transform
                .DOPunchScale(Vector3.one * strength, 0.2f, 5, 0.45f);
        }

        public void PlayDangerFeedback()
        {
            _scaleTween?.Kill();
            transform.localScale = Vector3.one;
            _scaleTween = transform
                .DOShakeScale(0.34f, 0.16f, 10, 70f)
                .SetEase(Ease.OutQuad);
        }

        private void OnDestroy()
        {
            _shakeTween?.Kill();
            _scaleTween?.Kill();
        }
        
        public BoardItemWrapper_Company GetComponent()
        {
            return _boardItemWrapper;
        }
    }
}
