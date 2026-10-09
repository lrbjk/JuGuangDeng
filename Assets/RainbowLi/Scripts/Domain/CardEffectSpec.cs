using System;
namespace RainbowLi {
 public enum EffectLifetime { Direct, Temporary, Persistent }
 public enum EffectPolarity { Positive, Negative, Special }
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
