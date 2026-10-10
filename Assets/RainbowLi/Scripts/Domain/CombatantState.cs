using System;
namespace RainbowLi {
 public abstract class CombatantState {
  public int Health, MaxHealth, Shield, Power;
  public readonly CombatStatuses Statuses=new CombatStatuses();
  public int OutgoingDamage(int amount)=>CombatResolver.OutgoingDamage(this,amount);
  public void GainShield(int amount){CombatResolver.GainShield(this,amount);}
  public void LoseHealth(int amount){CombatResolver.LoseHealth(this,amount);}
  public void ResetRound(){Shield=0;Power=0;}
  public void ReceiveDamage(int amount){CombatResolver.Damage(null,this,amount);}
  public void Heal(int amount){CombatResolver.Heal(this,amount);}
 }
}
