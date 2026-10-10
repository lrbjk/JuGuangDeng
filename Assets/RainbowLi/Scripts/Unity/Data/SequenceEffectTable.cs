using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/配表/牌序效果表")]
 public sealed class SequenceEffectTable : EffectTableDefinition {
  public override bool IsSequence=>true;
  public override bool IsRuntimeSupported(EffectTableRow row)=>row.behavior==nameof(SequenceBehavior.Damage)||row.behavior==nameof(SequenceBehavior.Shield)||row.behavior==nameof(SequenceBehavior.Boost)||row.behavior==nameof(SequenceBehavior.InsertNumber);
  public SequenceEffect CreateEffect(string id,int x,PatternRequirement requirement=PatternRequirement.Any){
   var r=Find(id);RequireSupported(r);
   return new SequenceEffect {Effect=r.behavior==nameof(SequenceBehavior.Damage)?RuleEffect.Damage:r.behavior==nameof(SequenceBehavior.Shield)?RuleEffect.Shield:r.behavior==nameof(SequenceBehavior.Boost)?RuleEffect.Power:RuleEffect.Insert,Value=r.ResolveX(x),Requirement=requirement};
  }
 }
}
