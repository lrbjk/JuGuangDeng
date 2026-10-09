using System;
namespace RainbowLi.UnityGame {
 [Serializable] public sealed class CardTableRow {
  public string id, name, description, range, notes;
  public CardEffect effect;
  public EffectLifetime lifetime;
  public EffectPolarity polarity;
  public bool enabled=true, starting=true, usesNumber;
  public int number=1, min=1, max=1;
  public float weight=10;
  public bool special;
  public ActivationCondition condition;
  public int threshold=3;
  public float specialWeightRatio=.5f;
  public CardTableRow Copy() => (CardTableRow)MemberwiseClone();
  public CardEffectSpec Spec() => new CardEffectSpec {Code=id,Name=name,Text=description,Effect=effect,Lifetime=lifetime,Polarity=polarity,UsesNumber=usesNumber,StartingEnabled=starting,Min=min,Max=max,Weight=weight};
  public static CardTableRow From(CardEffectDefinition d) => new CardTableRow {id=d.effectId,name=d.displayName,description=d.description,range=d.sourceRange,effect=d.effect,lifetime=d.lifetime,polarity=d.polarity,usesNumber=d.usesCardNumber,min=d.minValue,max=d.maxValue,weight=d.weight,starting=d.includeInStartingDeck};
 }
}
