using System;
using System.Linq;
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
  public CardTableEffect[] additionalEffects=Array.Empty<CardTableEffect>();
  public bool HasCompoundEffects=>(additionalEffects??Array.Empty<CardTableEffect>()).Any(e=>e!=null&&e.enabled);
  public CardTableRow Copy(){var copy=(CardTableRow)MemberwiseClone();copy.additionalEffects=(additionalEffects??Array.Empty<CardTableEffect>()).Select(e=>e?.Copy()).ToArray();return copy;}
  public void NormalizeRange(){CardTableRange.Parse(range,out usesNumber,out min,out max);}
  public CardEffectSpec[] EffectSpecs(){var copy=Copy();copy.NormalizeRange();var primary=new CardEffectSpec {Code=id,Name=name,Text=description,Effect=effect,Lifetime=lifetime,Polarity=polarity,UsesNumber=copy.usesNumber,StartingEnabled=starting,Min=copy.min,Max=copy.max,Weight=weight};return new[]{primary}.Concat((additionalEffects??Array.Empty<CardTableEffect>()).Select((e,i)=>new{e,i}).Where(p=>p.e!=null&&p.e.enabled).Select(p=>p.e.Spec(id+"/"+(p.i+2),name,weight))).ToArray();}
  public CardEffectSpec Spec(){if(HasCompoundEffects)throw new NotSupportedException(id+"：包含复合效果，玩法尚未接入 EffectSpecs() 多效果结算。请先禁用追加效果，不能只执行第一个效果。");return EffectSpecs()[0];}
  public static CardTableRow From(CardEffectDefinition d) => new CardTableRow {id=d.effectId,name=d.displayName,description=d.description,range=d.sourceRange,effect=d.effect,lifetime=d.lifetime,polarity=d.polarity,usesNumber=d.usesCardNumber,min=d.minValue,max=d.maxValue,weight=d.weight,starting=d.includeInStartingDeck};
 }
}
