using System;

namespace RainbowLi
{
    public readonly struct DamageResult
    {
        public readonly int RequestedAmount, ResolvedAmount, Damage, Blocked;
        public readonly int HealthBefore, HealthAfter, ShieldBefore, ShieldAfter;
        public readonly bool Parried;
        internal DamageResult(int requested, int resolved, int hp, int nextHp, int shield, int nextShield, bool parried)
        { RequestedAmount=requested; ResolvedAmount=resolved; HealthBefore=hp; HealthAfter=nextHp;
          ShieldBefore=shield; ShieldAfter=nextShield; Damage=hp-nextHp; Blocked=shield-nextShield; Parried=parried; }
    }

    // Authoritative arithmetic. Compatibility actor methods and game settlement
    // both use these operations; no Unity dependencies or event subscribers here.
    public static class CombatResolver
    {
        public static int Clamp(long value) => (int)Math.Max(0, Math.Min(int.MaxValue, value));
        public static int OutgoingDamage(CombatantState source, int amount)
        {
            if(amount<=0)return 0;
            long value=(long)amount+source.Statuses[CardEffect.Strength];
            if(source.Statuses[CardEffect.Weaken]>0)value=value*(100L-source.Statuses.WeakenReductionPercent)/100;
            return Clamp(value);
        }
        public static DamageResult Damage(CombatantState source, CombatantState target, int amount)
        {
            int hp=target.Health, shield=target.Shield;
            if(target.Statuses[CardEffect.Invulnerable]>0)return new DamageResult(amount,0,hp,hp,shield,shield,false);
            int outgoing=source==null?Math.Max(0,amount):OutgoingDamage(source,amount);
            if(outgoing==0||hp<=0)return new DamageResult(amount,0,hp,hp,shield,shield,false);
            long value=(long)outgoing+target.Statuses[CardEffect.Vulnerable];
            if(target.Statuses[CardEffect.Fortify]>0)value=value*(100L-target.Statuses.FortifyReductionPercent)/100;
            if(target.Statuses[CardEffect.DamageCap]>0)value=Math.Min(value,target.Statuses[CardEffect.DamageCap]);
            int incoming=Clamp(value);
            if(incoming==0)return new DamageResult(amount,0,hp,hp,shield,shield,false);
            if(target.Statuses[CardEffect.Parry]>0){
                target.Statuses[CardEffect.Parry]--;
                return new DamageResult(amount,incoming,hp,hp,shield,shield,true);
            }
            int blocked=Math.Min(Math.Max(0,shield),incoming);
            target.Shield=shield-blocked; target.Health=Clamp((long)hp-incoming+blocked);
            return new DamageResult(amount,incoming,hp,target.Health,shield,target.Shield,false);
        }
        public static int LoseHealth(CombatantState target, int amount)
        { int before=target.Health; target.Health=Clamp((long)before-Math.Max(0,amount)); return before-target.Health; }
        public static int Heal(CombatantState target, int amount)
        { int before=target.Health; if(before<=0||before>=target.MaxHealth||amount<=0)return 0; target.Health=(int)Math.Min(target.MaxHealth,(long)before+amount); return target.Health-before; }
        public static int GainShield(CombatantState target, int amount)
        {
            long value=Math.Max(0,amount);
            if(target.Statuses[CardEffect.IronWall]>0)value=value*(100L+target.Statuses.IronWallBonusPercent)/100;
            int before=target.Shield; target.Shield=Clamp((long)before+value); return target.Shield-before;
        }
    }
}
