using System;
namespace RainbowLi {
 public enum EffectLifetime { Direct, Temporary, Persistent }
 public enum EffectPolarity { Positive, Negative, Special }
 // Polarity is an authoring category, not an instruction to invert a behavior's recipient.
 public static class CardEffectTargeting {
  public static bool TargetsOpponent(CardEffect effect)=>effect==CardEffect.Strike||effect==CardEffect.IncreasedStrike||effect==CardEffect.InverseStrike||
   effect==CardEffect.Vulnerable||effect==CardEffect.Weaken||effect==CardEffect.Poison||effect==CardEffect.Bleed||effect==CardEffect.Burn||effect==CardEffect.Disorder;
  public static bool TargetsEnemy(BattleAction action){
   if(action.ContentCompletion&&action.SourceAction!=null)return TargetsEnemy(action.SourceAction);
   if(action.IsStatusTick||action.IsNumberOnly)return action.Enemy;
   var authored=action.Rule?.EncounterEffects??action.EncounterEffects;
   if(authored!=null&&authored.Length>0){foreach(var effect in authored)if(effect.Target!=EncounterTarget.Numbers)return effect.Target==EncounterTarget.Opponent?!action.Enemy:action.Enemy;return action.Enemy;}
   if(action.Rule!=null){var effect=action.Rule.Effect;bool opponent=effect==RuleEffect.Damage||effect==RuleEffect.Burn||effect==RuleEffect.Poison||effect==RuleEffect.Bleed||effect==RuleEffect.Vulnerable||effect==RuleEffect.LastStand;return opponent?!action.Enemy:action.Enemy;}
   return TargetsOpponent(action.Effect)?!action.Enemy:action.Enemy;
  }
 }
 [Serializable]
 public sealed class CardEffectSpec {
  public string Code,Name,Text,Pools;
  public CardEffect Effect;
  public EffectLifetime Lifetime;
  public EffectPolarity Polarity;
  public bool UsesNumber,StartingEnabled=true;
  public int Min=1,Max=1;
  public int ShieldBonusPercent=25;
  public int ReductionPercent=25;
  public string ConfiguredText(){
   if(Effect==CardEffect.Weaken||Effect==CardEffect.Fortify){
    string reductionText=Text??"";string reductionPattern=Effect==CardEffect.Weaken?@"(造成的伤害减少\s*)\d+\s*[%％]":@"(受到的伤害减少\s*)\d+\s*[%％]";
    return System.Text.RegularExpressions.Regex.IsMatch(reductionText,reductionPattern)?System.Text.RegularExpressions.Regex.Replace(reductionText,reductionPattern,m=>m.Groups[1].Value+ReductionPercent+"%"):reductionText+"（伤害减幅 "+ReductionPercent+"%）";
   }
   if(Effect!=CardEffect.IronWall)return Text;
   string text=Text??"";
   const string pattern=@"(护盾(?:量)?(?:增加|提高)\s*)\d+\s*[%％]";
   return System.Text.RegularExpressions.Regex.IsMatch(text,pattern)?System.Text.RegularExpressions.Regex.Replace(text,pattern,m=>m.Groups[1].Value+ShieldBonusPercent+"%"):text+"（护盾增幅 "+ShieldBonusPercent+"%）";
  }
  public float Weight=10;
  public bool InPool(string pool)=>EffectPoolNames.Contains(Pools,pool);
  public CardEffectSpec Copy()=>new CardEffectSpec {Code=Code,Name=Name,Text=Text,Pools=Pools,Effect=Effect,Lifetime=Lifetime,Polarity=Polarity,UsesNumber=UsesNumber,StartingEnabled=StartingEnabled,Min=Min,Max=Max,Weight=Weight,ShieldBonusPercent=ShieldBonusPercent,ReductionPercent=ReductionPercent};
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
