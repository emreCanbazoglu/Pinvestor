using System;
using System.Collections.Generic;
using AbilitySystem;
using AttributeSystem.Components;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Diagnostics;
using Pinvestor.Game;
using UnityEditor;
using UnityEngine;

namespace Pinvestor.Editor
{
    /// <summary>
    /// Live board state debugger. Shows all placed companies, their HP, attributes,
    /// active Gameplay Effects, and granted abilities. Right panel streams game events
    /// logged via GameEventLog.
    ///
    /// Open via: Pinvestor → Board State Viewer  (or Ctrl+Shift+B)
    /// </summary>
    public class BoardStateEditorWindow : EditorWindow
    {
        // ── Layout constants ─────────────────────────────────────────────────────
        private const float LogPanelWidth    = 290f;
        private const float DividerWidth     = 2f;
        private const float AutoRefreshRate  = 0.4f;     // seconds between repaints in play mode

        // ── State ─────────────────────────────────────────────────────────────────
        private Vector2 _boardScroll;
        private Vector2 _logScroll;
        private float   _refreshTimer;
        private bool    _autoRefresh       = true;
        private bool    _pinLogToBottom    = true;

        // Per-company foldout state
        private readonly Dictionary<string, bool> _effectFoldouts  = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _abilityFoldouts = new Dictionary<string, bool>();

        // GC-friendly buffer for modifier infos
        private readonly List<AbilitySystemCharacter.AppliedModifierInfo> _modInfoBuffer
            = new List<AbilitySystemCharacter.AppliedModifierInfo>();

        // ── Cached styles (created on first use) ─────────────────────────────────
        private GUIStyle _companyHeaderStyle;
        private GUIStyle _monospaceStyle;
        private bool     _stylesInitialized;

        // ═════════════════════════════════════════════════════════════════════════
        [MenuItem("Pinvestor/Board State Viewer %#b")]
        public static void Open() => GetWindow<BoardStateEditorWindow>("Board State");

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            GameEventLog.OnChanged   += OnLogChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            GameEventLog.OnChanged   -= OnLogChanged;
        }

