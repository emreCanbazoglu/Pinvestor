using System.Collections.Generic;
using DG.Tweening;
using MEC;
using MMFramework.MMUI;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace Pinvestor.UI
{
    public class Widget_FloatingText : WidgetBase
    {
        [SerializeField] private TMP_Text _text = null;

        public IObjectPool<Widget_FloatingText> ObjectPool { get; set; }

        private FloatingTextSkinScriptableObject _skin;
        private CoroutineHandle _lifeTimeRoutineHandle;
        private Tween _scaleTween;

        public void Init(
            Transform pivotTransform,
            string text,
            FloatingTextSkinScriptableObject skin)
        {
            transform.position = pivotTransform.position;

            _skin = skin;

            _text.text = _skin.Prefix + text + _skin.Suffix;
            _text.fontSize = _skin.TextSize;
            _text.color = _skin.TextColor;
            _text.alpha = 1.0f;
            transform.localScale = Vector3.one * 0.72f;
        }

        protected override void ActivatedCustomActions()
        {
            Timing.KillCoroutines(_lifeTimeRoutineHandle);
            _lifeTimeRoutineHandle = LifetimeRoutine()
                .CancelWith(gameObject)
                .RunCoroutine();

            _scaleTween?.Kill();
            _scaleTween = transform
                .DOScale(1f, 0.18f)
                .SetEase(Ease.OutBack);
            
            base.ActivatedCustomActions();
        }

        private IEnumerator<float> LifetimeRoutine()
        {
            if (_skin == null)
            {
                yield break;
            }
            
            float newY = transform.localPosition.y + _skin.YMovement;

            var moveTween = transform.DOLocalMoveY(
                newY, 
                _skin.Lifetime)
                .SetEase(_skin.MovementCurve);

            var fadeTween = _text.DOFade(
                    0,
                    _skin.Lifetime)
                .SetEase(_skin.FadeOutCurve);
            
            yield return Timing.WaitForSeconds(_skin.Lifetime);
            
            TryDeactivate();
            
            moveTween.Kill();
            fadeTween.Kill();
        }

        protected override void OnDestroyCustomActions()
        {
            Timing.KillCoroutines(_lifeTimeRoutineHandle);
            DOTween.Kill(transform);
            DOTween.Kill(_text);
            _scaleTween?.Kill();
            base.OnDestroyCustomActions();
        }

        protected override void DeactivatedCustomActions()
        {
            Timing.KillCoroutines(_lifeTimeRoutineHandle);
            DOTween.Kill(transform);
            DOTween.Kill(_text);
            _scaleTween?.Kill();
            ObjectPool.Release(this);
            base.DeactivatedCustomActions();
        }
    }
}
