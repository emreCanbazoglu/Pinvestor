using System;
using Pinvestor.BoardSystem.Authoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinvestor.UI.Offer
{
    /// <summary>
    /// One row in the offer panel's Portfolio section: company name, health,
    /// cashout payout value, and a cashout button. Populated by CompanyOfferPanel.
    /// </summary>
    public class Widget_PortfolioRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText = null;
        [SerializeField] private TextMeshProUGUI _healthText = null;
        [SerializeField] private TextMeshProUGUI _payoutText = null;
        [SerializeField] private Button _cashoutButton = null;

        private Action _onCashoutClicked;

        private void Awake()
        {
            if (_cashoutButton != null)
                _cashoutButton.onClick.AddListener(OnCashoutButtonClicked);
        }

        private void OnDestroy()
        {
            if (_cashoutButton != null)
                _cashoutButton.onClick.RemoveListener(OnCashoutButtonClicked);
        }

        public void Populate(
            BoardItemWrapper_Company companyWrapper,
            Action onCashoutClicked)
        {
            _onCashoutClicked = onCashoutClicked;

            string companyId = companyWrapper.Company?.CompanyId?.CompanyId ?? "???";
            var healthState = companyWrapper.HealthState;
            float payout = companyWrapper.ValuationModel?.CashoutValue ?? 0f;

            if (_nameText != null)
                _nameText.text = companyId;

            if (_healthText != null)
            {
                _healthText.text = healthState != null
                    ? $"{Mathf.CeilToInt(healthState.CurrentHealth)}/{Mathf.CeilToInt(healthState.MaxHealth)} HP"
                    : "-";
            }

            if (_payoutText != null)
                _payoutText.text = $"+${payout:N0}";

            // T021: no cashing out a doomed (or dead) company.
            bool canCashout = healthState != null
                && !healthState.PendingCollapse
                && !healthState.IsDead;

            if (_cashoutButton != null)
                _cashoutButton.interactable = canCashout;
        }

        private void OnCashoutButtonClicked()
        {
            _onCashoutClicked?.Invoke();
        }
    }
}
