using System;

namespace Pinvestor.Game.Health
{
    /// <summary>
    /// Pure per-instance state for DeferredAlpha's bounded round-level damage debt.
    /// </summary>
    public sealed class DeferredDamageState
    {
        public int MaxDeferrals { get; }
        public int PendingDamage { get; private set; }

        public DeferredDamageState(int maxDeferrals)
        {
            MaxDeferrals = Math.Max(0, maxDeferrals);
        }

        public bool TryDeferDamage()
        {
            if (PendingDamage >= MaxDeferrals)
                return false;

            PendingDamage++;
            return true;
        }

        public int ConsumePendingDamage()
        {
            int damage = PendingDamage;
            PendingDamage = 0;
            return damage;
        }
    }
}
