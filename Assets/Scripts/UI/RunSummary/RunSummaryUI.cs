using MMFramework.MMUI;
using Pinvestor.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityWeld.Binding;

namespace Pinvestor.UI
{
    /// <summary>
    /// End-of-run summary panel. Listens for ShowRunSummaryEvent (raised by GameManager
    /// after the run cycle completes), shows the outcome + run stats, and raises
    /// RunSummaryDismissedEvent when the player clicks Play Again so GameManager can
    /// reload the scene.
    /// </summary>
    [Binding]
    public class RunSummaryUI : VMBase
    {
        [SerializeField] private TextMeshProUGUI _titleText = null;
        [SerializeField] private TextMeshProUGUI _subtitleText = null;
        [SerializeField] private TextMeshProUGUI _worthText = null;
        [SerializeField] private TextMeshProUGUI _statsText = null;
        [SerializeField] private Button _playAgainButton = null;

        [SerializeField] private Color _winTitleColor = new Color(0.45f, 0.9f, 0.45f);
        [SerializeField] private Color _lossTitleColor = new Color(0.95f, 0.35f, 0.35f);

        private EventBinding<ShowRunSummaryEvent> _showBinding;

        protected override void AwakeCustomActions()
        {
            _showBinding = new EventBinding<ShowRunSummaryEvent>(OnShowRunSummary);
            EventBus<ShowRunSummaryEvent>.Register(_showBinding);

            if (_playAgainButton != null)
                _playAgainButton.onClick.AddListener(OnPlayAgainClick);

            base.AwakeCustomActions();
        }

        protected override void OnDestroyCustomActions()
        {
            EventBus<ShowRunSummaryEvent>.Deregister(_showBinding);

            if (_playAgainButton != null)
                _playAgainButton.onClick.RemoveListener(OnPlayAgainClick);

            base.OnDestroyCustomActions();
        }

        private void OnShowRunSummary(ShowRunSummaryEvent e)
        {
            if (_titleText != null)
            {
                _titleText.text = e.IsWin ? "EXIT ACHIEVED" : "MARKET CORRECTION";
                _titleText.color = e.IsWin ? _winTitleColor : _lossTitleColor;
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = e.IsWin
                    ? "You beat the market. The market will remember this."
                    : "Your portfolio is now a cautionary tale.";
            }

            if (_worthText != null)
            {
                _worthText.text =
                    $"Final Net Worth: ${e.FinalNetWorth:N0} / ${e.TargetNetWorth:N0}\n" +
                    $"Rounds Survived: {e.CompletedRoundCount} / {e.TotalRoundCount}";
            }

            if (_statsText != null)
            {
                _statsText.text =
                    $"Companies Acquired: {e.CompaniesPlaced}\n" +
                    $"Companies Collapsed: {e.CompaniesCollapsed}\n" +
                    $"Companies Cashed Out: {e.CompaniesCashedOut} (${e.TotalCashoutPayout:N0})";
            }

            TryActivate();
        }

        [Binding]
        public void OnPlayAgainClick()
        {
            TryDeactivate();
            EventBus<RunSummaryDismissedEvent>.Raise(new RunSummaryDismissedEvent());
        }
    }
}
