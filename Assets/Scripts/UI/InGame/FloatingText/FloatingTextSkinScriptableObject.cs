using UnityEngine;

namespace Pinvestor.UI
{
    [CreateAssetMenu(
        menuName = "Pinvestor/UI/In Game UI/Floating Text Skin",
        fileName = "FloatingTextSkin")]
    public class FloatingTextSkinScriptableObject : ScriptableObject
    {
        [field: SerializeField] public float TextSize = 10;
        [field: SerializeField] public Color TextColor = Color.red;
        [field: SerializeField] public AnimationCurve MovementCurve = default;
        [field: SerializeField] public float YMovement = 1.0f;
        [field: SerializeField] public AnimationCurve FadeOutCurve = default;
        [field: SerializeField] public float Lifetime = 1.0f;
        [field: SerializeField] public string Prefix = string.Empty;
        [field: SerializeField] public string Suffix = string.Empty;
    }
}
