using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [Serializable] public sealed class EnemySkillContent {
  public string slot,name,description,numberRange="1-6",conditionLabel,condition,notes;
  public float weight=80;
  public bool special;
  public EnemyContentTrigger trigger;
  public string requiredSkill;
  public EnemyAuthoredEffect[] effects=Array.Empty<EnemyAuthoredEffect>();
 }
 [Serializable] public sealed class EnemySequenceContent {
  public string slot,name,description,numbers="随机";
  public EnemyAuthoredEffect[] effects=Array.Empty<EnemyAuthoredEffect>();
 }
 [Serializable] public sealed class EnemyRewardContent {
  public string slot,name,amount="1",notes;
  public float probability=100;
  public EnemyContentReward kind;
  public int choiceCount;
  public bool canSkip;
 }
 public readonly struct EnemyNumberRange {
  public readonly int Min,Max;
  public EnemyNumberRange(int min,int max){Min=min;Max=max;}
  public static EnemyNumberRange Parse(string text,int minimum=0){
   var parts=(text??"").Trim().Replace('～','-').Replace('~','-').Replace('–','-').Split('-');
   if(parts.Length<1||parts.Length>2||!int.TryParse(parts[0],NumberStyles.None,CultureInfo.InvariantCulture,out int min)||!int.TryParse(parts[parts.Length-1],NumberStyles.None,CultureInfo.InvariantCulture,out int max)||min<minimum||max<min||max==int.MaxValue)
    throw new FormatException("范围须为整数或最小值-最大值，且下限至少为"+minimum+"："+text);
   return new EnemyNumberRange(min,max);
  }
 }
 [Serializable] public sealed class NormalEnemyRow {
  public string id,sheetName,title;
  [Tooltip("该敌人的战斗立绘。拖入 Sprite；不配置时显示占位。此图片绑定在 Unity 中维护，Excel 导入保留现有绑定。")]
  public Sprite portrait;
  public bool enabled=true;
  public bool configured;
  public int health=50,healthThresholdPercent=50,skillCount=4,sequenceCount=3;
  public string actions="2-3",attack="4-7",initialEffects="无";
  public EnemySkillContent[] skills=Array.Empty<EnemySkillContent>();
  public EnemySequenceContent[] sequences=Array.Empty<EnemySequenceContent>();
  public EnemyRewardContent[] rewards=Array.Empty<EnemyRewardContent>();
  public EnemyNumberRange ActionRange=>EnemyNumberRange.Parse(actions,1);
  public EnemyNumberRange AttackRange=>EnemyNumberRange.Parse(attack);
  public NormalEnemyRow Copy()=>JsonUtility.FromJson<NormalEnemyRow>(JsonUtility.ToJson(this));
 }
 [CreateAssetMenu(menuName="RainbowLi/配表/普通敌人内容表")]
 public sealed class NormalEnemyTable : ScriptableObject {
  public NormalEnemyRow[] rows=Array.Empty<NormalEnemyRow>();
  public NormalEnemyRow GetSnapshot(string id){
   ValidateTable();var row=rows.SingleOrDefault(r=>r.id==id&&r.enabled);
   if(row==null)throw new ArgumentException("未找到启用的普通敌人："+id);
   return row.Copy();
  }
  public NormalEnemyRow[] GetEnabledSnapshots(){ValidateTable();return rows.Where(r=>r.enabled).Select(r=>r.Copy()).ToArray();}
  public NormalEnemyRow GetConfiguredSnapshot(string id){var r=GetSnapshot(id);var issues=EnemyConfiguredContent.Check(r);if(issues.Length>0)throw new InvalidOperationException(string.Join("\n",issues));return r;}
  public void ValidateConfiguredTable(){ValidateTable();var issues=rows.Where(r=>r.enabled).SelectMany(EnemyConfiguredContent.Check).ToArray();if(issues.Length>0)throw new InvalidOperationException(string.Join("\n",issues));}
  public void ValidateTable(){var issues=Check(rows);if(issues.Length>0)throw new InvalidOperationException(string.Join("\n",issues));}
  public static string[] Check(NormalEnemyRow[] rows){
   var errors=new List<string>();var ids=new HashSet<string>();var sheets=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   if(rows==null||rows.Length==0)return new[]{"至少配置一种敌人。"};
   foreach(var r in rows){
    if(r==null){errors.Add("敌人行为空。");continue;}
    string prefix=(r.sheetName??r.id)+"：";
    Action<bool,string> require=(ok,msg)=>{if(!ok)errors.Add(prefix+msg);};
    require(!string.IsNullOrWhiteSpace(r.id)&&ids.Add(r.id),"敌人ID为空或重复。");
    require(!string.IsNullOrWhiteSpace(r.sheetName)&&r.sheetName.Length<=31&&!r.sheetName.Any(c=>"[]:*?/\\".Contains(c))&&sheets.Add(r.sheetName),"工作表名称无效或重复。");
    require(!string.IsNullOrWhiteSpace(r.title),"缺少敌人标题。");require(r.health>0,"血量必须大于0。");
    require(r.healthThresholdPercent>=0&&r.healthThresholdPercent<=100,"血量阈值须为0–100%。");
    try{var range=r.ActionRange;require(range.Max<=100,"回合技能数过大。");}catch(Exception e){errors.Add(prefix+"回合技能使用数："+e.Message);}
    try{_ = r.AttackRange;}catch(Exception e){errors.Add(prefix+"基础攻击值："+e.Message);}
    require(r.skills!=null&&r.skills.Length==r.skillCount&&r.skillCount>0,"技能总数与技能组不一致。");
    require(r.sequences!=null&&r.sequences.Length==r.sequenceCount&&r.sequenceCount>0,"牌序效果数与牌序列表不一致。");
    require(r.skills!=null&&r.skills.Any(s=>s!=null&&!s.special&&s.weight>0),"至少需要一种有正权重的基础技能。");
    var names=new HashSet<string>();
    foreach(var s in r.skills??Array.Empty<EnemySkillContent>()){
     if(s==null){errors.Add(prefix+"技能为空。");continue;}
     require(!string.IsNullOrWhiteSpace(s.name)&&names.Add(s.name),"技能名称为空或重复。");require(!string.IsNullOrWhiteSpace(s.description),s.name+"缺少技能效果。");
     require(!float.IsNaN(s.weight)&&!float.IsInfinity(s.weight)&&s.weight>=0,s.name+"权重必须为有限非负数。");
     try{_ = EnemyNumberRange.Parse(s.numberRange,1);}catch(Exception e){errors.Add(prefix+s.name+"数字范围："+e.Message);}
     require(!s.special||!string.IsNullOrWhiteSpace(s.condition),s.name+"缺少特殊技能触发条件。");
    }
    foreach(var s in r.sequences??Array.Empty<EnemySequenceContent>())require(s!=null&&!string.IsNullOrWhiteSpace(s.name)&&!string.IsNullOrWhiteSpace(s.description)&&!string.IsNullOrWhiteSpace(s.numbers),"牌序效果缺少名称、效果或数字排列。");
    foreach(var s in r.rewards??Array.Empty<EnemyRewardContent>()){
     if(s==null){errors.Add(prefix+"奖励为空。");continue;}
     require(!string.IsNullOrWhiteSpace(s.name),"奖励类型为空。");
     require(!float.IsNaN(s.probability)&&!float.IsInfinity(s.probability)&&s.probability>=0&&s.probability<=100,s.name+"出现概率须为0–100。");
     try{_ = EnemyNumberRange.Parse(s.amount);}catch(Exception e){errors.Add(prefix+s.name+"奖励数量："+e.Message);}
    }
   }return errors.ToArray();
  }
 }
}
