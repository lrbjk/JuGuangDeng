using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/卡牌效果定义")]
 public sealed class CardEffectDefinition : ScriptableObject {
  public string effectId,displayName,pools;
  public string sourceSheet="Sheet1",sourceRange;public int sourceRow;
  public CardEffect effect;
  public EffectLifetime lifetime;
  public EffectPolarity polarity;
  [TextArea(2,5)] public string description;
  public bool usesCardNumber,includeInStartingDeck=true;
  [Min(1)] public int minValue=1,maxValue=1;
  [Min(.001f)] public float weight=10;[Tooltip("铁壁的护盾增幅，默认25%")][Range(0,10000)]public int shieldBonusPercent=25;[Tooltip("弱化/坚固的伤害减幅，默认25%")][Range(0,100)]public int reductionPercent=25;
  public CardEffectSpec CreateSpec(){var spec=new CardEffectSpec {Code=effectId,Name=displayName,Text=description,Pools=pools,Effect=effect,Lifetime=lifetime,Polarity=polarity,UsesNumber=usesCardNumber,StartingEnabled=includeInStartingDeck,Min=minValue,Max=maxValue,Weight=weight,ShieldBonusPercent=shieldBonusPercent,ReductionPercent=reductionPercent};spec.Text=spec.ConfiguredText();return spec;}
  private void OnValidate(){minValue=Mathf.Max(1,minValue);maxValue=Mathf.Max(minValue,maxValue);weight=Mathf.Max(.001f,weight);shieldBonusPercent=Mathf.Clamp(shieldBonusPercent,0,10000);reductionPercent=Mathf.Clamp(reductionPercent,0,100);}
 }
}
