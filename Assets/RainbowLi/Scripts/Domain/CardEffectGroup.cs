using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi {
 // One configured row is one weighted candidate, regardless of its effect count.
 [Serializable] public sealed class CardEffectGroup {
  public CardEffectSpec[] Specs=Array.Empty<CardEffectSpec>();
  public CardEffectSpec Primary=>Specs!=null&&Specs.Length>0?Specs[0]:throw new InvalidOperationException("卡牌效果组为空。");
  public CardEffectGroup Copy()=>new CardEffectGroup{Specs=Specs.Select(s=>s.Copy()).ToArray()};
  public static int UpgradeFor(CardEffect effect)=>effect==CardEffect.DamageCap||effect==CardEffect.Reciprocity||effect==CardEffect.Disorder?0:2;
  public static List<CardEffectInstance> CreateEffects(CardEffectSpec[] specs,Random random,bool upgrades=true){
   if(specs==null||specs.Length==0||specs.Length>100)throw new InvalidOperationException("卡牌必须有1–100个效果。");
   return specs.Select(s=>{if(s==null||s.Min<0||s.Max<s.Min||s.Max==int.MaxValue)throw new InvalidOperationException("卡牌效果配置或数值范围无效。");return new CardEffectInstance{Spec=s.Copy(),BaseX=random==null?s.Min:random.Next(s.Min,s.Max+1),Multiplier=1,UpgradeBonus=upgrades?UpgradeFor(s.Effect):0};}).ToList();
  }
  public Card CreateCard(int number,Random random=null,int attack=0){
   var effects=CreateEffects(Specs,random);var p=effects[0];var card=new Card{Number=number,Effect=p.Spec.Effect,Spec=p.Spec.Copy(),EffectAmount=p.BaseX,AttackBase=attack,BaseBonus=0,UpgradeBonus=p.UpgradeBonus};
   if(effects.Count>1)card.Effects=effects;return card;
  }
 }
}
