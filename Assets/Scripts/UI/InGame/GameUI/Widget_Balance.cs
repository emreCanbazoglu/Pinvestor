using System.Globalization;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using DG.Tweening;
using MMFramework.MMUI;
using Pinvestor.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityWeld.Binding;

namespace Pinvestor.UI
{
    [Binding]
    public class Widget_Balance : WidgetBase
    {
        [SerializeField] private AttributeSystemComponent _attributeSystemComponent = null;
        [SerializeField] private AttributeScriptableObject _balanceAttribute = null;

        [Header("Round / Turn UI")]
        [SerializeField] private TextMeshProUGUI _roundText = null;
        [SerializeField] private TextMeshProUGUI _turnText = null;
        [SerializeField] private TextMeshProUGUI _targetText = null;
        [SerializeField] private Image _targetFillImage = null;
        [SerializeField] private TextMeshProUGUI _phaseText = null;
        [SerializeField] private TextMeshProUGUI _feedbackText = null;

        private string _balanceText;
        [Binding]
        public string BalanceText
        {
            get => _balanceText;
            set
            {
                _balanceText = value;
                OnPropertyChanged(nameof(BalanceText));
            }
        }

        private EventBinding<RoundStartedEvent> _roundStartedBinding;
        private EventBinding<TurnStartedEvent> _turnStartedBinding;
        private EventBinding<RoundCompletedEvent> _roundCompletedBinding;
        private EventBinding<TurnPhaseChangedEvent> _phaseChangedBinding;
        private EventBinding<CompanyCashedOutEvent> _cashedOutBinding;
        private EventBinding<CompanyCollapsedEvent> _collapsedBinding;
        private EventBinding<TurnResolutionCompletedEvent> _resolutionBinding;
        private EventBinding<AbilityTriggeredEvent> _abilityBinding;

        private Tween _targetTween;
        private Tween _balancePulseTween;
        private Tween _phasePulseTween;
        private Sequence _feedbackSequence;

        private int _currentRoundDisplay = 0;
        private int _currentTurnDisplay = 0;
        private int _currentRoundTurnCount = 0;
        private float _currentTargetWorth = 0f;

        protected override void AwakeCustomActions()
        {
            _roundStartedBinding = new EventBinding<RoundStartedEvent>(OnRoundStarted);
            _turnStartedBinding = new EventBinding<TurnStartedEvent>(OnTurnStarted);
            _roundCompletedBinding = new EventBinding<RoundCompletedEvent>(OnRoundCompleted);
            _phaseChangedBinding = new EventBinding<TurnPhaseChangedEvent>(OnPhaseChanged);
            _cashedOutBinding = new EventBinding<CompanyCashedOutEvent>(OnCompanyCashedOut);
            _collapsedBinding = new EventBinding<CompanyCollapsedEvent>(OnCompanyCollapsed);
            _resolutionBinding = new EventBinding<TurnResolutionCompletedEvent>(OnResolutionCompleted);
            _abilityBinding = new EventBinding<AbilityTriggeredEvent>(OnAbilityTriggered);

            EventBus<RoundStartedEvent>.Register(_roundStartedBinding);
            EventBus<TurnStartedEvent>.Register(_turnStartedBinding);
            EventBus<RoundCompletedEvent>.Register(_roundCompletedBinding);
            EventBus<TurnPhaseChangedEvent>.Register(_phaseChangedBinding);
            EventBus<CompanyCashedOutEvent>.Register(_cashedOutBinding);
            EventBus<CompanyCollapsedEvent>.Register(_collapsedBinding);
            EventBus<TurnResolutionCompletedEvent>.Register(_resolutionBinding);
            EventBus<AbilityTriggeredEvent>.Register(_abilityBinding);

            if (_attributeSystemComponent != null)
                _attributeSystemComponent.OnAttributeValueUpdated += OnAttributeValueUpdated;

            RefreshRoundTurnUI();
            RefreshBalanceAndTargetUI();

            base.AwakeCustomActions();
        }

        protected override void OnDestroyCustomActions()
        {
            EventBus<RoundStartedEvent>.Deregister(_roundStartedBinding);
            EventBus<TurnStartedEvent>.Deregister(_turnStartedBinding);
            EventBus<RoundCompletedEvent>.Deregister(_roundCompletedBinding);
            EventBus<TurnPhaseChangedEvent>.Deregister(_phaseChangedBinding);
            EventBus<CompanyCashedOutEvent>.Deregister(_cashedOutBinding);
            EventBus<CompanyCollapsedEvent>.Deregister(_collapsedBinding);
            EventBus<TurnResolutionCompletedEvent>.Deregister(_resolutionBinding);
            EventBus<AbilityTriggeredEvent>.Deregister(_abilityBinding);

            _targetTween?.Kill();
            _balancePulseTween?.Kill();
            _phasePulseTween?.Kill();
            _feedbackSequence?.Kill();

            if (_attributeSystemComponent != null)
                _attributeSystemComponent.OnAttributeValueUpdated -= OnAttributeValueUpdated;

            base.OnDestroyCustomActions();
        }

        private void OnRoundStarted(RoundStartedEvent roundStartedEvent)
        {
            _currentRoundDisplay = roundStartedEvent.RoundIndex + 1;
            _currentTurnDisplay = 0;
            _currentRoundTurnCount = Mathf.Max(0, roundStartedEvent.TurnCount);
            _currentTargetWorth = Mathf.Max(0f, roundStartedEvent.RequiredWorth);

            RefreshRoundTurnUI();
        }

