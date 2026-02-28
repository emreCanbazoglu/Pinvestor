using System.Globalization;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
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

        private int _currentRoundDisplay = 0;
        private int _currentTurnDisplay = 0;
        private int _currentRoundTurnCount = 0;
        private float _currentTargetWorth = 0f;

        protected override void AwakeCustomActions()
        {
            _roundStartedBinding = new EventBinding<RoundStartedEvent>(OnRoundStarted);
            _turnStartedBinding = new EventBinding<TurnStartedEvent>(OnTurnStarted);
            _roundCompletedBinding = new EventBinding<RoundCompletedEvent>(OnRoundCompleted);

            EventBus<RoundStartedEvent>.Register(_roundStartedBinding);
            EventBus<TurnStartedEvent>.Register(_turnStartedBinding);
            EventBus<RoundCompletedEvent>.Register(_roundCompletedBinding);

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

            float ratio = _currentTargetWorth <= 0f
                ? 1f
                : Mathf.Clamp01(currentBalance / _currentTargetWorth);

            if (_targetFillImage != null)
                _targetFillImage.fillAmount = ratio;

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
