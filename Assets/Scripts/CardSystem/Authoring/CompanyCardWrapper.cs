using Pinvestor.UI;
using UnityEngine;

namespace Pinvestor.CardSystem.Authoring
{
    public class CompanyCardWrapper : CardWrapperBase
    {
        [field: SerializeField] public Widget_CompanyCard Widget { get; private set; } = null;
        
        public CompanyCard CompanyCard => (CompanyCard)Card;

        protected override void WrapCardCore()
        {
            gameObject.name = "CompanyCardWrapper_" + CompanyCard.CastedCardDataSo.CompanyId.CompanyId;

            LogAbilities();
        }

        private void LogAbilities()
        {
            foreach (var triggerDef in CompanyCard.CastedCardDataSo.AbilityTriggerDefinitions)
            {
                Debug.Log(triggerDef.Ability.GetDescription());
            }
        }
    }
}