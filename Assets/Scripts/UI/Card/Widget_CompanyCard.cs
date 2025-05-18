using System.Globalization;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using MMFramework.MMUI;
using Pinvestor.CardSystem.Authoring;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityWeld.Binding;

namespace Pinvestor.UI
{
    [Binding]
    public class Widget_CompanyCard : WidgetBase
    {
        [field: SerializeField] public EventTrigger ButtonEventTrigger { get; private set; } = null;
        
        [SerializeField] private CompanyCardWrapper _companyCardWrapper = null;
        
        [SerializeField] private AttributeScriptableObject _maxHPAttribute = null;
        [SerializeField] private AttributeScriptableObject _rphAttribute = null;
        
        private string _companyNameText;
        [Binding]
        public string CompanyNameText
        {
            get => _companyNameText;
            set
            {
                _companyNameText = value;
                OnPropertyChanged(nameof(CompanyNameText));
            }
        }
        
        private string _maxHPText;
        [Binding]
        public string MaxHPText
        {
            get => _maxHPText;
            set
            {
                _maxHPText = value;
                OnPropertyChanged(nameof(MaxHPText));
            }
        }
        
        private string _rphText;
        [Binding]
        public string RPHText
        {
            get => _rphText;
            set
            {
                _rphText = value;
                OnPropertyChanged(nameof(RPHText));
            }
        }

        protected override void ActivatingCustomActions()
        {
            SetCompanyNameText();
            SetHPText();
            SetRPHText();
            
            base.ActivatingCustomActions();
        }
        
        private void SetCompanyNameText()
        {
            CompanyNameText 
                = _companyCardWrapper.CompanyCard.CastedCardDataSo
                    .CompanyId.CompanyId;
        }
        
        private void SetHPText()
        {
            if (_companyCardWrapper.CompanyCard.CastedCardDataSo
                .AttributeSet.TryGetAttributeValue(
                    _maxHPAttribute,
                    out AttributeValue maxHPAttribute))
            {
                MaxHPText 
                    = maxHPAttribute.CurrentValue.ToString() + " HP";
            }
        }
        
        private void SetRPHText()
        {
            if (_companyCardWrapper.CompanyCard.CastedCardDataSo
                .AttributeSet.TryGetAttributeValue(
                    _rphAttribute,
                    out AttributeValue rphAttribute))
            {
                RPHText 
                    = rphAttribute.CurrentValue.ToString(
                    "C0", CultureInfo.GetCultureInfo("en-US")) + " RPH";
            }
        }
    }
}
