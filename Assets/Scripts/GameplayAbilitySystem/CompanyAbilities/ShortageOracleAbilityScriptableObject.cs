using System;
using System.Collections.Generic;
using System.Linq;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CompanySystem;
using Pinvestor.Diagnostics;
using Pinvestor.Game;
using Pinvestor.Game.BallSystem;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// Shortage Oracle AI — at round start, predict one category (random);
    /// at round end, if that category was under-hit (fewer hits than average), gain a payout;
    /// if prediction fails (over-hit), apply a self-cost penalty.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/ShortageOracle Ability",
        fileName = "Ability.Company.ShortageOracle.asset")]
    public class ShortageOracleAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject PredictionPayoutEffect { get; private set; } = null;
        [field: SerializeField] public GameplayEffectScriptableObject MispredictionPenaltyEffect { get; private set; } = null;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new ShortageOracleAbilitySpec(this, owner);
        }

        protected override IEnumerable<GameplayEffectScriptableObject> GetDescriptiveGameplayEffects()
        {
            if (PredictionPayoutEffect != null) yield return PredictionPayoutEffect;
            if (MispredictionPenaltyEffect != null) yield return MispredictionPenaltyEffect;
        }
    }

    public class ShortageOracleAbilitySpec : AbstractAbilitySpec
    {
        private ShortageOracleAbilityScriptableObject ShortageOracleAbility
            => (ShortageOracleAbilityScriptableObject)Ability;

        private ECompanyCategory _predictedCategory;
        private Dictionary<ECompanyCategory, int> _categoryHits = new Dictionary<ECompanyCategory, int>();
        private bool _predictionActive;

        private Dictionary<BoardItem_Company, EventBinding<BallHitEvent>> _hitTrackers 
            = new Dictionary<BoardItem_Company, EventBinding<BallHitEvent>>();

        private EventBinding<RoundStartedEvent> _roundStartBinding;
        private EventBinding<TurnResolutionStartedEvent> _turnResBinding;

        public ShortageOracleAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            _roundStartBinding = new EventBinding<RoundStartedEvent>(OnRoundStarted);
            _turnResBinding = new EventBinding<TurnResolutionStartedEvent>(OnTurnResolution);

            EventBus<RoundStartedEvent>.Register(_roundStartBinding);
            EventBus<TurnResolutionStartedEvent>.Register(_turnResBinding);

            GameManager.Instance.BoardWrapper.Board.OnBoardItemAdded += OnBoardItemAdded;

            // Subscribe to existing board items
            foreach (var item in GameManager.Instance.BoardWrapper.Board.BoardItems)
                OnBoardItemAdded(item);

            MakePrediction();

            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }

        public override void CancelAbility()
        {
            EventBus<RoundStartedEvent>.Deregister(_roundStartBinding);
            EventBus<TurnResolutionStartedEvent>.Deregister(_turnResBinding);

            if (GameManager.Instance != null)
                GameManager.Instance.BoardWrapper.Board.OnBoardItemAdded -= OnBoardItemAdded;

            base.CancelAbility();
        }

        private void OnBoardItemAdded(BoardItemBase boardItem)
        {
            if (!(boardItem is BoardItem_Company companyItem))
                return;

            var ballTarget = companyItem.Wrapper?.GetComponentInChildren<BallTarget>();
            if (ballTarget == null)
                return;

            ballTarget.OnBallCollided += (ball) => TrackHit(companyItem);
        }

        private void TrackHit(BoardItem_Company companyItem)
        {
            var companyId = companyItem.CompanyData?.RefCardId;
            var category = CompanyCategoryResolver.ResolveOrNone(companyId);
            if (category == ECompanyCategory.None)
                return;

            if (!_categoryHits.ContainsKey(category))
                _categoryHits[category] = 0;

            _categoryHits[category]++;
        }

        private void MakePrediction()
        {
            var categories = Enum.GetValues(typeof(ECompanyCategory))
                .Cast<ECompanyCategory>()
                .Where(c => c != ECompanyCategory.None)
                .ToArray();

            _predictedCategory = categories[UnityEngine.Random.Range(0, categories.Length)];
            _predictionActive = true;
            _categoryHits.Clear();

            GameEventLog.Add("ABILITY", $"[ShortageOracle] Predicted: {_predictedCategory}", new UnityEngine.Color(0.8f, 0.8f, 0.4f));
        }

        private void OnRoundStarted(RoundStartedEvent _)
        {
            MakePrediction();
        }

        private void OnTurnResolution(TurnResolutionStartedEvent _)
        {
            if (!_predictionActive)
                return;

            // Evaluate at end of each turn — check if predicted category is under-hit vs average
            if (_categoryHits.Count == 0)
                return;

            float totalHits = 0;
            foreach (var kv in _categoryHits)
                totalHits += kv.Value;

            float average = totalHits / _categoryHits.Count;
            int predictedHits = _categoryHits.TryGetValue(_predictedCategory, out int h) ? h : 0;

            if (predictedHits < average)
            {
                // Under-hit: prediction correct — payout
                if (ShortageOracleAbility.PredictionPayoutEffect != null)
                {
                    var spec = Owner.MakeOutgoingSpec(this, ShortageOracleAbility.PredictionPayoutEffect);
                    Owner.ApplyGameplayEffectSpecToSelf(spec);
                }
                GameEventLog.Add("ABILITY", $"[ShortageOracle] Correct! {_predictedCategory} under-hit ({predictedHits} vs avg {average:F1}) — payout", new UnityEngine.Color(0.4f, 1f, 0.4f));
            }
            else if (predictedHits > average)
            {
                // Over-hit: prediction failed — penalty
                if (ShortageOracleAbility.MispredictionPenaltyEffect != null)
                {
                    var spec = Owner.MakeOutgoingSpec(this, ShortageOracleAbility.MispredictionPenaltyEffect);
                    Owner.ApplyGameplayEffectSpecToSelf(spec);
                }
                GameEventLog.Add("ABILITY", $"[ShortageOracle] Failed! {_predictedCategory} over-hit ({predictedHits} vs avg {average:F1}) — penalty", new UnityEngine.Color(1f, 0.4f, 0.4f));
            }
        }
    }
}
