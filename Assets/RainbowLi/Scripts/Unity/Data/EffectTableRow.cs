using System;
using System.Globalization;
using System.Text.RegularExpressions;
namespace RainbowLi.UnityGame {
 public enum EngravingBehavior { Poison,Burn,FollowUpDamage,Growth,ExtraNumber,Wildcard,SequenceTailBonus,SequenceHeadBonus,BlockSequence,Bridge,Inherit,RandomizeFollowing,Roulette,OnlyPlayerCard,LastActionBonus,FirstActionBonus,Charge,PoisonExplosion,BurnExplosion }
 public enum SequenceBehavior { Damage,InsertNumber,Shield,Heal,Boost,Burn,Poison,Strength,Bleed,RandomNumbers,LastStand,RandomCardEffects,Vulnerable,Parry,RepeatSequenceCards }
 [Serializable] public sealed class EffectTableRow {
  public string id,name,description,range,pools,notes,behavior;
  public bool enabled=true;
  public float weight=10;
  public int multiplier=1,fixedValue;
  public EffectTableRow Copy()=>(EffectTableRow)MemberwiseClone();
  public bool TryRange(out int min,out int max){
   min=max=0;if(string.IsNullOrWhiteSpace(range))return false;
   var text=range.Trim().Replace('－','-').Replace('–','-').Replace('—','-').Replace('～','-').Replace('~','-');
   var match=Regex.Match(text,@"^(\d+)\s*(?:-\s*(\d+))?$");
   if(!match.Success||!int.TryParse(match.Groups[1].Value,NumberStyles.None,CultureInfo.InvariantCulture,out min))return false;
   max=min;if(match.Groups[2].Success&&!int.TryParse(match.Groups[2].Value,NumberStyles.None,CultureInfo.InvariantCulture,out max))return false;
   return min>=0&&max>=min;
  }
  public int ResolveX(int x){if(!TryRange(out int min,out int max))return fixedValue;if(x<min||x>max)throw new InvalidOperationException(id+" 的 X 必须在 "+range+" 内。");return checked(x*multiplier);}
 }
}
