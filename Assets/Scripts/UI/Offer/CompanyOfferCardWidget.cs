using System.Globalization;
using MMFramework.MMUI;
using Pinvestor.CardSystem;
using Pinvestor.CompanySystem;
using Pinvestor.Game.Offer;
using Pinvestor.GameConfigSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityWeld.Binding;

namespace Pinvestor.UI.Offer
{
    /// <summary>
    /// Displays company attributes for a single offer card in the Company Offer Panel.
    /// Driven by CompanyConfigModel data from GameConfig.
    /// Visual properties (artwork, category colors) are looked up from CompanyCardDataScriptableObject.
    ///
    /// Selected / unselected state is reflected via a highlight border color.
    /// No animation required per spec.
    /// </summary>
    [Binding]
    public class CompanyOfferCardWidget : WidgetBase
    {
        [SerializeField] private Image _selectionBorder = null;
        [SerializeField] private Color _selectedBorderColor = Color.yellow;
        [SerializeField] private Color _deselectedBorderColor = Color.clear;

        // --- Bound properties ---

        private string _companyNameText;
        [Binding]
        public string CompanyNameText
        {
            get => _companyNameText;
            private set
            {
                _companyNameText = value;
                OnPropertyChanged(nameof(CompanyNameText));
            }
        }

        private string _industryTagText;
        [Binding]
        public string IndustryTagText
        {
            get => _industryTagText;
            private set
            {
                _industryTagText = value;
                OnPropertyChanged(nameof(IndustryTagText));
            }
        }

        private string _healthText;
        [Binding]
        public string HealthText
        {
            get => _healthText;
            private set
            {
                _healthText = value;
                OnPropertyChanged(nameof(HealthText));
            }
        }

        private string _rphText;
        [Binding]
        public string RPHText
        {
            get => _rphText;
            private set
            {
                _rphText = value;
                OnPropertyChanged(nameof(RPHText));
            }
        }

        private string _opCostText;
        [Binding]
        public string OpCostText
        {
            get => _opCostText;
            private set
            {
                _opCostText = value;
                OnPropertyChanged(nameof(OpCostText));
            }
        }

        private string _skillDescription;
        [Binding]
        public string SkillDescription
        {
            get => _skillDescription;
            private set
            {
                _skillDescription = value;
                OnPropertyChanged(nameof(SkillDescription));
            }
        }

        private Sprite _companyArtwork;
        [Binding]
        public Sprite CompanyArtwork
        {
            get => _companyArtwork;
            private set
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
            private set
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
            private set
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
            private set
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
            private set
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
            private set
            {
                _categoryIcon = value;
                OnPropertyChanged(nameof(CategoryIcon));
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            private set
            {
                _isSelected = value;
                ApplySelectionVisual();
            }
        }

        public CompanyConfigModel Model { get; private set; }

        // --- Public API ---

        /// <summary>
        /// Populates the widget with data from a CompanyConfigModel.
        /// Looks up visual data (artwork, category) from CardFactory if available.
        /// Call this before activating the widget.
        /// </summary>
        public void Populate(CompanyConfigModel model)
        {
            Model = model;

            CompanyNameText = model.CompanyId;
            HealthText = model.HasMaxHP ? $"{model.MaxHP} HP" : "-- HP";
            RPHText = model.HasRevenuePerHit
                ? model.RevenuePerHit.ToString("C0", CultureInfo.GetCultureInfo("en-US")) + " RPH"
                : "-- RPH";
            OpCostText = model.HasTurnlyCost
                ? model.TurnlyCost.ToString("C0", CultureInfo.GetCultureInfo("en-US")) + " / turn"
                : "-- / turn";

            PopulateFromCardDataSo(model.CompanyId);
        }

        /// <summary>
        /// Sets the selected visual state. Only one card in the panel should be selected.
        /// </summary>
        public void SetSelected(bool selected)
        {
            IsSelected = selected;
        }

        // --- Private helpers ---

        private void PopulateFromCardDataSo(string companyId)
        {
            CompanyCardDataScriptableObject cardDataSo = FindCardDataSo(companyId);

            if (cardDataSo == null)
            {
                Debug.LogWarning($"[CompanyOfferCardWidget] No CompanyCardDataScriptableObject found for '{companyId}'. Using fallback visuals.");
                IndustryTagText = string.Empty;
                SkillDescription = string.Empty;
                CompanyArtwork = null;
                return;
            }

            // Industry tag from ECompanyCategory enum name.
            IndustryTagText = cardDataSo.CompanyCategory.ToString();

            // Skill description from ability definitions (T015: fallback to skill ID string with warning if not found).
            if (cardDataSo.AbilityTriggerDefinitions != null && cardDataSo.AbilityTriggerDefinitions.Length > 0)
            {
                string description = cardDataSo.AbilityTriggerDefinitions[0].Ability.GetDescription();
                SkillDescription = string.IsNullOrEmpty(description)
                    ? $"[{cardDataSo.AbilityTriggerDefinitions[0].Ability.name}]"
                    : description;
            }
            else
            {
                SkillDescription = string.Empty;
            }

            CompanyArtwork = cardDataSo.CompanyArtwork;

            ApplyCategoryVisuals(cardDataSo.CompanyCategory);
        }

        private void ApplyCategoryVisuals(ECompanyCategory category)
        {
            if (CompanyFactory.Instance == null
                || CompanyFactory.Instance.CompanyCardSettings == null)
            {
                Debug.LogWarning("[CompanyOfferCardWidget] CompanyFactory or CompanyCardSettings not available.");
                return;
            }

            if (!CompanyFactory.Instance.CompanyCardSettings.TryGetSettings(category, out var settings))
            {
                Debug.LogWarning($"[CompanyOfferCardWidget] No settings for category {category}.");
                return;
            }

            MainFrameColor = settings.MainFrameColor;
            TopContainerColor = settings.TopContainerColor;
            NameContainerColor = settings.NameContainerColor;
            InfoContainerColor = settings.InfoContainerColor;
            CategoryIcon = settings.CategoryIcon;
        }

        private void ApplySelectionVisual()
        {
            if (_selectionBorder == null)
                return;

            _selectionBorder.color = _isSelected ? _selectedBorderColor : _deselectedBorderColor;
        }

        private static CompanyCardDataScriptableObject FindCardDataSo(string companyId)
        {
            if (CardFactory.Instance == null || CardFactory.Instance.CardContainer == null)
                return null;

            var allCards = CardFactory.Instance.CardContainer
                .GetCardDataOfType<CompanyCardDataScriptableObject>();

            foreach (var cardSo in allCards)
            {
                if (cardSo.CompanyId != null
                    && cardSo.CompanyId.CompanyId == companyId)
                {
                    return cardSo;
                }
            }

            return null;
        }
    }
}
