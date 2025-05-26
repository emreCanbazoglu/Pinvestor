using System;
using UnityEngine;

namespace Pinvestor.CompanySystem
{
    [CreateAssetMenu(
        menuName = "Pinvestor/Company System/Company Card Settings",
        fileName = "CompanyCardSettings")]
    public class CompanyCardSettingsScriptableObject : ScriptableObject
    {
        [Serializable]
        public class Settings
        {
            [field: SerializeField] public ECompanyCategory CompanyCategory { get; private set; } 
                = ECompanyCategory.None;
            
            [field: SerializeField] public Color MainFrameColor { get; private set; } = Color.white;
            [field: SerializeField] public Color TopContainerColor { get; private set; } = Color.white;
            [field: SerializeField] public Color NameContainerColor { get; private set; } = Color.white;
            [field: SerializeField] public Color InfoContainerColor { get; private set; } = Color.white;
            
            [field: SerializeField] public Sprite CategoryIcon { get; private set; } = null;
        }

        [SerializeField] private Settings[] _settings
            = Array.Empty<Settings>();

        public bool TryGetSettings(
            ECompanyCategory companyCategory,
            out Settings settings)
        {
            settings = Array.Find(_settings, s => s.CompanyCategory == companyCategory);
            return settings != null;
        }
    }
}