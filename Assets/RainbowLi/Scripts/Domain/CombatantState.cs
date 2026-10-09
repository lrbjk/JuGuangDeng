using System;
namespace RainbowLi {
 public abstract class CombatantState {
  public int Health, MaxHealth, Shield, Power;
  public readonly CombatStatuses Statuses=new CombatStatuses();
  public int OutgoingDamage(int amount){amount=Math.Max(0,amount+Statuses[CardEffect.Strength]);return Statuses[CardEffect.Weaken]>0?amount*3/4:amount;}
  public void GainShield(int amount){amount=Math.Max(0,amount);if(Statuses[CardEffect.IronWall]>0)amount=amount*5/4;Shield+=amount;}
  public void LoseHealth(int amount){Health=Math.Max(0,Health-Math.Max(0,amount));}
  public void ResetRound(){Shield=0;Power=0;}
  public void ReceiveDamage(int amount){
   amount=Math.Max(0,amount)+Statuses[CardEffect.Vulnerable];
   if(Statuses[CardEffect.Fortify]>0)amount=amount*3/4;
   if(Statuses[CardEffect.DamageCap]>0)amount=Math.Min(amount,Statuses[CardEffect.DamageCap]);
   if(amount==0)return;
   if(Statuses[CardEffect.Parry]>0){Statuses[CardEffect.Parry]--;return;}
   int blocked=Math.Min(Shield,amount);Shield-=blocked;Health=Math.Max(0,Health-amount+blocked);
  }
  public void Heal(int amount){Health=Math.Min(MaxHealth,Health+Math.Max(0,amount));}
 }
}
