using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using AssetVariants;

[CanEditMultipleObjects]
[CustomEditor(typeof(FolderedExampleSO))]
public class FolderedExampleSOEditor : FolderedEditor<FolderedExampleSO> { }
#endif

[CreateAssetMenu(menuName = "Asset Variants Examples/FolderedExampleSO")]
public class FolderedExampleSO : ExampleScriptableObject
{
#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void Init()
    {
        AVHeader.parentFilterTypes[typeof(FolderedExampleSO)] = typeof(ExampleScriptableObject);
    }
#endif

    [Space(50)]
    public string checkIfExistsAgainAfterUndoRevertMScriptToParent;
    
    [TextArea(4, 7)]
    public string folderedEditorIsIMGUI = "FolderedEditor only supports IMGUI, at least currently.";
}