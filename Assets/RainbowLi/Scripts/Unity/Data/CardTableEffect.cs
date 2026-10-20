using System;
namespace RainbowLi.UnityGame {
 // Authoring contract: primary effect first, then enabled additional effects in array order.
 [Serializable] public sealed class CardTableEffect {
  public bool enabled=true;public int shieldBonusPercent=25,reductionPercent=25;
  public CardEffect effect;
  public EffectLifetime lifetime;
  public EffectPolarity polarity;
  public string description="伤害 X",range="1";
  public CardTableEffect Copy()=>(CardTableEffect)MemberwiseClone();
  public CardEffectSpec Spec(string code,string name,float weight){CardTableRange.Parse(range,out bool usesNumber,out int min,out int max);var spec=new CardEffectSpec{Code=code,Name=name,Text=description,Effect=effect,Lifetime=lifetime,Polarity=polarity,UsesNumber=usesNumber,Min=min,Max=max,Weight=weight,ShieldBonusPercent=shieldBonusPercent,ReductionPercent=reductionPercent};spec.Text=spec.ConfiguredText();return spec;}
 }
}
