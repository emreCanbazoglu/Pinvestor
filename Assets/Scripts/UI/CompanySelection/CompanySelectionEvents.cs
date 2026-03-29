using Pinvestor.CardSystem.Authoring;

namespace Pinvestor.UI
{
    public class InitializeCompanySelectionUIEvent : IEvent
    {
        public CompanySelectionPileWrapper PileWrapper { get; }

        public InitializeCompanySelectionUIEvent(
            CompanySelectionPileWrapper pileWrapper)
        {
            PileWrapper = pileWrapper;
        }
    }

    public class ShowCompanySelectionUIEvent : IEvent { }
    public class HideCompanySelectionUIEvent : IEvent { }
    public class DeactivateCompanySelectionUIEvent : IEvent { }

    public class CompanyCardSelectedEvent : IEvent
    {
        public CompanyCardWrapper CompanyCard { get; }

        public CompanyCardSelectedEvent(
            CompanyCardWrapper companyCard)
        {
            CompanyCard = companyCard;
        }
    }
}
