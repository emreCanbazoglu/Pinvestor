using System.Globalization;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using MMFramework.MMUI;
using Pinvestor.CardSystem;
using Pinvestor.CardSystem.Authoring;

using Pinvestor.CompanySystem;
using Pinvestor.GameConfigSystem;
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
        [SerializeField] private CardContainerScriptableObject _cardContainer = null;

        private bool _isPopulatedFromConfig;
        
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

        /// <summary>
        /// Populates the widget directly from a CompanyConfigModel (config-driven offer flow).
        /// Call this before TryActivate(). Bypasses the card wrapper pipeline.
        /// </summary>
        public void PopulateFromConfig(CompanyConfigModel model)
        {
            _isPopulatedFromConfig = true;

            CompanyNameText = model.CompanyId;
            MaxHPText = model.HasMaxHP ? $"{model.MaxHP} HP" : "-- HP";
            RPHText = model.HasRevenuePerHit
                ? model.RevenuePerHit.ToString("C0", CultureInfo.GetCultureInfo("en-US")) + " RPH"
                : "-- RPH";
            AbilityDescription = ResolveAbilityDescription(model.CompanyId);
            CompanyArtwork = null;

            if (model.TryGetCompanyCategory(out ECompanyCategory category)
                && CompanyFactory.Instance.CompanyCardSettings
                    .TryGetSettings(category, out var settings))
            {
                MainFrameColor = settings.MainFrameColor;
                TopContainerColor = settings.TopContainerColor;
                NameContainerColor = settings.NameContainerColor;
                InfoContainerColor = settings.InfoContainerColor;
                CategoryIcon = settings.CategoryIcon;
            }
        }

        protected override void ActivatingCustomActions()
        {
            if (!_isPopulatedFromConfig)
            {
                SetCompanyNameText();
                SetHPText();
                SetRPHText();
                SetAbilityDescription();
                SetCompanyArtwork();
                SetVisual();
            }

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

        private string ResolveAbilityDescription(string companyId)
        {
            if (_cardContainer == null)
            {
                Debug.LogWarning("[Widget_CompanyCard] _cardContainer is not assigned — ability description will be empty.", this);
                return string.Empty;
            }

            var allCompanyCards = _cardContainer
                .GetCardDataOfType<CompanyCardDataScriptableObject>();

            foreach (var cardData in allCompanyCards)
            {
                if (cardData.CompanyId != null
                    && cardData.CompanyId.CompanyId == companyId
                    && cardData.AbilityTriggerDefinitions.Length > 0)
                {
                    return cardData.AbilityTriggerDefinitions[0].Ability?.GetDescription()
                           ?? string.Empty;
                }
            }

            return string.Empty;
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
