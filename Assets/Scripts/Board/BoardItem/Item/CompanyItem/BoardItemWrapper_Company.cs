using AbilitySystem;
using AttributeSystem.Components;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CompanySystem;
using Pinvestor.DamagableSystem;
using Pinvestor.Game;
using Pinvestor.Game.BallSystem;
using Pinvestor.Game.Economy;
using Pinvestor.Game.Health;
using Pinvestor.GameplayAbilitySystem.Abilities;
using Pinvestor.GameConfigSystem;
using Pinvestor.RevenueGeneratorSystem.Core;
using UnityEngine;

namespace Pinvestor.BoardSystem.Authoring
{
    public class BoardItemWrapper_Company : BoardItemWrapperBase<BoardItem_Company>
    {
        [field: SerializeField] public AbilitySystemCharacter AbilitySystemCharacter { get; private set; } = null;
        [field: SerializeField] public AttributeSystemComponent AttributeSystemComponent { get; private set; } = null;
        [SerializeField] private GenerateRevenueAbilityScriptableObject _generateRevenueAbility = null;

        [SerializeField] private Damagable _damagable = null;

        [SerializeField] private BallTarget _ballTarget = null;

        [field: SerializeField] public RevenueGenerator RevenueGenerator { get; private set; } = null;

        public Company Company { get; private set; }

        /// <summary>Per-instance health state. Initialized in WrapCore from GameConfig MaxHP.</summary>
        public CompanyHealthState HealthState { get; private set; }

        /// <summary>Per-instance valuation model. Initialized in WrapCore from GameConfig.</summary>
        public CompanyValuationModel ValuationModel { get; private set; }


        private Transform _slotTransform;

        private void OnEnable()
        {
            _ballTarget.OnBallCollided += OnBallCollided;

            _damagable.OnDied += OnDied;
        }

        private void OnDisable()
        {
            _ballTarget.OnBallCollided -= OnBallCollided;

            _damagable.OnDied -= OnDied;
        }

        protected override void WrapCore()
        {
            InitializeAttributeSystem();

            CreateCompany();

            InitializeHealthAndValuation();

            string companyId = BoardItem.CompanyData.RefCardId;
            gameObject.name = "BoardItemWrapper_" + companyId;

            BoardItem.TryGetPropertySpec(
                out BoardItemPropertySpec_PlacableCompany placableCompanySpec);

            placableCompanySpec.OnPlaced += OnCompanyPlaced;

            base.WrapCore();
        }

        protected override void DisposeCore()
        {
            BoardItem.TryGetPropertySpec(
                out BoardItemPropertySpec_PlacableCompany placableCompanySpec);

            placableCompanySpec.OnPlaced -= OnCompanyPlaced;
            AttributeSystemComponent.ClearBaseValueOverrideResolver();

            base.DisposeCore();
        }

        private void OnCompanyPlaced(Cell cell)
        {
            if (cell == null)
                return;

            var parentCellWrapper
                = GameManager.Instance.BoardWrapper
                    .CellWrappers[cell];

            transform.SetParent(parentCellWrapper.transform);
        }

        private void InitializeAttributeSystem()
        {
            string companyId = BoardItem.CompanyData.RefCardId;

            var companyConfigResolver = new CompanyAttributeConfigResolver(
                GameConfigManager.Instance);
            var baseValueResolver = new CompanyAttributeBaseValueOverrideResolver(
                companyId,
                companyConfigResolver);

            AttributeSystemComponent.SetBaseValueOverrideResolver(baseValueResolver);

            // Initialize using the AttributeSet already assigned in the inspector.
            // Values are overridden at runtime via CompanyAttributeBaseValueOverrideResolver.
            AbilitySystemCharacter.AttributeSystem.Initialize();
        }

        private void CreateCompany()
        {
            string companyId = BoardItem.CompanyData.RefCardId;

            CompanyFactory.Instance.TryCreateCompany(
                companyId,
                out Company company);

            if (company == null)
            {
                Debug.LogError($"[BoardItemWrapper_Company] CompanyFactory could not create company for ID '{companyId}'.");
                return;
            }

            Company = company;
            Company.SetBoardItemWrapper(this);

            Company.transform.SetParent(VisualContainer);
            Company.transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// Creates per-instance CompanyHealthState and CompanyValuationModel from GameConfig.
        /// Called during WrapCore so models are ready before OnEnable subscribes listeners.
        /// </summary>
        private void InitializeHealthAndValuation()
        {
            float maxHp = 1f;
            float purchaseCost = 0f;
            float cashoutRate = CompanyValuationModel.DefaultCashoutRate;

            if (GameConfigManager.Instance != null && GameConfigManager.Instance.IsInitialized)
            {
                string companyId = BoardItem.CompanyData.RefCardId;

                if (GameConfigManager.Instance.TryGetService(out CompanyConfigService companyConfigService))
                {
                    companyConfigService.TryGetCompanyMaxHP(
                        companyId,
                        out maxHp);

                    // Purchase cost: use TurnlyCost as a proxy until a dedicated cost field is added.
                    // TODO(spec-004): replace with actual purchase cost field when spec 004 merges.
                    if (companyConfigService.TryGetCompanyConfig(
                            companyId,
                            out var companyConfig))
                    {
                        if (companyConfig.TryGetTurnlyCost(out float turnlyCost))
                            purchaseCost = turnlyCost;
                    }
                }

                if (GameConfigManager.Instance.TryGetService(out BalanceConfigService balanceService))
                {
                    balanceService.TryGetValue(
                        CompanyValuationModel.CashoutRateKey,
                        out cashoutRate);
                }
            }

            HealthState = new CompanyHealthState(maxHp);
            ValuationModel = new CompanyValuationModel(purchaseCost, cashoutRate);
        }


        public void SetSlotTransform(Transform slotTransform)
        {
            _slotTransform = slotTransform;

            transform.SetParent(slotTransform);
            transform.localPosition = Vector3.zero;
        }

        public void SetSelected(bool isSelected)
        {
            Debug.Log("Company: " + (Company != null ? Company.CompanyId?.CompanyId : "null") + " SetSelected: " + isSelected);

            gameObject.SetActive(true);
        }

        public void ReleaseToSlot()
        {
            if (_slotTransform == null)
                return;

            gameObject.SetActive(false);
        }

        private void OnBallCollided(Ball ball)
        {
            if (AbilitySystemCharacter.TryActivateAbility(
                    _generateRevenueAbility,
                    out _))
            {
                Debug.Log($"Company {gameObject.name} activated ability: {_generateRevenueAbility.name}");

                //Shake the company
                Company.Shake();
            }
        }

        private void OnDied(
            AbilitySystemCharacter other,
            DamageInfo damageInfo)
        {
            // Cancel all abilities immediately so no further revenue or effects fire.
            AbilitySystemCharacter.CancelAllAbilities();

            // Mark the runtime health model as pending collapse.
            // Actual board removal and CompanyCollapsedEvent emission happen in
            // Turn.RemoveCollapsedCompanies() during the Resolution Phase, so the
            // ball's current launch-phase trajectory is not disrupted mid-flight.
            HealthState?.MarkPendingCollapse();

            Debug.Log($"[spec-006] Company {gameObject.name} HP reached 0 — flagged PendingCollapse. " +
                      $"Removal deferred to Resolution Phase.");
        }
    }
}
