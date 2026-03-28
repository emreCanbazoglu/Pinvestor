using UnityEngine;

namespace Pinvestor.Game.Health
{
    /// <summary>
    /// Per-instance health model for a placed company.
    /// Created on placement; not shared across companies of the same type.
    /// Health is decremented by the ability system (via Damagable.OnDied / OnTookDamage)
    /// through the GAS HP attribute on the company's AbilitySystemCharacter.
    ///
    /// This class is a lightweight runtime mirror of the GAS HP attribute that
    /// provides a direct, non-GAS API for game logic (collapse checks, UI).
    /// The authoritative HP value lives in the GAS attribute system — this state
    /// is updated via TakeDamage() called from BoardItemWrapper_Company.
    /// </summary>
    public sealed class CompanyHealthState
    {
        public float MaxHealth { get; }
        public float CurrentHealth { get; private set; }

        /// <summary>True when CurrentHealth has reached 0. Set to false after revival (unused for now).</summary>
        public bool IsDead => CurrentHealth <= 0f;

        /// <summary>
        /// Flagged true when health hits 0 — collapse will be processed at resolution time.
        /// A company can be PendingCollapse but not yet removed from the board.
        /// </summary>
        public bool PendingCollapse { get; private set; }

        public CompanyHealthState(float maxHealth)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            CurrentHealth = MaxHealth;
            PendingCollapse = false;
        }

        /// <summary>
        /// Reduce health by <paramref name="amount"/> (clamped to 0).
        /// Sets PendingCollapse when health reaches 0.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (PendingCollapse)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            if (CurrentHealth <= 0f)
            {
                PendingCollapse = true;
            }
        }

        /// <summary>Directly mark as pending collapse (e.g. from OnDied callback path).</summary>
        public void MarkPendingCollapse()
        {
            CurrentHealth = 0f;
            PendingCollapse = true;
        }
    }
}
