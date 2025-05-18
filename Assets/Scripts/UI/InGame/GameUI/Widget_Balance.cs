using System.Collections.Generic;
using System.Globalization;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using MEC;
using MMFramework.MMUI;
using UnityEngine;
using UnityWeld.Binding;

namespace Pinvestor.UI
{
    [Binding]
    public class Widget_Balance : WidgetBase
    {
        [SerializeField] private AttributeSystemComponent _attributeSystemComponent = null;
        [SerializeField] private AttributeScriptableObject _balanceAttribute = null;

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
        
        private CoroutineHandle _updateBalanceHandle;
        
        protected override void ActivatingCustomActions()
        {
            _updateBalanceHandle
                = UpdateBalanceRoutine()
                    .CancelWith(gameObject)
                    .RunCoroutine();
            
            base.ActivatingCustomActions();
        }

        protected override void DeactivatingCustomActions()
        {
            Timing.KillCoroutines(_updateBalanceHandle);
            
            base.DeactivatingCustomActions();
        }

        private IEnumerator<float> UpdateBalanceRoutine()
        {
            while (true)
            {
                if (_attributeSystemComponent == null)
                {
                    yield return Timing.WaitForOneFrame;
                    continue;
                }

                if (_attributeSystemComponent.TryGetAttributeValue(
                        _balanceAttribute,
                        out AttributeValue balanceAttribute))
                {
                    BalanceText 
                        = balanceAttribute.CurrentValue.ToString(
                            "C0", CultureInfo.GetCultureInfo("en-US"));
                }
                
                yield return Timing.WaitForOneFrame;
            }
        }
    }
}