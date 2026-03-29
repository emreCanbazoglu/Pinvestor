using System;
using System.Collections.Generic;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.Diagnostics
{
    /// <summary>
    /// Static runtime event log. Subscribes to game EventBus events automatically when
    /// play mode starts. Read by BoardStateEditorWindow for its event log panel.
    /// Also exposes a public Add() for any game system to write custom entries.
    /// </summary>
    public static class GameEventLog
    {
        public struct LogEntry
        {
            public float Time;
            public string Category;
            public string Message;
            public Color Color;
        }

        private static List<LogEntry> _entries = new List<LogEntry>();
        public static IReadOnlyList<LogEntry> Entries => _entries;
        public static event Action OnChanged;
        public const int MaxEntries = 500;

        // EventBus bindings — held as fields so GC does not collect them
        private static EventBinding<RoundStartedEvent>             _roundStarted;
        private static EventBinding<TurnStartedEvent>              _turnStarted;
        private static EventBinding<TurnResolutionStartedEvent>    _turnResolution;
        private static EventBinding<TurnResolutionCompletedEvent>  _turnResolutionCompleted;
        private static EventBinding<TurnCompletedEvent>            _turnCompleted;
        private static EventBinding<CompanyPlacedEvent>            _companyPlaced;
        private static EventBinding<CompanyCollapsedEvent>         _companyCollapsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            _entries = new List<LogEntry>();

            _roundStarted = new EventBinding<RoundStartedEvent>(e =>
                Add("ROUND", $"Round {e.RoundIndex + 1} started — need ${e.RequiredWorth:F0}", new Color(0.5f, 0.8f, 1f)));
            EventBus<RoundStartedEvent>.Register(_roundStarted);

            _turnStarted = new EventBinding<TurnStartedEvent>(e =>
                Add("TURN", $"Turn {e.TurnIndex + 1}  (Round {e.RoundIndex + 1})", new Color(0.8f, 0.95f, 0.6f)));
            EventBus<TurnStartedEvent>.Register(_turnStarted);

            _turnResolution = new EventBinding<TurnResolutionStartedEvent>(e =>
                Add("RESOLVE", $"Resolution started  R{e.RoundIndex + 1} T{e.TurnIndex + 1}", new Color(1f, 0.8f, 0.3f)));
            EventBus<TurnResolutionStartedEvent>.Register(_turnResolution);

            _turnResolutionCompleted = new EventBinding<TurnResolutionCompletedEvent>(e =>
                Add("RESOLVE", $"Resolution done — cost ${e.TotalTurnlyCost:F0}, collapsed: {e.CollapsedCompanyCount}", new Color(1f, 0.65f, 0.2f)));
            EventBus<TurnResolutionCompletedEvent>.Register(_turnResolutionCompleted);

            _turnCompleted = new EventBinding<TurnCompletedEvent>(e =>
                Add("TURN", $"Turn {e.TurnIndex + 1} completed", new Color(0.55f, 0.55f, 0.55f)));
            EventBus<TurnCompletedEvent>.Register(_turnCompleted);

            _companyPlaced = new EventBinding<CompanyPlacedEvent>(OnCompanyPlaced);
            EventBus<CompanyPlacedEvent>.Register(_companyPlaced);

            _companyCollapsed = new EventBinding<CompanyCollapsedEvent>(e =>
                Add("COLLAPSE", $"{e.CompanyId} @ ({e.BoardPosition.x},{e.BoardPosition.y}) collapsed!", new Color(1f, 0.35f, 0.35f)));
            EventBus<CompanyCollapsedEvent>.Register(_companyCollapsed);
        }

        private static void OnCompanyPlaced(CompanyPlacedEvent e)
        {
            string id = e.Company?.BoardItem?.CompanyData?.RefCardId ?? "?";
            Add("PLACED", $"{id} placed on board", new Color(0.5f, 1f, 0.65f));

            var asc = e.Company?.AbilitySystemCharacter;
            if (asc == null) return;

            asc.OnGameplayEffectApplied += container =>
            {
                string effectName = container?.Spec?.GameplayEffect?.name ?? "effect";
                Add("EFFECT+", $"{id}: {effectName}", new Color(0.75f, 0.65f, 1f));
            };

            asc.OnGameplayEffectRemoved += container =>
            {
                string effectName = container?.Spec?.GameplayEffect?.name ?? "effect";
                Add("EFFECT-", $"{id}: {effectName} expired", new Color(0.5f, 0.45f, 0.75f));
            };

            asc.OnGrantedAbility += spec =>
            {
                string abilityName = spec?.Ability?.name ?? "ability";
                Add("ABILITY+", $"{id}: granted {abilityName}", new Color(0.6f, 0.9f, 0.6f));
            };

            asc.OnRemovedAbility += spec =>
            {
                string abilityName = spec?.Ability?.name ?? "ability";
                Add("ABILITY-", $"{id}: removed {abilityName}", new Color(0.5f, 0.6f, 0.5f));
            };
        }

        public static void Add(string category, string message, Color? color = null)
        {
            while (_entries.Count >= MaxEntries)
                _entries.RemoveAt(0);

            _entries.Add(new LogEntry
            {
                Time     = Application.isPlaying ? Time.time : 0f,
                Category = category,
                Message  = message,
                Color    = color ?? Color.white
            });

            OnChanged?.Invoke();
        }

        public static void Clear()
        {
            _entries.Clear();
            OnChanged?.Invoke();
        }
    }
}
