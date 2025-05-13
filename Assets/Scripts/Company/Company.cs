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
        
        public BoardItemWrapper_Company GetComponent()
        {
            return _boardItemWrapper;
        }
    }
}