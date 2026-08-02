using System;
using System.Globalization;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using DG.Tweening;
using MMFramework.MMUI;
using Pinvestor.CardSystem;
using Pinvestor.CardSystem.Authoring;

using Pinvestor.CompanySystem;
using Pinvestor.GameConfigSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityWeld.Binding;

namespace Pinvestor.UI
{
    [Binding]
    public class Widget_CompanyCard : WidgetBase
    {
        [field: SerializeField] public EventTrigger ButtonEventTrigger { get; private set; } = null;

        [Header("Hover")]
        [SerializeField] private float _hoverScale = 1.1f;
        [SerializeField] private float _hoverDuration = 0.5f;
        [SerializeField] private Ease _hoverEase = Ease.OutBounce;

        [SerializeField] private CompanyCardWrapper _companyCardWrapper = null;

        [SerializeField] private AttributeScriptableObject _maxHPAttribute = null;
        [SerializeField] private AttributeScriptableObject _rphAttribute = null;
        [SerializeField] private CardContainerScriptableObject _cardContainer = null;

        // Direct reference (not UnityWeld-bound): shows the one-time acquisition cost.
        [SerializeField] private TMPro.TextMeshProUGUI _purchaseCostText = null;
        [SerializeField] private TextMeshProUGUI _categoryText = null;

        /// <summary>Raised when the player clicks this card. The owning panel subscribes on creation.</summary>
        public Action<Widget_CompanyCard> OnClicked { get; set; }

        /// <summary>Config entry this card was populated from, so the panel does not have to track indices.</summary>
        public CompanyConfigModel Model { get; private set; }

        private bool _isPopulatedFromConfig;

        private EventTrigger.Entry _clickEntry;
        private EventTrigger.Entry _pointerEnterEntry;
        private EventTrigger.Entry _pointerExitEntry;

        private Tween _scaleTween;

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
            Model = model;

            CompanyNameText = MvpVisualTheme.HumanizeCompanyName(model.CompanyId);
            MaxHPText = model.HasMaxHP ? $"{model.MaxHP} HP" : "-- HP";
            RPHText = model.HasRevenuePerHit
                ? model.RevenuePerHit.ToString("C0", CultureInfo.GetCultureInfo("en-US")) + " / HIT"
                : "-- RPH";
            AbilityDescription = NormalizeAbilityDescription(
                ResolveAbilityDescription(model.CompanyId));
            CompanyArtwork = null;

            if (_purchaseCostText != null)
            {
                _purchaseCostText.text = model.TryGetPurchaseCost(out float purchaseCost)
                    ? "BUY " + purchaseCost.ToString("C0", CultureInfo.GetCultureInfo("en-US"))
                    : string.Empty;
            }

            if (model.TryGetCompanyCategory(out ECompanyCategory category)
                && CompanyFactory.Instance.CompanyCardSettings
                    .TryGetSettings(category, out var settings))
            {
                MainFrameColor = settings.MainFrameColor;
                TopContainerColor = settings.TopContainerColor;
                NameContainerColor = settings.NameContainerColor;
                InfoContainerColor = settings.InfoContainerColor;
                CategoryIcon = settings.CategoryIcon;
                CompanyArtwork = settings.CategoryIcon;

                if (_categoryText != null)
                {
                    _categoryText.text = MvpVisualTheme.GetCategoryLabel(category);
                    _categoryText.color = MvpVisualTheme.GetCategoryColor(category);
                }
            }
        }

        private static string NormalizeAbilityDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return "No special behavior.";

            string[] words = description.Split(
                (char[])null,
                System.StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words);
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

        protected override void ActivatedCustomActions()
        {
            RegisterPointerEntries();

            base.ActivatedCustomActions();
        }

        protected override void DeactivatedCustomActions()
        {
            UnregisterPointerEntries();

            KillScaleTween();

            base.DeactivatedCustomActions();
        }

        protected override void OnDestroyCustomActions()
        {
            UnregisterPointerEntries();

            KillScaleTween();

            OnClicked = null;

            base.OnDestroyCustomActions();
        }

        /// <summary>
        /// Adds this widget's own pointer entries to the card's EventTrigger. Only
        /// the entries created here are removed again, so any authored on the prefab
        /// are left alone.
        /// </summary>
        private void RegisterPointerEntries()
        {
            if (ButtonEventTrigger == null)
            {
                Debug.LogError(
                    "[Widget_CompanyCard] ButtonEventTrigger is not assigned. The card will not respond to input.",
                    this);
                return;
            }

            if (_clickEntry != null)
                return;

            _clickEntry = CreateEntry(EventTriggerType.PointerClick, OnPointerClick);
            _pointerEnterEntry = CreateEntry(EventTriggerType.PointerEnter, OnPointerEnter);
            _pointerExitEntry = CreateEntry(EventTriggerType.PointerExit, OnPointerExit);

            ButtonEventTrigger.triggers.Add(_clickEntry);
            ButtonEventTrigger.triggers.Add(_pointerEnterEntry);
            ButtonEventTrigger.triggers.Add(_pointerExitEntry);
        }

        private void UnregisterPointerEntries()
        {
            if (_clickEntry == null)
                return;

            if (ButtonEventTrigger != null)
            {
                ButtonEventTrigger.triggers.Remove(_clickEntry);
                ButtonEventTrigger.triggers.Remove(_pointerEnterEntry);
                ButtonEventTrigger.triggers.Remove(_pointerExitEntry);
            }

            _clickEntry = null;
            _pointerEnterEntry = null;
            _pointerExitEntry = null;
        }

        private static EventTrigger.Entry CreateEntry(
            EventTriggerType eventType,
            UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(callback);

            return entry;
        }

        private void OnPointerClick(BaseEventData eventData)
        {
            OnClicked?.Invoke(this);
        }

        private void OnPointerEnter(BaseEventData eventData)
        {
            PlayScale(_hoverScale, _hoverDuration, _hoverEase);
        }

        private void OnPointerExit(BaseEventData eventData)
        {
            PlayScale(1f, _hoverDuration, _hoverEase);
        }

        /// <summary>Scales the card in from nothing. Duration/ease come from the owning panel.</summary>
        public void PlayShow(float duration, Ease ease)
        {
            PlayScale(1f, duration, ease);
        }

        /// <summary>Scales the card away. Duration/ease come from the owning panel.</summary>
        public void PlayHide(float duration, Ease ease)
        {
            PlayScale(0f, duration, ease);
        }

        private void PlayScale(float target, float duration, Ease ease)
        {
            KillScaleTween();

            _scaleTween = transform
                .DOScale(target, duration)
                .SetEase(ease)
                .OnKill(() => _scaleTween = null);
        }

        private void KillScaleTween()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
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
