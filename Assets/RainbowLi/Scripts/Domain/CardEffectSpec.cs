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
}