        private void OnTurnStarted(TurnStartedEvent turnStartedEvent)
        {
            _currentTurnDisplay = Mathf.Max(0, turnStartedEvent.TurnIndex + 1);
            RefreshRoundTurnUI();
        }

        private void OnRoundCompleted(RoundCompletedEvent roundCompletedEvent)
        {
            _currentTargetWorth = Mathf.Max(0f, roundCompletedEvent.RequiredWorth);
            RefreshRoundTurnUI();
            RefreshBalanceAndTargetUI();
        }

        private void OnPhaseChanged(TurnPhaseChangedEvent phaseEvent)
        {
            if (_phaseText == null)
                return;

            _phaseText.text = phaseEvent.Phase.ToString().ToUpperInvariant();
            _phaseText.color = MvpVisualTheme.GetPhaseColor(phaseEvent.Phase);

            _phasePulseTween?.Kill();
            _phaseText.rectTransform.localScale = Vector3.one;
            _phasePulseTween = _phaseText.rectTransform
                .DOPunchScale(Vector3.one * 0.12f, 0.24f, 5, 0.5f);
        }

        private void OnCompanyCashedOut(CompanyCashedOutEvent e)
        {
            ShowFeedback(
                $"CASHED OUT  +{e.PayoutAmount.ToString("C0", CultureInfo.GetCultureInfo("en-US"))}",
                MvpVisualTheme.Cash);
        }

        private void OnCompanyCollapsed(CompanyCollapsedEvent e)
        {
            ShowFeedback($"{MvpVisualTheme.HumanizeCompanyName(e.CompanyId)} COLLAPSED", MvpVisualTheme.Danger);
        }

        private void OnResolutionCompleted(TurnResolutionCompletedEvent e)
        {
            string message = e.TotalTurnlyCost > 0f
                ? $"OPERATING COST  -{e.TotalTurnlyCost.ToString("C0", CultureInfo.GetCultureInfo("en-US"))}"
                : "BOOKS BALANCED";
            ShowFeedback(message, e.TotalTurnlyCost > 0f ? MvpVisualTheme.Danger : MvpVisualTheme.Revenue);
        }

        private void OnAbilityTriggered(AbilityTriggeredEvent e)
        {
            string label = MvpVisualTheme.HumanizeCompanyName(
                e.AbilityName.Replace("Ability", string.Empty).Replace("ScriptableObject", string.Empty));
            ShowFeedback(label.ToUpperInvariant(), MvpVisualTheme.Deferred);
        }

        private void ShowFeedback(string message, Color color)
        {
            if (_feedbackText == null)
                return;

            _feedbackSequence?.Kill();
            _feedbackText.text = message;
            _feedbackText.color = new Color(color.r, color.g, color.b, 1f);
            _feedbackText.rectTransform.localScale = Vector3.one * 0.86f;

            _feedbackSequence = DOTween.Sequence()
                .Append(_feedbackText.rectTransform.DOScale(1f, 0.16f).SetEase(Ease.OutBack))
                .AppendInterval(0.9f)
                .Append(_feedbackText.DOFade(0f, 0.3f));
        }

        private void OnAttributeValueUpdated(
            AttributeSystemComponent.AttributeValueChangedEvent changedEvent)
        {
            if (changedEvent.Attribute != _balanceAttribute)
                return;

            float currentBalance = changedEvent.CurrentValue.CurrentValue;
            UpdateBalanceAndTargetUI(currentBalance);
        }

        private void RefreshRoundTurnUI()
        {
            if (_roundText != null)
                _roundText.text = _currentRoundDisplay > 0
                    ? $"Round {_currentRoundDisplay}"
                    : "Round -";

            if (_turnText != null)
            {
                if (_currentRoundTurnCount <= 0)
                    _turnText.text = "Turn -";
                else
                {
                    int displayTurn = _currentTurnDisplay > 0
                        ? _currentTurnDisplay
                        : 1;

                    _turnText.text = $"Turn {displayTurn}/{_currentRoundTurnCount}";
                }
            }
        }

        private void RefreshBalanceAndTargetUI()
        {
            if (_attributeSystemComponent == null)
                return;

            if (!_attributeSystemComponent.TryGetAttributeValue(_balanceAttribute, out AttributeValue balanceAttribute))
                return;

            UpdateBalanceAndTargetUI(balanceAttribute.CurrentValue);
        }

        private void UpdateBalanceAndTargetUI(float currentBalance)
        {
            BalanceText = currentBalance.ToString("C0", CultureInfo.GetCultureInfo("en-US"));

            Transform balanceTransform = transform.Find("BalanceText");
            if (balanceTransform != null)
            {
                _balancePulseTween?.Kill();
                balanceTransform.localScale = Vector3.one;
                _balancePulseTween = balanceTransform
                    .DOPunchScale(Vector3.one * 0.08f, 0.22f, 4, 0.5f);
            }

            float ratio = _currentTargetWorth <= 0f
                ? 1f
                : Mathf.Clamp01(currentBalance / _currentTargetWorth);

            if (_targetFillImage != null)
            {
                _targetTween?.Kill();
                _targetTween = _targetFillImage
                    .DOFillAmount(ratio, 0.28f)
                    .SetEase(Ease.OutCubic);
            }

            if (_targetText != null)
            {
                string targetText = _currentTargetWorth <= 0f
                    ? "Target: -"
                    : $"Target {currentBalance.ToString("C0", CultureInfo.GetCultureInfo("en-US"))} / {_currentTargetWorth.ToString("C0", CultureInfo.GetCultureInfo("en-US"))}";

                _targetText.text = targetText;
            }
        }
    }
}
