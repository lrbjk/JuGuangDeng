using System;
namespace RainbowLi {
 public enum EffectLifetime { Direct, Temporary, Persistent }
 public enum EffectPolarity { Positive, Negative, Special }
 // Polarity is an authoring category, not an instruction to invert a behavior's recipient.
 public static class CardEffectTargeting {
  public static bool TargetsOpponent(CardEffect effect)=>effect==CardEffect.Strike||effect==CardEffect.IncreasedStrike||effect==CardEffect.InverseStrike||
   effect==CardEffect.Vulnerable||effect==CardEffect.Weaken||effect==CardEffect.Poison||effect==CardEffect.Bleed||effect==CardEffect.Burn||effect==CardEffect.Disorder;
  public static bool TargetsEnemy(BattleAction action){
   if(action.IsStatusTick||action.IsNumberOnly)return action.Enemy;
   if(action.Rule!=null)return action.Rule.Effect==RuleEffect.Damage?!action.Enemy:action.Enemy;
   return TargetsOpponent(action.Effect)?!action.Enemy:action.Enemy;
  }
 }
 [Serializable]
 public sealed class CardEffectSpec {
  public string Code,Name,Text;
  public CardEffect Effect;
  public EffectLifetime Lifetime;
  public EffectPolarity Polarity;
  public bool UsesNumber,StartingEnabled=true;
  public int Min=1,Max=1;
  public float Weight=10;
  public CardEffectSpec Copy()=>new CardEffectSpec {Code=Code,Name=Name,Text=Text,Effect=Effect,Lifetime=Lifetime,Polarity=Polarity,UsesNumber=UsesNumber,StartingEnabled=StartingEnabled,Min=Min,Max=Max,Weight=Weight};
 }

 // A generated effect stores its rolled X; execution and preview never reroll it.
 [Serializable]
 public sealed class CardEffectInstance {
  public CardEffectSpec Spec;
  public int BaseX;
  public int Multiplier=1;
  public int UpgradeBonus;
  public CardEffectInstance Copy()=>new CardEffectInstance {Spec=Spec?.Copy(),BaseX=BaseX,Multiplier=Multiplier,UpgradeBonus=UpgradeBonus};
  public int ResolveValue(int number,int upgrades){
   if(Spec==null)throw new InvalidOperationException("运行时卡效缺少配置");
   if(Multiplier<1||UpgradeBonus<0)throw new InvalidOperationException("卡效倍率须大于零，升级加成不能为负数");
   int x=Spec.UsesNumber?(Spec.Effect==CardEffect.IncreasedStrike?1+number:Spec.Effect==CardEffect.InverseStrike?Math.Max(1,7-number):number):BaseX;
   long value=(long)x*Multiplier+(long)upgrades*UpgradeBonus;
   return (int)Math.Max(Spec.Effect==CardEffect.InverseStrike?1:0,Math.Min(int.MaxValue,value));
  }
 }
}
