using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/卡牌效果定义")]
 public sealed class CardEffectDefinition : ScriptableObject {
  public string effectId,displayName;
  public string sourceSheet="Sheet1",sourceRange;public int sourceRow;
  public CardEffect effect;
  public EffectLifetime lifetime;
  public EffectPolarity polarity;
  [TextArea(2,5)] public string description;
  public bool usesCardNumber,includeInStartingDeck=true;
  [Min(1)] public int minValue=1,maxValue=1;
  [Min(.001f)] public float weight=10;
  public CardEffectSpec CreateSpec()=>new CardEffectSpec {Code=effectId,Name=displayName,Text=description,Effect=effect,Lifetime=lifetime,Polarity=polarity,UsesNumber=usesCardNumber,StartingEnabled=includeInStartingDeck,Min=minValue,Max=maxValue,Weight=weight};
  private void OnValidate(){minValue=Mathf.Max(1,minValue);maxValue=Mathf.Max(minValue,maxValue);weight=Mathf.Max(.001f,weight);}
 }
}
