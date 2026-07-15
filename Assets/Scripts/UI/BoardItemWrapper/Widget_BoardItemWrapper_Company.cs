using AbilitySystem;
using AttributeSystem.Components;
using MMFramework.MMUI;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.CompanySystem;
using Pinvestor.DamagableSystem;
using Pinvestor.GameConfigSystem;
using Pinvestor.RevenueGeneratorSystem.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinvestor.UI
{
    public class Widget_BoardItemWrapper_Company : VMBase
    {
        [field: SerializeField] public Widget_FillBarAttributeValue HealthFillBarWidget { get; private set; }
        [SerializeField] private AttributeSystemComponent _attributeSystemComponent = null;

        [SerializeField] private RevenueGenerator _revenueGenerator = null;
        [SerializeField] private FloatingTextSkinScriptableObject _revenueSkin = null;
        [SerializeField] private FloatingTextSkinScriptableObject _damageSkin = null;
        [SerializeField] private Transform _pivotTransform = null;

        [Header("MVP readability")]
        [SerializeField] private TextMeshProUGUI _companyCodeText = null;
        [SerializeField] private TextMeshProUGUI _companyNameText = null;
        [SerializeField] private TextMeshProUGUI _categoryText = null;
        [SerializeField] private TextMeshProUGUI _revenueText = null;
        [SerializeField] private TextMeshProUGUI _stateText = null;
        [SerializeField] private Image _categoryAccent = null;

        private BoardItemWrapper_Company _companyWrapper;
        private Damagable _damagable;

        public void Bind(BoardItemWrapper_Company companyWrapper)
        {
            _companyWrapper = companyWrapper;
            if (_companyWrapper?.Company == null)
                return;

            string companyId = _companyWrapper.Company.CompanyId?.CompanyId
                ?? _companyWrapper.BoardItem?.CompanyData?.RefCardId
                ?? string.Empty;
            ECompanyCategory category = CompanyCategoryResolver.ResolveOrNone(companyId);
            Color categoryColor = MvpVisualTheme.GetCategoryColor(category);

            if (_companyCodeText != null)
                _companyCodeText.text = MvpVisualTheme.GetMonogram(companyId);
            if (_companyNameText != null)
                _companyNameText.text = MvpVisualTheme.HumanizeCompanyName(companyId);
            if (_categoryText != null)
                _categoryText.text = $"{MvpVisualTheme.GetCategoryGlyph(category)}  {MvpVisualTheme.GetCategoryLabel(category)}";
            if (_categoryAccent != null)
                _categoryAccent.color = categoryColor;
            if (_stateText != null)
                _stateText.gameObject.SetActive(false);

            if (_revenueText != null)
            {
                _revenueText.text = TryGetRevenuePerHit(companyId, out float revenuePerHit)
                    ? $"${revenuePerHit:N0}/HIT"
                    : "$--/HIT";
            }

            ApplyCategoryColorToCompany(categoryColor);
        }

        protected override void AwakeCustomActions()
        {
            HealthFillBarWidget.InitWidget(_attributeSystemComponent);
            _damagable = GetComponentInParent<Damagable>();

            base.AwakeCustomActions();
        }

        protected override void ActivatingCustomActions()
        {
            _revenueGenerator.OnRevenueGenerated += OnRevenueGenerated;
            if (_damagable != null)
            {
                _damagable.OnTookDamage += OnTookDamage;
                _damagable.OnDied += OnDied;
            }
            
            base.ActivatingCustomActions();
        }

        protected override void DeactivatingCustomActions()
        {
            _revenueGenerator.OnRevenueGenerated -= OnRevenueGenerated;
            if (_damagable != null)
            {
                _damagable.OnTookDamage -= OnTookDamage;
                _damagable.OnDied -= OnDied;
            }
            
            base.DeactivatingCustomActions();
        }

        private void OnRevenueGenerated(
            AbilitySystemCharacter other,
            float revenueAmount,
            float currentBalance)
        {

            SpawnFloatingText(revenueAmount.ToString("N0"), _revenueSkin);
        }

        private void OnTookDamage(
            AbilitySystemCharacter other,
            DamageInfo damageInfo)
        {
            SpawnFloatingText(
                $"-{damageInfo.DamageAmount:N0} HP",
                _damageSkin);
            _companyWrapper?.Company?.PlayHitFeedback(damageInfo.IsCrit);

            if (_companyWrapper?.HealthState != null
                && damageInfo.CurrentHealth
                    <= _companyWrapper.HealthState.MaxHealth * 0.3f)
            {
                ShowState("AT RISK", MvpVisualTheme.Danger);
            }
        }

        private void OnDied(
            AbilitySystemCharacter other,
            DamageInfo damageInfo)
        {
            SpawnFloatingText("COLLAPSE", _damageSkin);
            ShowState("COLLAPSING", MvpVisualTheme.Danger);
            _companyWrapper?.Company?.PlayDangerFeedback();
        }

        private void SpawnFloatingText(
            string text,
            FloatingTextSkinScriptableObject skin)
        {
            if (skin == null
                || _pivotTransform == null
                || FloatingTextPool.Instance == null)
                return;

            Widget_FloatingText floatingText = FloatingTextPool.Instance.Pool.Get();
            if (floatingText == null)
                return;

            floatingText.Init(_pivotTransform, text, skin);
            floatingText.TryActivate();
        }

        private void ShowState(string state, Color color)
        {
            if (_stateText == null)
                return;

            _stateText.text = state;
            _stateText.color = color;
            _stateText.gameObject.SetActive(true);
        }

        private static bool TryGetRevenuePerHit(
            string companyId,
            out float revenuePerHit)
        {
            revenuePerHit = 0f;
            return GameConfigManager.Instance != null
                && GameConfigManager.Instance.IsInitialized
                && GameConfigManager.Instance.TryGetService(
                    out CompanyConfigService companyConfigService)
                && companyConfigService.TryGetCompanyConfig(
                    companyId,
                    out CompanyConfigModel companyConfig)
                && companyConfig.HasRevenuePerHit
                && (revenuePerHit = companyConfig.RevenuePerHit) >= 0f;
        }

        private void ApplyCategoryColorToCompany(Color categoryColor)
        {
            Renderer renderer = _companyWrapper?.Company
                ?.GetComponentInChildren<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
                return;

            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);

            if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                propertyBlock.SetColor("_BaseColor", categoryColor);
            if (renderer.sharedMaterial.HasProperty("_Color"))
                propertyBlock.SetColor("_Color", categoryColor);

            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
