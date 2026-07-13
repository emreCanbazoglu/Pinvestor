using System;

namespace Pinvestor.Game
{
    /// <summary>
    /// Accumulates per-run statistics for the run summary screen by listening to
    /// gameplay events. Create at run start, Dispose after the summary is shown.
    /// </summary>
    public sealed class RunStatsTracker : IDisposable
    {
        public int CompaniesPlaced { get; private set; }
        public int CompaniesCollapsed { get; private set; }
        public int CompaniesCashedOut { get; private set; }
        public float TotalCashoutPayout { get; private set; }

        private readonly EventBinding<CompanyPlacedEvent> _placedBinding;
        private readonly EventBinding<CompanyCollapsedEvent> _collapsedBinding;
        private readonly EventBinding<CompanyCashedOutEvent> _cashedOutBinding;

        private bool _isDisposed;

        public RunStatsTracker()
        {
            _placedBinding = new EventBinding<CompanyPlacedEvent>(OnCompanyPlaced);
            _collapsedBinding = new EventBinding<CompanyCollapsedEvent>(OnCompanyCollapsed);
            _cashedOutBinding = new EventBinding<CompanyCashedOutEvent>(OnCompanyCashedOut);

            EventBus<CompanyPlacedEvent>.Register(_placedBinding);
            EventBus<CompanyCollapsedEvent>.Register(_collapsedBinding);
            EventBus<CompanyCashedOutEvent>.Register(_cashedOutBinding);
        }

        private void OnCompanyPlaced(CompanyPlacedEvent e)
        {
            CompaniesPlaced++;
        }

        private void OnCompanyCollapsed(CompanyCollapsedEvent e)
        {
            CompaniesCollapsed++;
        }

        private void OnCompanyCashedOut(CompanyCashedOutEvent e)
        {
            CompaniesCashedOut++;
            TotalCashoutPayout += e.PayoutAmount;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            EventBus<CompanyPlacedEvent>.Deregister(_placedBinding);
            EventBus<CompanyCollapsedEvent>.Deregister(_collapsedBinding);
            EventBus<CompanyCashedOutEvent>.Deregister(_cashedOutBinding);
        }
    }
}
