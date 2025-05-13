namespace Pinvestor.DamagableSystem
{
    public struct DamageInfo
    {
        public float DamageAmount { get; private set; }
        public bool IsCrit { get; private set; }
        public float CurrentHealth { get; private set; }
        public DamageInfo(
            float damageAmount,
            bool isCrit,
            float currentHealth = 0f)
        {
            DamageAmount = damageAmount;
            IsCrit = isCrit;
            CurrentHealth = currentHealth;
        }
    }
}