        private void OnLogChanged()
        {
            if (_pinLogToBottom)
                Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!_autoRefresh || !Application.isPlaying) return;
            _refreshTimer += 0.05f;
            if (_refreshTimer >= AutoRefreshRate)
            {
                _refreshTimer = 0f;
                Repaint();
            }
        }

        // ── Main draw ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            float boardWidth = position.width - LogPanelWidth - DividerWidth;

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            // Board panel
            EditorGUILayout.BeginVertical(GUILayout.Width(boardWidth));
            DrawBoardPanel(boardWidth);
            EditorGUILayout.EndVertical();

            // Vertical divider
            var divRect = GUILayoutUtility.GetRect(DividerWidth, position.height, GUILayout.Width(DividerWidth));
            EditorGUI.DrawRect(divRect, new Color(0.15f, 0.15f, 0.15f));

            // Log panel
            EditorGUILayout.BeginVertical(GUILayout.Width(LogPanelWidth));
            DrawLogPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        // ── Toolbar ───────────────────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh",
                EditorStyles.toolbarButton, GUILayout.Width(88));

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(64)))
                Repaint();

            GUILayout.FlexibleSpace();

            _pinLogToBottom = GUILayout.Toggle(_pinLogToBottom, "Log ↓",
                EditorStyles.toolbarButton, GUILayout.Width(50));

            if (GUILayout.Button("Clear Log", EditorStyles.toolbarButton, GUILayout.Width(68)))
                GameEventLog.Clear();

            EditorGUILayout.EndHorizontal();
        }

        // ── Board panel ───────────────────────────────────────────────────────────
        private void DrawBoardPanel(float width)
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to inspect live board state.", MessageType.Info);
                return;
            }

            if (GameManager.Instance == null)
            {
                EditorGUILayout.LabelField("GameManager not available.", EditorStyles.miniLabel);
                return;
            }

            var board = GameManager.Instance.BoardWrapper?.Board;
            if (board == null)
            {
                EditorGUILayout.LabelField("Board not initialized.", EditorStyles.miniLabel);
                return;
            }

            int count = 0;
            _boardScroll = EditorGUILayout.BeginScrollView(_boardScroll);

            foreach (var boardItem in board.BoardItems)
            {
                if (!(boardItem is BoardItem_Company companyItem)) continue;
                if (!(companyItem.Wrapper is BoardItemWrapper_Company wrapper)) continue;

                DrawCompanyPanel(companyItem, wrapper);
                GUILayout.Space(4);
                count++;
            }

            if (count == 0)
                EditorGUILayout.LabelField("No companies on board.", EditorStyles.miniLabel);

            EditorGUILayout.EndScrollView();
        }

        // ── Company panel ─────────────────────────────────────────────────────────
        private void DrawCompanyPanel(BoardItem_Company item, BoardItemWrapper_Company wrapper)
        {
            string id     = item.CompanyData?.RefCardId ?? "Unknown";
            var    health = wrapper.HealthState;
            var    asc    = wrapper.AbilitySystemCharacter;
            var    attrSys = wrapper.AttributeSystemComponent;

            bool collapsing = health?.PendingCollapse == true;
            var  bgColor    = collapsing
                ? new Color(0.35f, 0.08f, 0.08f)
                : new Color(0.21f, 0.21f, 0.21f);

            // Draw background behind the whole panel
            var panelRect = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(panelRect, bgColor);

            // ── Header ────────────────────────────────────────────────────────────
            string pos = item.MainPiece?.Cell != null
                ? $"  ({item.MainPiece.Cell.Position.x}, {item.MainPiece.Cell.Position.y})"
                : "  (?, ?)";
            string collapseTag = collapsing ? "  ⚠ COLLAPSING" : "";

            EditorGUILayout.LabelField($"  {id}{pos}{collapseTag}", _companyHeaderStyle);

            // ── HP bar ────────────────────────────────────────────────────────────
            if (health != null)
                DrawHpBar(health.CurrentHealth, health.MaxHealth);

            // ── Attributes ────────────────────────────────────────────────────────
            if (attrSys != null)
                DrawAttributes(attrSys);

            // ── Active GEs ────────────────────────────────────────────────────────
            if (asc != null && asc.AppliedGameplayEffects.Count > 0)
                DrawActiveEffects(id, asc);

            // ── Granted abilities ─────────────────────────────────────────────────
            if (asc != null && asc.GrantedAbilities.Count > 0)
                DrawAbilities(id, asc);

            GUILayout.Space(2);
            EditorGUILayout.EndVertical();
        }

        // ── HP bar ────────────────────────────────────────────────────────────────
        private void DrawHpBar(float current, float max)
        {
            if (max <= 0f) return;

            float ratio    = Mathf.Clamp01(current / max);
            Color barColor = ratio > 0.6f
                ? new Color(0.25f, 0.78f, 0.25f)
                : ratio > 0.3f
                    ? new Color(0.9f, 0.70f, 0.1f)
                    : new Color(0.88f, 0.18f, 0.18f);

            var outerRect = GUILayoutUtility.GetRect(0, 14, GUILayout.ExpandWidth(true));
            outerRect = new Rect(outerRect.x + 6, outerRect.y + 1, outerRect.width - 12, outerRect.height - 2);

            EditorGUI.DrawRect(outerRect, new Color(0.12f, 0.12f, 0.12f));
            EditorGUI.DrawRect(new Rect(outerRect.x, outerRect.y, outerRect.width * ratio, outerRect.height), barColor);
            GUI.Label(outerRect, $"  HP  {current:F0} / {max:F0}", EditorStyles.miniLabel);
        }

        // ── Attributes ────────────────────────────────────────────────────────────
        private static readonly HashSet<string> _skipAttributeNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase) { "HP", "MaxHP", "Max HP", "Max_HP" };

        private void DrawAttributes(AttributeSystemComponent attrSys)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.BeginVertical();

            foreach (var kv in attrSys.AttributeIndexCache)
            {
                var attr = kv.Key;
                if (attr == null) continue;

                string attrName = string.IsNullOrWhiteSpace(attr.Name) ? attr.name : attr.Name;
                if (_skipAttributeNames.Contains(attrName)) continue;

                if (!attrSys.TryGetAttributeValue(attr, out var val)) continue;

                string modText = string.Empty;
                if (!Mathf.Approximately(val.Modifier.Add, 0f) || !Mathf.Approximately(val.Modifier.Multiply, 0f))
                    modText = $"  [add {val.Modifier.Add:+0.##;-0.##}  ×{1f + val.Modifier.Multiply:F2}]";

                EditorGUILayout.LabelField(
                    $"{attrName}:  {val.BaseValue:F2} → {val.CurrentValue:F2}{modText}",
                    _monospaceStyle);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        // ── Active GEs ────────────────────────────────────────────────────────────
        private void DrawActiveEffects(string id, AbilitySystemCharacter asc)
        {
            if (!_effectFoldouts.ContainsKey(id)) _effectFoldouts[id] = true;
            _effectFoldouts[id] = EditorGUILayout.Foldout(
                _effectFoldouts[id],
                $"  Active Effects ({asc.AppliedGameplayEffects.Count})",
                true);

            if (!_effectFoldouts[id]) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            EditorGUILayout.BeginVertical();

            foreach (var container in asc.AppliedGameplayEffects)
            {
                if (container?.Spec == null) continue;

                string effectName = container.Spec.GameplayEffect?.name ?? "?";
                EDurationPolicy durPolicy = container.Spec.GameplayEffect?.gameplayEffect.DurationPolicy
                    ?? EDurationPolicy.Instant;

                string durationText = durPolicy == EDurationPolicy.Infinite
                    ? "[∞]"
                    : durPolicy == EDurationPolicy.HasDuration
                        ? $"[{container.Spec.DurationRemaining:F1}s / {container.Spec.TotalDuration:F1}s]"
                        : "[instant]";

                var oldColor = GUI.color;
                GUI.color = new Color(0.75f, 0.85f, 1f);
                EditorGUILayout.LabelField($"• {effectName}  {durationText}", _monospaceStyle);
                GUI.color = oldColor;

                if (container.Modifiers == null) continue;
                foreach (var mod in container.Modifiers)
                {
                    if (mod?.Attribute == null) continue;
                    string attrName = string.IsNullOrWhiteSpace(mod.Attribute.Name)
                        ? mod.Attribute.name
                        : mod.Attribute.Name;

                    string modLine = $"    └ {attrName}:";
                    if (!Mathf.Approximately(mod.Modifier.Add, 0f))
                        modLine += $"  add {mod.Modifier.Add:+0.##;-0.##}";
                    if (!Mathf.Approximately(mod.Modifier.Multiply, 0f))
                        modLine += $"  mul ×{1f + mod.Modifier.Multiply:F2}";
                    if (!Mathf.Approximately(mod.Modifier.Override, 0f))
                        modLine += $"  override {mod.Modifier.Override:F2}";

                    EditorGUILayout.LabelField(modLine, _monospaceStyle);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        // ── Granted abilities ─────────────────────────────────────────────────────
        private void DrawAbilities(string id, AbilitySystemCharacter asc)
        {
            if (!_abilityFoldouts.ContainsKey(id)) _abilityFoldouts[id] = false;
            _abilityFoldouts[id] = EditorGUILayout.Foldout(
                _abilityFoldouts[id],
                $"  Abilities ({asc.GrantedAbilities.Count})",
                true);

            if (!_abilityFoldouts[id]) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            EditorGUILayout.BeginVertical();

            foreach (var spec in asc.GrantedAbilities)
            {
                if (spec?.Ability == null) continue;
                EditorGUILayout.LabelField($"• {spec.Ability.name}", _monospaceStyle);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        // ── Log panel ─────────────────────────────────────────────────────────────
        private void DrawLogPanel()
        {
            EditorGUILayout.LabelField("Event Log", EditorStyles.boldLabel);

            var entries = GameEventLog.Entries;

            if (_pinLogToBottom)
                _logScroll.y = float.MaxValue;

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));

            // Draw newest-first
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];

                var oldColor = GUI.color;
                GUI.color = entry.Color;
                EditorGUILayout.LabelField(
                    $"{entry.Time,6:F1}s  [{entry.Category,-8}]  {entry.Message}",
                    _monospaceStyle);
                GUI.color = oldColor;
            }

            if (entries.Count == 0)
                EditorGUILayout.LabelField("No events yet.", EditorStyles.miniLabel);

            EditorGUILayout.EndScrollView();
        }

        // ── Style helpers ─────────────────────────────────────────────────────────
        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _companyHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal   = { textColor = new Color(0.95f, 0.95f, 0.95f) }
            };

            _monospaceStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                font = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font
                    ?? EditorStyles.miniLabel.font,
                richText  = false,
                wordWrap  = false,
                clipping  = TextClipping.Clip,
                normal    = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
        }
    }
}
