using UnityEngine;
using System.Linq;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/敌人定义")]
 public sealed class EnemyDefinition : ScriptableObject {
  [Min(1)] public int maxHealth=36,bossHealth=110;
  [Min(0)] public int healthGrowth=12,attack=5,shield=6,bossAttack=9;
  [Range(1,3)] public int actionCount=2,bossActionCount=3;
  [Range(0,2)] public int shieldSlot=1;
  [Tooltip("每隔几回合在指定行动槽施加护盾；0 禁用")][Min(0)] public int shieldEveryRounds=2;
  [Tooltip("每个行动槽的数字，0 为随机")] public int[] actionNumbers;
  public SequenceDefinition sequence;
  [Tooltip("基础技能可重复，特殊技能首次达成条件时额外加入，其后每回合至多随机抽到一种特殊技能")] public EnemySkill[] skills;
  [System.Serializable] public sealed class ConditionalRuleDefinition {public ActivationCondition condition;public int threshold=3;public SequenceDefinition sequence;}
  public ConditionalRuleDefinition[] conditionalSequences;
  public EnemyCardTable cardTable;
  [Header("BOSS策划表（新运行时接入入口）")]
  [Tooltip("运行时BOSS来源。按出没层数抽取；填写bossId可指定一种BOSS。")]
  public BossContentTable bossTable;
  public string bossId;
  public BossContentRow CreateBossContentSnapshot()=>bossTable==null?null:bossTable.GetConfiguredSnapshot(bossId);
  [Header("普通敌人策划表（新运行时接入入口）")]
  [Tooltip("运行时普通敌人来源。留空contentEnemyId时随机抽取启用且已配置的敌人。")]
  public NormalEnemyTable contentTable;
  public string contentEnemyId;
  public NormalEnemyRow CreateContentSnapshot()=>contentTable==null?null:contentTable.GetSnapshot(contentEnemyId);
  public NormalEnemyRow CreateConfiguredContentSnapshot()=>contentTable==null?null:contentTable.GetConfiguredSnapshot(contentEnemyId);
  // Presentation looks up the current encounter ID, so saves never embed Unity asset references.
  public Sprite GetPortrait(string enemyId,bool isBoss){
   if(string.IsNullOrWhiteSpace(enemyId))return null;
   return isBoss?bossTable?.rows?.FirstOrDefault(r=>r?.content!=null&&r.content.id==enemyId)?.content.portrait:contentTable?.rows?.FirstOrDefault(r=>r!=null&&r.id==enemyId)?.portrait;
  }
  public void Apply(GameSetup setup){
   setup.EnemySkills.Clear();
   float baseWeight=skills==null?10:skills.Where(b=>b!=null&&!b.Special).Select(b=>Mathf.Max(.001f,b.Weight)).DefaultIfEmpty(10).Average();
   if(skills!=null)foreach(var skill in skills)if(skill!=null){
    float ratio=Mathf.Clamp(skill.SpecialWeightRatio,.3f,.7f);
    setup.EnemySkills.Add(new EnemySkill {Name=skill.Name,Number=Mathf.Clamp(skill.Number,1,6),Effect=skill.Effect,Modifier=skill.Modifier,Spec=skill.Spec?.Copy(),Specs=(skill.Specs??System.Array.Empty<CardEffectSpec>()).Select(s=>s.Copy()).ToArray(),
     Weight=skill.Special?baseWeight*ratio:Mathf.Max(.001f,skill.Weight),SpecialWeightRatio=ratio,Special=skill.Special,Condition=skill.Condition,Threshold=skill.Threshold});
   }
   setup.EnemyConditionalRules.Clear();
   if(conditionalSequences!=null)foreach(var rule in conditionalSequences)if(rule!=null&&rule.sequence!=null)
    setup.EnemyConditionalRules.Add(new ConditionalSequence {Condition=rule.condition,Threshold=rule.threshold,Rule=rule.sequence.CreateRule()});
   setup.EnemyHealth=maxHealth;setup.BossHealth=bossHealth;setup.EnemyHealthGrowth=healthGrowth;
   setup.EnemyAttack=attack;setup.EnemyGuard=shield;setup.BossAttack=bossAttack;setup.EnemyActions=actionCount;setup.BossActions=bossActionCount;
   setup.GuardSlot=shieldSlot;setup.GuardInterval=shieldEveryRounds;setup.EnemyNumbers=actionNumbers==null?null:(int[])actionNumbers.Clone();
   if(sequence!=null)setup.EnemyRule=sequence.CreateRule();
   cardTable?.Apply(setup);
   setup.NormalEncounters.Clear();setup.BossEncounters.Clear();
   if(contentTable!=null){contentTable.ValidateConfiguredTable();foreach(var row in contentTable.rows.Where(r=>r.enabled&&(string.IsNullOrWhiteSpace(contentEnemyId)||r.id==contentEnemyId)))setup.NormalEncounters.Add(EnemyEncounterAdapter.Normal(row));if(setup.NormalEncounters.Count==0)throw new System.InvalidOperationException("普通敌人表中没有符合所选ID的启用敌人。");}
   if(bossTable!=null){bossTable.ValidateTable(true);foreach(var row in bossTable.rows.Where(r=>r.content.enabled&&(string.IsNullOrWhiteSpace(bossId)||r.content.id==bossId)))setup.BossEncounters.Add(EnemyEncounterAdapter.Boss(row));if(setup.BossEncounters.Count==0)throw new System.InvalidOperationException("BOSS表中没有符合所选ID的启用BOSS。");}
  }
 }
}
