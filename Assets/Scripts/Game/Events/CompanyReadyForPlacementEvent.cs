using Pinvestor.BoardSystem.Authoring;

namespace Pinvestor.Game
{
    public class CompanyReadyForPlacementEvent : IEvent
    {
        public BoardItemWrapper_Company CompanyWrapper { get; }

        public CompanyReadyForPlacementEvent(BoardItemWrapper_Company wrapper)
        {
            CompanyWrapper = wrapper;
        }
    }
}
