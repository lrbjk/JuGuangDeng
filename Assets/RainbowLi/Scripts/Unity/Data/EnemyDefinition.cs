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
  public void Apply(GameSetup setup){
   setup.EnemySkills.Clear();
   float baseWeight=skills==null?10:skills.Where(b=>b!=null&&!b.Special).Select(b=>Mathf.Max(.001f,b.Weight)).DefaultIfEmpty(10).Average();
   if(skills!=null)foreach(var skill in skills)if(skill!=null){
    float ratio=Mathf.Clamp(skill.SpecialWeightRatio,.3f,.7f);
    setup.EnemySkills.Add(new EnemySkill {Name=skill.Name,Number=Mathf.Clamp(skill.Number,1,6),Effect=skill.Effect,Modifier=skill.Modifier,
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
  }
 }
}
