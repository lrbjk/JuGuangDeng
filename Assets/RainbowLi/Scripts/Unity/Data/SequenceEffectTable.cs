using System;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/配表/牌序效果表")]
 public sealed class SequenceEffectTable : EffectTableDefinition {
  public override bool IsSequence=>true;
  public override bool IsRuntimeSupported(EffectTableRow row)=>row!=null&&Enum.TryParse<SequenceBehavior>(row.behavior,out _);
  public SequenceEffect CreateEffect(string id,int x,PatternRequirement requirement=PatternRequirement.Any){
   var r=Find(id);RequireSupported(r);var e=RuntimeTableAdapter.Sequence(r);
   return new SequenceEffect {Code=r.id,ShieldBonusPercent=r.shieldBonusPercent,Effect=e.Sequence,Value=r.ResolveX(x==0?e.Min:x),BaseX=x==0?e.Min:x,MinX=e.Min,MaxX=e.Max,RollX=x==0&&r.TryRange(out _,out _),Multiplier=r.multiplier,FixedValue=r.fixedValue,Requirement=requirement};
  }
 }
}
