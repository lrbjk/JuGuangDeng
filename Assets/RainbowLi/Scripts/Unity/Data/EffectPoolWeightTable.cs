using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 public enum EffectPoolKind {Card,Sequence}
 [Serializable] public sealed class EffectPoolWeightRow {
  public string id,name;
  public float weight;
  public EffectPoolWeightRow Copy()=>(EffectPoolWeightRow)MemberwiseClone();
 }
 [CreateAssetMenu(menuName="RainbowLi/配表/效果池权重表")]
 public sealed class EffectPoolWeightTable:ScriptableObject {
  public EffectPoolKind kind;
  public EffectPoolWeightRow[] rows=Array.Empty<EffectPoolWeightRow>();
  public EffectPoolWeightRow[] GetSnapshot(EffectPoolKind expected){if(kind!=expected)throw new InvalidOperationException("绑定了错误类型的效果池权重表。");ValidateTable();return rows.Select(r=>r.Copy()).ToArray();}
  public double TotalWeight=>rows==null?0:rows.Where(r=>r!=null).Sum(r=>(double)r.weight);
  public void ValidateTable(){var issues=Check(rows).Concat(CheckKind(rows,kind)).ToArray();if(issues.Length>0)throw new InvalidOperationException(string.Join("\n",issues));}
  public static string[] CheckKind(EffectPoolWeightRow[] rows,EffectPoolKind kind){var wrong=kind==EffectPoolKind.Card?"卡序池_":"卡效池_";return (rows??Array.Empty<EffectPoolWeightRow>()).Where(r=>r?.id!=null&&r.id.StartsWith(wrong,StringComparison.Ordinal)).Select(r=>"编号"+r.id+"与权重表类型不符。").ToArray();}
  public static string[] Check(EffectPoolWeightRow[] rows){
   if(rows==null||rows.Length==0)return new[]{"效果池权重表至少需要一行。"};var errors=new List<string>();var ids=new HashSet<string>();var names=new HashSet<string>();
   for(int i=0;i<rows.Length;i++){var r=rows[i];string prefix="第"+(i+2)+"行：";if(r==null){errors.Add(prefix+"行为空。");continue;}
    if(string.IsNullOrWhiteSpace(r.id)||!ids.Add(r.id))errors.Add(prefix+"编号为空或重复。");if(string.IsNullOrWhiteSpace(r.name)||!names.Add(r.name))errors.Add(prefix+"名称为空或重复。");if(float.IsNaN(r.weight)||float.IsInfinity(r.weight)||r.weight<0)errors.Add(prefix+"权重须为有限非负数。");
   }
   if(!rows.Any(r=>r!=null&&r.weight>0&&!float.IsNaN(r.weight)&&!float.IsInfinity(r.weight)))errors.Add("至少一个效果池的权重须大于0。");return errors.ToArray();
  }
  public static string RuntimePoolName(string displayName){switch(displayName){case "默认卡池":return "默认池";case "伤害卡池":return "伤害池";case "增益效果池":return "增益池";case "减益效果池":return "减益池";default:return displayName;}}
 }
}
