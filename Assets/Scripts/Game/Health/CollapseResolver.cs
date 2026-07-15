using System;
using System.Collections.Generic;
using System.Linq;
using AbilitySystem.Authoring;
using Pinvestor.BoardSystem;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.Game.Health
{
    /// <summary>
    /// The shared collapse handler ("spec-006 collapse handler" referenced by the
    /// company ability stubs).
    ///
    /// During the Resolution Phase, Turn asks this resolver before destroying a
    /// collapsed company. The resolver consults every active company ability spec
    /// implementing <see cref="ICollapseInterceptor"/> (e.g. AuditFog's hidden
    /// collapse); if any intercepts, the collapse is deferred — the company stays
    /// on the board, keeps generating revenue, and is destroyed when the round ends.
    ///
    /// Created once per run by GameManager; Dispose at run end.
    /// </summary>
    public sealed class CollapseResolver : IDisposable
    {
        private readonly Board _board;
        private readonly HashSet<BoardItem_Company> _deferredThisRound
            = new HashSet<BoardItem_Company>();

        private readonly EventBinding<RoundCompletedEvent> _roundCompletedBinding;

        private bool _isDisposed;

        public CollapseResolver(Board board)
        {
            _board = board;

            _roundCompletedBinding = new EventBinding<RoundCompletedEvent>(OnRoundCompleted);
            EventBus<RoundCompletedEvent>.Register(_roundCompletedBinding);
        }

        /// <summary>
        /// Returns true if the given collapsing company should be kept on the board
        /// for now (its collapse is deferred to round end).
        /// </summary>
        public bool ShouldDeferCollapse(
            BoardItem_Company collapsingItem,
            out bool newlyRecognizedCollapse)
        {
            newlyRecognizedCollapse = false;

            if (collapsingItem == null)
                return false;

            // Already deferred earlier this round — keep it alive until the round flush.
            if (_deferredThisRound.Contains(collapsingItem))
                return true;

            if (!(collapsingItem.Wrapper is BoardItemWrapper_Company collapsingWrapper))
                return false;

            foreach (ICollapseInterceptor interceptor in EnumerateActiveInterceptors())
            {
                if (!interceptor.TryInterceptCollapse(collapsingWrapper))
                    continue;

                _deferredThisRound.Add(collapsingItem);
                newlyRecognizedCollapse = true;
                RaiseCompanyCollapsedEvent(collapsingItem);
                Debug.Log(
                    $"[CollapseResolver] Collapse of '{collapsingWrapper.Company?.CompanyId?.CompanyId}' " +
                    "deferred until round end.");
                return true;
            }

            return false;
        }

        private IEnumerable<ICollapseInterceptor> EnumerateActiveInterceptors()
        {
            if (_board == null)
                yield break;

            // Snapshot: interceptors must not mutate the board while we iterate.
            var companies = _board.BoardItems.OfType<BoardItem_Company>().ToList();
            foreach (BoardItem_Company company in companies)
            {
                var wrapper = company.Wrapper as BoardItemWrapper_Company;
                if (wrapper?.AbilitySystemCharacter?.GrantedAbilities == null)
                    continue;

                foreach (AbstractAbilitySpec spec in wrapper.AbilitySystemCharacter.GrantedAbilities)
                {
                    if (spec is ICollapseInterceptor interceptor && spec.isActive)
                        yield return interceptor;
                }
            }
        }

        private void OnRoundCompleted(RoundCompletedEvent _)
        {
            FlushDeferredCollapses();
        }

        /// <summary>
        /// Destroys every collapse deferred during this round. Called on round end.
        /// </summary>
        public void FlushDeferredCollapses()
        {
            if (_deferredThisRound.Count == 0)
                return;

            var deferred = new List<BoardItem_Company>(_deferredThisRound);
            _deferredThisRound.Clear();

            foreach (BoardItem_Company companyItem in deferred)
            {
                if (TryExecuteCollapse(companyItem, emitCollapseEvent: false))
                {
                    Debug.Log("[CollapseResolver] Deferred collapse executed at round end.");
                }
            }
        }

        /// <summary>
        /// Shared collapse execution: destroys the company and optionally emits
        /// <see cref="CompanyCollapsedEvent"/>. Deferred collapses already emitted
        /// their logical event, so the round-end flush suppresses a duplicate.
        /// Returns false if the item can't be destroyed
        /// (missing spec, already destroying, or already off the board).
        /// </summary>
        public static bool TryExecuteCollapse(
            BoardItem_Company companyItem,
            bool emitCollapseEvent = true)
        {
            if (companyItem == null)
                return false;

            if (!companyItem.TryGetPropertySpec(out BoardItemPropertySpec_Destroyable destroyableSpec))
                return false;

            if (destroyableSpec.IsDestroying)
                return false;

            destroyableSpec.Destroy(null);

            if (emitCollapseEvent)
                RaiseCompanyCollapsedEvent(companyItem);

            Debug.Log("[spec-006] Company collapse executed. Investment lost.");
            return true;
        }

        private static void RaiseCompanyCollapsedEvent(
            BoardItem_Company companyItem)
        {
            if (companyItem == null)
                return;

            string companyId = companyItem.CompanyData?.RefCardId ?? string.Empty;
            var boardPosition = new Vector2Int(
                companyItem.BoardItemData.Col,
                companyItem.BoardItemData.Row);

            EventBus<CompanyCollapsedEvent>.Raise(
                new CompanyCollapsedEvent(companyId, boardPosition));

            Debug.Log(
                $"[spec-006] Company '{companyId}' recognized as collapsed " +
                $"at {boardPosition}. Investment lost.");
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _deferredThisRound.Clear();
            EventBus<RoundCompletedEvent>.Deregister(_roundCompletedBinding);
        }
    }
}
