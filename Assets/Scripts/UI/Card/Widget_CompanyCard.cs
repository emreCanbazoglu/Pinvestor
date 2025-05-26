using System.Globalization;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using MMFramework.MMUI;
using Pinvestor.CardSystem.Authoring;
using Pinvestor.CompanySystem;
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
        
        private string _abilityDescription;
        [Binding]
        public string AbilityDescription
        {
            get => _abilityDescription;
            set
            {
                _abilityDescription = value;
                OnPropertyChanged(nameof(AbilityDescription));
            }
        }
        
        private Sprite _companyArtwork;
        [Binding]
        public Sprite CompanyArtwork
        {
            get => _companyArtwork;
            set
            {
                _companyArtwork = value;
                OnPropertyChanged(nameof(CompanyArtwork));
            }
        }
        
        private Color _mainFrameColor;
        [Binding]
        public Color MainFrameColor
        {
            get => _mainFrameColor;
            set
            {
                _mainFrameColor = value;
                OnPropertyChanged(nameof(MainFrameColor));
            }
        }
        
        private Color _topContainerColor;
        [Binding]
        public Color TopContainerColor
        {
            get => _topContainerColor;
            set
            {
                _topContainerColor = value;
                OnPropertyChanged(nameof(TopContainerColor));
            }
        }
        
        private Color _nameContainerColor;
        [Binding]
        public Color NameContainerColor
        {
            get => _nameContainerColor;
            set
            {
                _nameContainerColor = value;
                OnPropertyChanged(nameof(NameContainerColor));
            }
        }
        
        private Color _infoContainerColor;
        [Binding]
        public Color InfoContainerColor
        {
            get => _infoContainerColor;
            set
            {
                _infoContainerColor = value;
                OnPropertyChanged(nameof(InfoContainerColor));
            }
        }
        
        private Sprite _categoryIcon;
        [Binding]
        public Sprite CategoryIcon
        {
            get => _categoryIcon;
            set
            {
                _categoryIcon = value;
                OnPropertyChanged(nameof(CategoryIcon));
            }
        }

        protected override void ActivatingCustomActions()
        {
            SetCompanyNameText();
            SetHPText();
            SetRPHText();
            SetAbilityDescription();
            SetCompanyArtwork();
            
            SetVisual();
            
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
        
        private void SetAbilityDescription()
        {
            AbilityDescription
                = _companyCardWrapper.CompanyCard.GetCompanyAbilityDescription();
        }
        
        private void SetCompanyArtwork()
        {
            if (_companyCardWrapper.CompanyCard.CastedCardDataSo
                .CompanyArtwork != null)
            {
                CompanyArtwork 
                    = _companyCardWrapper.CompanyCard.CastedCardDataSo
                        .CompanyArtwork;
            }
        }

        private void SetVisual()
        {
            var category = _companyCardWrapper
                .CompanyCard.CastedCardDataSo
                .CompanyCategory;

            if (!CompanyFactory.Instance.CompanyCardSettings
                    .TryGetSettings(
                        category,
                        out var settings))
            {
                Debug.LogError($"No settings found for company category: {category}");
                return;
            }
            
            MainFrameColor = settings.MainFrameColor;
            TopContainerColor = settings.TopContainerColor;
            NameContainerColor = settings.NameContainerColor;
            InfoContainerColor = settings.InfoContainerColor;
            
            CategoryIcon = settings.CategoryIcon;
        }
    }
}
