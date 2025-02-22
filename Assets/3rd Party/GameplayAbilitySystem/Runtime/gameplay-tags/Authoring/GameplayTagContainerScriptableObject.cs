using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameplayTag.Authoring
{
    [CreateAssetMenu(menuName = "Gameplay Ability System/Tag Container")]
    public class GameplayTagContainerScriptableObject : ScriptableObject
    {
        [SerializeField] private string _folderPath = string.Empty;
        
        [SerializeField] private List<GameplayTagScriptableObject> _gameplayTags
            = new List<GameplayTagScriptableObject>();

        public bool TryGetGameplayTag(
            int index, 
            out GameplayTagScriptableObject gameplayTagSO)
        {
            gameplayTagSO = null;
            
            if (index < 0 || index >= _gameplayTags.Count)
                return false;

            gameplayTagSO = _gameplayTags[index];

            return gameplayTagSO;
        }

        public bool TryGetIndex(
            GameplayTagScriptableObject tag,
            out uint index)
        {
            int i = _gameplayTags.IndexOf(tag);

            if (i == -1)
            {
                index = 0;

                return false;
            }

            index = (uint)i;

            return true;
        }
        
        #if UNITY_EDITOR
        public void CollectGameplayTags()
        {
            _gameplayTags.Clear();

            string[] assetPaths = AssetDatabase.FindAssets("t:GameplayTagScriptableObject", new[] { _folderPath });

            foreach (string assetPath in assetPaths)
            {
                GameplayTagScriptableObject tag 
                    = AssetDatabase.LoadAssetAtPath<GameplayTagScriptableObject>(
                        AssetDatabase.GUIDToAssetPath(assetPath));
                
                if (tag != null)
                    _gameplayTags.Add(tag);
            }
            
            EditorUtility.SetDirty(this);
        }
        #endif
    }
}
