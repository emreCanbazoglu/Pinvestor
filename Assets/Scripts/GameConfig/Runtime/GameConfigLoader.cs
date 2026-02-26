using UnityEngine;

namespace Pinvestor.GameConfigSystem
{
    public sealed class GameConfigLoader
    {
        public bool TryLoadFromResources(
            string resourcesPath,
            out GameConfigRootJsonDto dto,
            out string error)
        {
            dto = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(resourcesPath))
            {
                error = "GameConfig resources path is empty.";
                return false;
            }

            TextAsset textAsset = Resources.Load<TextAsset>(resourcesPath);
            if (textAsset == null)
            {
                error = $"GameConfig JSON not found at Resources path '{resourcesPath}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(textAsset.text))
            {
                error = $"GameConfig JSON is empty at Resources path '{resourcesPath}'.";
                return false;
            }

            try
            {
                dto = JsonUtility.FromJson<GameConfigRootJsonDto>(textAsset.text);
            }
            catch (System.Exception e)
            {
                error = $"GameConfig JSON parse exception at '{resourcesPath}': {e.Message}";
                return false;
            }

            if (dto == null)
            {
                error = $"GameConfig JSON parse returned null at '{resourcesPath}'.";
                return false;
            }

            if (dto.schemaVersion <= 0)
            {
                error = "GameConfig schemaVersion must be greater than 0.";
                return false;
            }

            return true;
        }
    }
}

