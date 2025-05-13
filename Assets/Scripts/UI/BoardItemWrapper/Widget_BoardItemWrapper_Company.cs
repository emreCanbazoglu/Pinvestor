using AbilitySystem;
using AttributeSystem.Components;
using MMFramework.MMUI;
using Pinvestor.RevenueGeneratorSystem.Core;
using UnityEngine;

namespace Pinvestor.UI
{
    public class Widget_BoardItemWrapper_Company : VMBase
    {
        [field: SerializeField] public Widget_FillBarAttributeValue HealthFillBarWidget { get; private set; }
        [SerializeField] private AttributeSystemComponent _attributeSystemComponent = null;

        [SerializeField] private RevenueGenerator _revenueGenerator = null;
        [SerializeField] private FloatingTextSkinScriptableObject _revenueSkin = null;
        [SerializeField] private Transform _pivotTransform = null;

        protected override void AwakeCustomActions()
        {
            HealthFillBarWidget.InitWidget(_attributeSystemComponent);

            base.AwakeCustomActions();
        }

        protected override void ActivatingCustomActions()
        {
            _revenueGenerator.OnRevenueGenerated += OnRevenueGenerated;
            
            base.ActivatingCustomActions();
        }

        protected override void DeactivatingCustomActions()
        {
            _revenueGenerator.OnRevenueGenerated -= OnRevenueGenerated;
            
            base.DeactivatingCustomActions();
        }

        private void OnRevenueGenerated(
            AbilitySystemCharacter other,
            float revenueAmount,
            float currentBalance)
        {

            Widget_FloatingText widgetFloatingText
                = FloatingTextPool.Instance.Pool.Get();

            if (widgetFloatingText == null)
                return;

            FloatingTextSkinScriptableObject skin
                = _revenueSkin;

            if (_pivotTransform == null)
                return;

            widgetFloatingText.Init(
                _pivotTransform,
                revenueAmount.ToString(),
                skin);

            widgetFloatingText.TryActivate();
        }
    }
}
