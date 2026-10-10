using System;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/配表/铭刻效果表")]
 public sealed class EngravingEffectTable : EffectTableDefinition {
  public override bool IsSequence=>false;
  public override bool IsRuntimeSupported(EffectTableRow row)=>row!=null&&Enum.TryParse<EngravingEffect>(row.behavior,out _);
  public RuntimeEngraving CreateEffect(string id,int x){var r=Find(id);RequireSupported(r);var entry=RuntimeTableAdapter.Engraving(r);r.ResolveX(x==0?entry.Min:x);return new RuntimeEngraving {Code=r.id,Effect=(EngravingEffect)Enum.Parse(typeof(EngravingEffect),r.behavior),BaseX=x==0?entry.Min:x,MinX=entry.Min,MaxX=entry.Max,RollX=x==0&&r.TryRange(out _,out _),Multiplier=r.multiplier,FixedValue=r.fixedValue};}
  public int ResolveDamage(string id,int x){var e=CreateEffect(id,x);if(e.Effect!=EngravingEffect.FollowUpDamage)throw new InvalidOperationException("Use CreateEffect for non-damage engravings");return e.Value;}
 }
}
