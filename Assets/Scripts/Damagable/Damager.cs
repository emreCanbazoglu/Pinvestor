using System;
using AbilitySystem;
using UnityEngine;

namespace Pinvestor.DamagableSystem
{
    public class Damager : MonoBehaviour,
        IComponentProvider<Damager>
    {
        public DamageInfo LastDamageInfo { get; private set; }
        
        public Action<AbilitySystemCharacter, DamageInfo> OnDealtDamage { get; set; }
        public Action<AbilitySystemCharacter, DamageInfo> OnKilled { get; set; }

        public Damager GetComponent()
        {
            return this;
        }

        public void HandleDealtDamage(
            AbilitySystemCharacter target,
            DamageInfo damageInfo)
        {
            LastDamageInfo = damageInfo;

            OnDealtDamage?.Invoke(target, damageInfo);
        }
        
        public void HandleKilled(
            AbilitySystemCharacter target,
            DamageInfo damageInfo)
        {
            LastDamageInfo = damageInfo;
            
            OnKilled?.Invoke(target, damageInfo);
        }
    }
}