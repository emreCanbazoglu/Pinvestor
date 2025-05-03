using Pinvestor.BoardSystem.Authoring;

namespace Pinvestor.Game
{
    public class CompanyPlacedEvent : IEvent
    {
        public BoardItemWrapper_Company Company { get; private set; }
        
        public CompanyPlacedEvent(
            BoardItemWrapper_Company company)
        {
            Company = company;
        }
    }
}