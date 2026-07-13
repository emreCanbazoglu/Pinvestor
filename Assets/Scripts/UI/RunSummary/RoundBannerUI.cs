using DG.Tweening;
using MMFramework.MMUI;
using Pinvestor.Game;
using TMPro;
using UnityEngine;
using UnityWeld.Binding;

namespace Pinvestor.UI
{
    /// <summary>
    /// Brief "Round N — Target $X" interstitial shown at the start of every round.
    /// Listens for RoundStartedEvent; Round.ExecuteAsync waits a matching beat
    /// before running the first turn so the banner is readable.
    /// </summary>
    [Binding]
    public class RoundBannerUI : VMBase
    {
        [SerializeField] private TextMeshProUGUI _roundText = null;
        [SerializeField] private TextMeshProUGUI _targetText = null;

        [SerializeField] private float _fadeInDuration = 0.25f;
        [SerializeField] private float _holdDuration = 1.0f;
        [SerializeField] private float _fadeOutDuration = 0.25f;
        [SerializeField] private Ease _fadeEase = Ease.OutQuad;

        private EventBinding<RoundStartedEvent> _roundStartedBinding;
        private Sequence _sequence;

        protected override void AwakeCustomActions()
        {
            _roundStartedBinding = new EventBinding<RoundStartedEvent>(OnRoundStarted);
            EventBus<RoundStartedEvent>.Register(_roundStartedBinding);

            base.AwakeCustomActions();
        }

        protected override void OnDestroyCustomActions()
        {
            EventBus<RoundStartedEvent>.Deregister(_roundStartedBinding);

            _sequence?.Kill();
            _sequence = null;

            base.OnDestroyCustomActions();
        }

        private void OnRoundStarted(RoundStartedEvent e)
        {
            if (_roundText != null)
                _roundText.text = $"ROUND {e.RoundIndex + 1}";

            if (_targetText != null)
                _targetText.text = $"Target: ${e.RequiredWorth:N0}";

            TryActivate();

            PlayBannerSequence();
        }

        private void PlayBannerSequence()
        {
            _sequence?.Kill();

            // VMBase's serialized _canvasGroup, exposed via the protected property.
            if (CanvasGroup == null)
                return;

            CanvasGroup.alpha = 0f;

            _sequence = DOTween.Sequence()
                .Append(CanvasGroup.DOFade(1f, _fadeInDuration).SetEase(_fadeEase))
                .AppendInterval(_holdDuration)
                .Append(CanvasGroup.DOFade(0f, _fadeOutDuration).SetEase(_fadeEase))
                .OnComplete(() =>
                {
                    _sequence = null;
                    TryDeactivate();
                });
        }
    }
}
