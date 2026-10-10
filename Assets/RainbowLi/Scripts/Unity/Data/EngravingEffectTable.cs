using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/配表/铭刻效果表")]
 public sealed class EngravingEffectTable : EffectTableDefinition {
  public override bool IsSequence=>false;
  public override bool IsRuntimeSupported(EffectTableRow row)=>row.behavior==nameof(EngravingBehavior.FollowUpDamage);
  public int ResolveDamage(string id,int x){var r=Find(id);RequireSupported(r);return r.ResolveX(x);}
 }
}
