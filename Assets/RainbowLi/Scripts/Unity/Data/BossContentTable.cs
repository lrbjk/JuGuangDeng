using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 public enum BossTrigger { 无,血量首次达到阈值,使用过指定技能,牌序触发次数超过,固定回合间隔,阶段内固定回合间隔 }
 public enum BossTiming { 回合开始,本回合首次自身行动后,每次自身行动后,卡牌出现时 }
 public enum BossSequenceMode { 随机排列,必须包含指定数字,固定排列 }
 [Serializable] public sealed class BossSkillRule {
  public string slot;
  public BossTrigger trigger;
  public int thresholdPercent,requiredStage,roundInterval,sequenceCountThreshold;
  public string requiredSkill;
  public bool fixedTrigger,priorityOnAppearance,lockInPhase;
 }
 [Serializable] public sealed class BossPassive {
  public BossTiming timing;
  public EnemyAuthoredEffect[] effects=Array.Empty<EnemyAuthoredEffect>();
 }
 [Serializable] public sealed class BossPhase {
  public string label,description;
  public int index,thresholdPercent;
  public BossPassive[] passives=Array.Empty<BossPassive>();
 }
 [Serializable] public sealed class BossSequenceRule {
  public string slot;
  public BossSequenceMode mode;
  public int requiredNumber;
  public int[] fixedNumbers=Array.Empty<int>();
 }
 [Serializable] public sealed class BossContentRow {
  public NormalEnemyRow content=new NormalEnemyRow();
  public string spawnLayers="1",thresholdText;
  public BossPhase[] phases=Array.Empty<BossPhase>();
  public BossSkillRule[] skillRules=Array.Empty<BossSkillRule>();
  public BossSequenceRule[] sequenceRules=Array.Empty<BossSequenceRule>();
  public BossContentRow Copy()=>JsonUtility.FromJson<BossContentRow>(JsonUtility.ToJson(this));
 }
 [CreateAssetMenu(menuName="RainbowLi/配表/BOSS内容表")]
 public sealed class BossContentTable:ScriptableObject {
  public BossContentRow[] rows=Array.Empty<BossContentRow>();
  public BossContentRow GetConfiguredSnapshot(string id){ValidateTable(true);var r=rows.SingleOrDefault(p=>p.content.enabled&&p.content.id==id);if(r==null)throw new ArgumentException("未找到启用BOSS："+id);return r.Copy();}
  public BossContentRow[] GetLayerSnapshots(int layer){ValidateTable(true);return rows.Where(r=>r.content.enabled&&layer>=EnemyNumberRange.Parse(r.spawnLayers,1).Min&&layer<=EnemyNumberRange.Parse(r.spawnLayers,1).Max).Select(r=>r.Copy()).ToArray();}
  public void ValidateTable(bool configured=false){var issues=Check(rows,configured);if(issues.Length>0)throw new InvalidOperationException(string.Join("\n",issues));}
  public static string[] Check(BossContentRow[] rows,bool configured=false){
   if(rows==null||rows.Length==0)return new[]{"至少配置一个BOSS。"};if(rows.Any(r=>r?.content==null))return new[]{"BOSS内容为空。"};
   var issues=new List<string>(NormalEnemyTable.Check(rows.Select(r=>r.content).ToArray()));
   if(issues.Count>0)return issues.ToArray();
   foreach(var r in rows){var c=r.content;string prefix=c.title+"：";
    try{_ = EnemyNumberRange.Parse(r.spawnLayers,1);}catch(Exception e){issues.Add(prefix+"出没层数："+e.Message);}
    if(!configured||!c.enabled)continue;
    if(!c.configured)issues.Add(prefix+"数值配置尚未完成。");
    if(r.phases==null||r.phases.Length==0||r.phases[0]?.index!=0)issues.Add(prefix+"必须有初始阶段。");
    int previous=101;
    foreach(var phase in r.phases??Array.Empty<BossPhase>()){
     if(phase==null){issues.Add(prefix+"阶段为空。");continue;}
     if(phase.index>0&&(phase.thresholdPercent<=0||phase.thresholdPercent>=previous))issues.Add(prefix+"阶段血量阈值应递减且介于1–100。");
     if(phase.index>0)previous=phase.thresholdPercent;
     foreach(var passive in phase.passives??Array.Empty<BossPassive>())if(passive==null)issues.Add(prefix+"被动为空。");else EnemyConfiguredContent.CheckEffects(passive.effects,prefix+phase.label,issues);
    }
    if((r.phases??Array.Empty<BossPhase>()).Where(p=>p!=null).Select(p=>p.index).Distinct().Count()!=(r.phases?.Length??0))issues.Add(prefix+"阶段编号重复。");
    foreach(var s in c.skills){EnemyConfiguredContent.CheckEffects(s.effects,prefix+s.name,issues);var rules=(r.skillRules??Array.Empty<BossSkillRule>()).Where(t=>t!=null&&t.slot==s.slot).ToArray();if(rules.Length!=1){issues.Add(prefix+s.name+"需一个对应技能规则。");continue;}var rule=rules[0];
     if(s.special&&rule.trigger==BossTrigger.无)issues.Add(prefix+s.name+"缺少触发规则。");
     if(rule.trigger==BossTrigger.使用过指定技能&&!c.skills.Any(p=>p.name==rule.requiredSkill&&p.slot!=s.slot))issues.Add(prefix+s.name+"前置技能无效。");
     if(rule.trigger==BossTrigger.血量首次达到阈值&&(rule.thresholdPercent<=0||rule.thresholdPercent>100))issues.Add(prefix+s.name+"血量阈值无效。");
     if(rule.trigger==BossTrigger.阶段内固定回合间隔&&!(r.phases??Array.Empty<BossPhase>()).Any(p=>p!=null&&p.index==rule.requiredStage))issues.Add(prefix+s.name+"所需阶段不存在。");
     if((rule.trigger==BossTrigger.固定回合间隔||rule.trigger==BossTrigger.阶段内固定回合间隔)&&(!rule.fixedTrigger||rule.roundInterval<1))issues.Add(prefix+s.name+"固定回合配置无效。");
     if(rule.trigger==BossTrigger.牌序触发次数超过&&rule.sequenceCountThreshold<0)issues.Add(prefix+s.name+"牌序次数无效。");
    }
    foreach(var s in c.sequences){EnemyConfiguredContent.CheckEffects(s.effects,prefix+s.name,issues);var rules=(r.sequenceRules??Array.Empty<BossSequenceRule>()).Where(t=>t!=null&&t.slot==s.slot).ToArray();if(rules.Length!=1){issues.Add(prefix+s.name+"需一个牌序排列规则。");continue;}var rule=rules[0];if(rule.mode==BossSequenceMode.必须包含指定数字&&(rule.requiredNumber<1||rule.requiredNumber>6))issues.Add(prefix+s.name+"必须数字无效。");if(rule.mode==BossSequenceMode.固定排列&&(rule.fixedNumbers==null||rule.fixedNumbers.Length!=3||rule.fixedNumbers.Any(n=>n<1||n>6)))issues.Add(prefix+s.name+"固定排列应有3个1–6的数字。");}
    foreach(var reward in c.rewards)if(reward.kind==EnemyContentReward.未配置||reward.kind==EnemyContentReward.卡牌&&reward.choiceCount<1)issues.Add(prefix+reward.name+"奖励未配置。");
   }return issues.ToArray();
  }
 }
}
