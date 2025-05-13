using MMFramework.MMUI;
using UnityEngine;

namespace Pinvestor.UI
{
    public class Widget_HPBar : WidgetBase
    {
        [SerializeField] private float _lerpSpeed = 1.0f;
        [SerializeField] private RectTransform _fillRect = null;
        [SerializeField] private RectTransform _fillLerpRect = null;
        
        public float NormalizedValue { get; set; }

        private Vector3 _initFillRectScale;
        private Vector3 _initFillLerpRectScale;

        protected override void AwakeCustomActions()
        {
            _initFillRectScale = _fillRect.localScale;
            
            if(_fillLerpRect)
                _initFillLerpRectScale = _fillLerpRect.localScale;
            
            base.AwakeCustomActions();
        }

        private void Update()
        {
            UpdateHPBar();
        }

        private void UpdateHPBar()
        {
            Vector3 scale = _initFillRectScale;
            scale.x *= NormalizedValue;

            _fillRect.localScale = scale;

            if (_fillLerpRect)
            {
                scale = _initFillLerpRectScale;
                scale.x = _fillLerpRect.localScale.x;
                scale.x = Mathf.Lerp(
                    scale.x,
                    _initFillLerpRectScale.x * NormalizedValue,
                    Time.deltaTime * _lerpSpeed);

                _fillLerpRect.localScale = scale;
            }
        }
    }
}
