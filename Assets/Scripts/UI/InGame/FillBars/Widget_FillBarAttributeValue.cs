using System.Collections;
using System.Collections.Generic;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using MEC;
using ResetableSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityWeld.Binding;

namespace Pinvestor.UI
{
    [Binding]
    public class Widget_FillBarAttributeValue : FillBarWidget,
        IResetable
    {
        [SerializeField] private HorizontalLayoutGroup _horizontalLayoutGroup = null;

        [SerializeField] private GameObject _linePrefab = null;

        [SerializeField] private float _amountPerSegment = 1.0f;

        private List<GameObject> _segmentLines = new List<GameObject>();
        public override float FillBarSize => 1.0f;

        [SerializeField] private AttributeScriptableObject _maxAtt = null;
        [SerializeField] private AttributeScriptableObject _baseAtt = null;

        private string _amountText;

        [Binding]
        public string AmountText
        {
            get => _amountText;
            private set
            {
                _amountText = value;
                OnPropertyChanged(nameof(AmountText));
            }
        }

        private AttributeSystemComponent _attributeSystemComponent;

        private AttributeValue _maxAttValueState;
        private AttributeValue _baseAttValueState;

        private IEnumerator _updateWidgetRoutine;

        public void InitWidget(
            AttributeSystemComponent attributeSystemComponent)
        {
            _attributeSystemComponent = attributeSystemComponent;

            Initialize();
        }

        public void Initialize()
        {
            UpdateAttributeValueStates();

            UpdateSegments();

            float normalizedValue = 1.0f;

            if (_maxAttValueState.CurrentValue != 0)
                normalizedValue = _baseAttValueState.CurrentValue / _maxAttValueState.CurrentValue;

            SetCurNormalizedValue(normalizedValue);
            TargetNormalizedValue = normalizedValue;

            UpdateAmountText();
        }

        protected override void ActivatedCustomActions()
        {
            if (_updateWidgetRoutine != null)
                StopCoroutine(_updateWidgetRoutine);
            
            _updateWidgetRoutine = UpdateWidget();
            StartCoroutine(_updateWidgetRoutine);

            base.ActivatedCustomActions();
        }

        protected override void DeactivatingCustomActions()
        {
            if (_updateWidgetRoutine != null)
                StopCoroutine(_updateWidgetRoutine);

            base.DeactivatingCustomActions();
        }

        private IEnumerator UpdateWidget()
        {
            while (true)
            {
                if (_attributeSystemComponent == null)
                    yield break;

                UpdateAttributeValueStates();

                UpdateSegments();
                UpdateFillAmount();
                UpdateAmountText();

                yield return null;
            }
        }

        private void UpdateAttributeValueStates()
        {
            _attributeSystemComponent.TryGetAttributeValue(
                _baseAtt, out _baseAttValueState);

            _attributeSystemComponent.TryGetAttributeValue(
                _maxAtt, out _maxAttValueState);

        }

        private void UpdateSegments()
        {
            if (_linePrefab == null)
                return;

            int segmentCount = (int)(_maxAttValueState.CurrentValue / _amountPerSegment);
            int lineCount = segmentCount - 0;

            if (segmentCount < 1)
                return;

            while (_segmentLines.Count < lineCount)
            {
                GameObject segmentLine
                    = Object.Instantiate(_linePrefab, _horizontalLayoutGroup.transform);

                segmentLine.SetActive(true);

                _segmentLines.Add(segmentLine);
            }

            while (_segmentLines.Count > lineCount)
            {
                GameObject segmentLine = _segmentLines[^1];

                _segmentLines.Remove(segmentLine);

                Destroy(segmentLine);
            }
        }

        private void UpdateFillAmount()
        {
            if (_maxAttValueState.CurrentValue == 0)
            {
                TargetNormalizedValue = 0;
                return;
            }

            float normalizedValue
                = _baseAttValueState.CurrentValue / _maxAttValueState.CurrentValue;

            normalizedValue = Mathf.Clamp(normalizedValue, 0, 1);

            TargetNormalizedValue = normalizedValue;
        }

        private void UpdateAmountText()
        {
            AmountText = _baseAttValueState.CurrentValue.ToString();
        }

        public void ResetResetable()
        {
            Initialize();
        }
    }
}
