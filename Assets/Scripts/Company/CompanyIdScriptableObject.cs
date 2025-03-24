using UnityEngine;

namespace Pinvestor.CompanySystem
{
    [CreateAssetMenu(
        menuName = "Pinvestor/Company System/Company Id",
        fileName = "CompanyId")]
    public class CompanyIdScriptableObject : UniqueScriptableObject
    {
        [field: SerializeField] public string CompanyId { get; private set; } = string.Empty;
    }
}