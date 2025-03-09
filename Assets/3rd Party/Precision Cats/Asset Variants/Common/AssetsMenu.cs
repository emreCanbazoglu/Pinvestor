using UnityEngine;
using UnityEditor;

internal class AssetsMenu
{
    // DuplicateAsset remedies the bug where Unity doesn't copy user data when duplicating an asset

    [MenuItem("Assets/Duplicate Asset", false, 1110)]
    private static void DuplicateAsset()
    {
        Object parent = Selection.activeObject;
        string parentPath = AssetDatabase.GetAssetPath(parent);
        string childPath = AssetDatabase.GenerateUniqueAssetPath(parentPath);
        AssetDatabase.CopyAsset(parentPath, childPath);
        AssetDatabase.Refresh();
        var parentImporter = AssetImporter.GetAtPath(parentPath);
        var childImporter = AssetImporter.GetAtPath(childPath);
        childImporter.userData = parentImporter.userData;
        childImporter.SaveAndReimport();
    }
    [MenuItem("Assets/Duplicate Asset", true)]
    private static bool ValidateDuplicateAsset()
    {
        return !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject));
    }
}
