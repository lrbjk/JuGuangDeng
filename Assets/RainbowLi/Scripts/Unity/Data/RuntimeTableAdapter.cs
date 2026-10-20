using System;
namespace RainbowLi.UnityGame {
 public static class RuntimeTableAdapter {
  public static EffectPoolEntry Sequence(EffectTableRow r){
   RuleEffect effect=r.behavior=="Boost"?RuleEffect.Power:r.behavior=="InsertNumber"?RuleEffect.Insert:(RuleEffect)Enum.Parse(typeof(RuleEffect),r.behavior);
   var e=Entry(r);e.Sequence=effect;return e;
  }
  public static EffectPoolEntry Engraving(EffectTableRow r){var e=Entry(r);e.Engraving=(EngravingEffect)Enum.Parse(typeof(EngravingEffect),r.behavior);return e;}
  private static EffectPoolEntry Entry(EffectTableRow r){if(!r.TryRange(out int min,out int max))min=max=r.fixedValue;return new EffectPoolEntry {Code=r.id,Pools=r.pools,Enabled=r.enabled,Weight=r.weight,Min=min,Max=max,Multiplier=r.multiplier,FixedValue=r.fixedValue,ShieldBonusPercent=r.shieldBonusPercent};}
 }
}
