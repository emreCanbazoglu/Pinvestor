using UnityEditor;
using UnityEngine;

namespace Pinvestor.GameConfigSystem.Editor
{
    public sealed class GameConfigEditorWindow : EditorWindow
    {
        private const string DefaultAuthoringAssetPath = "Assets/ScriptableObjects/GameConfig/GameConfigAuthoring.asset";

        private enum DomainTab
        {
            Companies,
            Balance,
            RoundCriteria,
            RunCycle,
            Ball,
            Shop
        }

        private GameConfigAuthoringAsset _authoringAsset;
        private SerializedObject _serializedObject;
        private Vector2 _scroll;
        private DomainTab _selectedTab;
        private readonly GameConfigValidationService _validationService
            = new GameConfigValidationService();
        private readonly GameConfigExportService _exportService
            = new GameConfigExportService();
        private readonly GameConfigCompanyImportService _companyImportService
            = new GameConfigCompanyImportService();
        private string _lastMessage = string.Empty;
        private MessageType _lastMessageType = MessageType.Info;

        [MenuItem("Pinvestor/Game Config/Editor")]
        public static void Open()
        {
            GetWindow<GameConfigEditorWindow>("Game Config");
        }

        private void OnEnable()
        {
            EnsureDefaultAuthoringAssetLoaded();
        }

        private void OnGUI()
        {
            EnsureDefaultAuthoringAssetLoaded();
            DrawHeader();

            if (_authoringAsset == null)
            {
                EditorGUILayout.HelpBox("Assign a GameConfigAuthoringAsset to begin editing.", MessageType.Info);
                return;
            }

            EnsureSerializedObject();
            _serializedObject.Update();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawSchema();
            DrawSelectedDomain();
            EditorGUILayout.EndScrollView();

            _serializedObject.ApplyModifiedProperties();

            if (!string.IsNullOrEmpty(_lastMessage))
            {
                EditorGUILayout.HelpBox(_lastMessage, _lastMessageType);
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Authoring Asset",
                    _authoringAsset,
                    typeof(GameConfigAuthoringAsset),
                    false);
            }

            _selectedTab = (DomainTab)GUILayout.Toolbar(
                (int)_selectedTab,
                new[] { "Companies", "Balance", "Round", "Run Cycle", "Ball", "Shop" });

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate"))
            {
                GameConfigValidationResult result = _validationService.Validate(_authoringAsset);
                _lastMessage = result.ToMultilineString();
                _lastMessageType = result.IsValid ? MessageType.Info : MessageType.Error;
            }

            if (GUILayout.Button("Import Companies"))
            {
                bool success = _companyImportService.TryImportCompanies(_authoringAsset, out string message);
                _lastMessage = message;
                _lastMessageType = success ? MessageType.Info : MessageType.Error;
                _serializedObject = null;
            }

            if (GUILayout.Button("Export JSON"))
            {
                bool success = _exportService.TryExport(_authoringAsset, out string message);
                _lastMessage = message;
                _lastMessageType = success ? MessageType.Info : MessageType.Error;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSchema()
        {
            SerializedProperty schemaProp = _serializedObject.FindProperty("_schemaVersion");
            if (schemaProp != null)
            {
                EditorGUILayout.PropertyField(schemaProp);
            }

            EditorGUILayout.Space(6);
        }

        private void DrawSelectedDomain()
        {
            switch (_selectedTab)
            {
                case DomainTab.Companies:
                    CompanyConfigDomainEditor.Draw(_serializedObject);
                    break;
                case DomainTab.Balance:
                    BalanceConfigDomainEditor.Draw(_serializedObject);
                    break;
                case DomainTab.RoundCriteria:
                    RoundCriteriaConfigDomainEditor.Draw(_serializedObject);
                    break;
                case DomainTab.RunCycle:
                    RunCycleConfigDomainEditor.Draw(_serializedObject);
                    break;
                case DomainTab.Ball:
                    BallConfigDomainEditor.Draw(_serializedObject);
                    break;
                case DomainTab.Shop:
                    ShopConfigDomainEditor.Draw(_serializedObject);
                    break;
            }
        }

        private void EnsureSerializedObject()
        {
            if (_serializedObject == null && _authoringAsset != null)
            {
                _serializedObject = new SerializedObject(_authoringAsset);
            }
        }

        private void EnsureDefaultAuthoringAssetLoaded()
        {
            if (_authoringAsset != null)
            {
                return;
            }

            _authoringAsset = AssetDatabase.LoadAssetAtPath<GameConfigAuthoringAsset>(DefaultAuthoringAssetPath);
            if (_authoringAsset != null)
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:GameConfigAuthoringAsset");
            if (guids != null && guids.Length > 0)
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                _authoringAsset = AssetDatabase.LoadAssetAtPath<GameConfigAuthoringAsset>(existingPath);
            }

            if (_authoringAsset != null)
            {
                return;
            }

            EnsureFolderPath("Assets/ScriptableObjects/GameConfig");
            _authoringAsset = ScriptableObject.CreateInstance<GameConfigAuthoringAsset>();
            AssetDatabase.CreateAsset(_authoringAsset, DefaultAuthoringAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolderPath(string fullAssetFolderPath)
        {
            string[] parts = fullAssetFolderPath.Split('/');
            if (parts.Length == 0)
            {
                return;
            }

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
