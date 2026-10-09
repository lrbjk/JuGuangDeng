using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi {
 public sealed class CombatStatuses {
  private readonly Dictionary<CardEffect,EffectLifetime> lifetimes=new Dictionary<CardEffect,EffectLifetime>();
  private readonly Dictionary<CardEffect,int> values=new Dictionary<CardEffect,int>();
  public int this[CardEffect effect] {get=>values.TryGetValue(effect,out int value)?value:0;set{if(value<=0)values.Remove(effect);else values[effect]=value;}}
  public bool Any=>values.Count>0;
  public string Summary=>string.Join(" · ",values.OrderBy(v=>(int)v.Key).Select(v=>Name(v.Key)+v.Value));
  public string Brief=>string.Join(" · ",values.OrderBy(v=>(int)v.Key).Take(6).Select(v=>Name(v.Key)+v.Value))+(values.Count>6?" …":"");
  public void Add(CardEffect effect,int amount,EffectLifetime? lifetime=null){if(amount<=0)return;lifetimes[effect]=lifetime??(Temporary.Contains(effect)?EffectLifetime.Temporary:EffectLifetime.Persistent);this[effect]=effect==CardEffect.Reciprocity||effect==CardEffect.Disorder?1:effect==CardEffect.DamageCap&&this[effect]>0?Math.Min(this[effect],amount):this[effect]+amount;}
  public void Clear(){values.Clear();lifetimes.Clear();}
  public void CopyFrom(CombatStatuses source){Clear();foreach(var entry in source.values)values.Add(entry.Key,entry.Value);foreach(var entry in source.lifetimes)lifetimes.Add(entry.Key,entry.Value);}
  public void EndRound(){foreach(var e in new[]{CardEffect.Weaken,CardEffect.Fortify,CardEffect.IronWall})this[e]--;foreach(var e in Temporary)if(!lifetimes.TryGetValue(e,out var kind)||kind!=EffectLifetime.Persistent)this[e]=0;foreach(var e in lifetimes.Where(p=>p.Value!=EffectLifetime.Persistent).Select(p=>p.Key).ToArray())this[e]=0;}
  private static readonly CardEffect[] Temporary={CardEffect.Strength,CardEffect.Vulnerable,CardEffect.RandomNumbers,CardEffect.Chant,CardEffect.Reciprocity,CardEffect.Ignite,CardEffect.Envenom,CardEffect.Disorder,CardEffect.Mutation,CardEffect.AddNumber,CardEffect.SubtractNumber};
  public static bool IsDirect(CardEffect effect)=>effect==CardEffect.Strike||effect==CardEffect.Guard||effect==CardEffect.Mend||effect==CardEffect.IncreasedStrike||effect==CardEffect.InverseStrike;
  public static string Name(CardEffect effect){switch(effect){
   case CardEffect.Strike:return "伤害";case CardEffect.Guard:return "防御";case CardEffect.Mend:return "恢复";case CardEffect.IncreasedStrike:return "增值伤害";case CardEffect.InverseStrike:return "减值伤害";
   case CardEffect.Strength:return "力量";case CardEffect.Vulnerable:return "易伤";case CardEffect.Weaken:return "弱化";case CardEffect.Regeneration:return "再生";case CardEffect.Fortify:return "坚固";case CardEffect.Poison:return "中毒";case CardEffect.Bleed:return "流血";case CardEffect.IronWall:return "铁壁";case CardEffect.Parry:return "招架";case CardEffect.Barrier:return "壁垒";case CardEffect.DamageCap:return "限伤";case CardEffect.RandomNumbers:return "随机";case CardEffect.Chant:return "吟唱";case CardEffect.Reciprocity:return "往复";case CardEffect.Burn:return "烧伤";case CardEffect.Ignite:return "点燃";case CardEffect.Envenom:return "淬毒";case CardEffect.Disorder:return "无序";case CardEffect.Mutation:return "变动";case CardEffect.AddNumber:return "加值";case CardEffect.SubtractNumber:return "减值";default:return effect.ToString();
  }}
 }
}
