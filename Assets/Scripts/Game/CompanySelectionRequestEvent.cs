using System;
using System.Collections.Generic;
using Pinvestor.CardSystem;

namespace Pinvestor.Game
{
    public class CompanySelectionRequestEvent : IEvent
    {
        public List<CompanyCard> CompanyCards { get; private set; }
        
        public Action<CompanyCard> OnCompanyCardSelected { get; private set; }
        
        public CompanySelectionRequestEvent(
            List<CompanyCard> companyCards,
            Action<CompanyCard> onCompanyCardSelected)
        {
            CompanyCards = companyCards;
            OnCompanyCardSelected = onCompanyCardSelected;
        }
    }
}