using System;
using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 public abstract class EffectTableDefinition : ScriptableObject {
  public EffectTableRow[] rows=Array.Empty<EffectTableRow>();
  public abstract bool IsSequence {get;}
  public abstract bool IsRuntimeSupported(EffectTableRow row);
  public void ValidateTable(){
   if(rows==null||rows.Length==0)throw new InvalidOperationException("配表至少需要一行。");
   var ids=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
   for(int i=0;i<rows.Length;i++){
    var r=rows[i];string at="第 "+(i+1)+" 行：";
    if(r==null||string.IsNullOrWhiteSpace(r.id)||!ids.Add(r.id))throw new InvalidOperationException(at+"效果编号为空或重复。");
    if(string.IsNullOrWhiteSpace(r.name)||string.IsNullOrWhiteSpace(r.description))throw new InvalidOperationException(at+"名称和具体效果不能为空。");
    bool hasX=r.name.Contains("X");if((hasX||!string.IsNullOrWhiteSpace(r.range))&&!r.TryRange(out _,out _))throw new InvalidOperationException(at+"范围须为 1 或 1-6，最小值不能大于最大值。");
    if(float.IsNaN(r.weight)||float.IsInfinity(r.weight)||r.weight<=0||r.multiplier<1||r.fixedValue<0)throw new InvalidOperationException(at+"权重须为有限正数，倍率须大于零，固定参数不能为负。");
    Type type=IsSequence?typeof(SequenceBehavior):typeof(EngravingBehavior);
    if(string.IsNullOrWhiteSpace(r.behavior)||!Enum.GetNames(type).Contains(r.behavior))throw new InvalidOperationException(at+"程序效果标识无效。");
    if(IsSequence&&string.IsNullOrWhiteSpace(r.pools))throw new InvalidOperationException(at+"卡效所属池不能为空。");
    if(r.behavior=="InsertNumber"||r.behavior=="ExtraNumber"){if(!r.TryRange(out int min,out int max)||min<1||(long)max*r.multiplier>6)throw new InvalidOperationException(at+"数字效果范围必须落在 1–6。");}
   }
  }
  public EffectTableRow Find(string id){ValidateTable();return rows.FirstOrDefault(r=>r.id==id)??throw new InvalidOperationException("找不到效果编号："+id);}
  public EffectTableRow[] GetPool(string pool)=>rows.Where(r=>r!=null&&r.enabled&&EffectPoolNames.Contains(r.pools,pool)).ToArray();
  public void RequireSupported(EffectTableRow row){if(!row.enabled)throw new InvalidOperationException(row.id+" 已禁用。");if(!IsRuntimeSupported(row))throw new InvalidOperationException(row.id+"（"+row.name+"）配表已就绪，核心程序尚未实现结算，不能绑定运行。");}
 }
}
