using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.CompanySystem;
using Pinvestor.Game;
using Pinvestor.Game.BallSystem;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// RageLoop Studio — every 3rd hit converts 1 self-HP loss into +2 RPH for all SocialMedia
    /// companies this turn (max 2 procs per turn).
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/RageLoop Ability",
        fileName = "Ability.Company.RageLoop.asset")]
    public class RageLoopAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject RphBuffEffect { get; private set; } = null;
        [field: SerializeField] public int HitsPerProc { get; private set; } = 3;
        [field: SerializeField] public int MaxProcsPerTurn { get; private set; } = 2;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new RageLoopAbilitySpec(this, owner);
        }
    }

    public class RageLoopAbilitySpec : AbstractAbilitySpec
    {
        private RageLoopAbilityScriptableObject RageLoopAbility
            => (RageLoopAbilityScriptableObject)Ability;

        private BoardItemWrapper_Company _selfWrapper;
        private BallTarget _ballTarget;
        private int _hitCount;
        private int _procCount;

        private EventBinding<TurnResolutionStartedEvent> _turnResetBinding;

        public RageLoopAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _selfWrapper = owner.GetComponent<BoardItemWrapper_Company>();
            _ballTarget = owner.GetComponentInChildren<BallTarget>();
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            _hitCount = 0;
            _procCount = 0;

            _turnResetBinding = new EventBinding<TurnResolutionStartedEvent>(OnTurnReset);
            EventBus<TurnResolutionStartedEvent>.Register(_turnResetBinding);

            if (_ballTarget != null)
                _ballTarget.OnBallCollided += OnBallCollided;

            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }

        public override void CancelAbility()
        {
            if (_ballTarget != null)
                _ballTarget.OnBallCollided -= OnBallCollided;

            EventBus<TurnResolutionStartedEvent>.Deregister(_turnResetBinding);
            base.CancelAbility();
        }

        private void OnBallCollided(Ball ball)
        {
            _hitCount++;
            if (_hitCount % RageLoopAbility.HitsPerProc != 0)
                return;
            if (_procCount >= RageLoopAbility.MaxProcsPerTurn)
                return;

            _procCount++;
            BuffAllSocialMedia();
        }

        private void OnTurnReset(TurnResolutionStartedEvent _)
        {
            _hitCount = 0;
            _procCount = 0;
        }

        private void BuffAllSocialMedia()
        {
            if (RageLoopAbility.RphBuffEffect == null)
                return;

            var allBoardItems = GameManager.Instance.BoardWrapper.Board.BoardItems;
            foreach (var item in allBoardItems)
            {
                if (!(item is BoardItem_Company companyItem))
                    continue;

                var companyId = companyItem.CompanyData?.RefCardId;
                var category = CompanyCategoryResolver.ResolveOrNone(companyId);
                if (category != ECompanyCategory.SocialMedia)
                    continue;

                if (!companyItem.Wrapper.TryGetComponent(out AbilitySystemCharacter asc))
                    continue;

                var spec = Owner.MakeOutgoingSpec(this, RageLoopAbility.RphBuffEffect);
                asc.ApplyGameplayEffectSpecToSelf(spec);
            }
        }
    }
}
