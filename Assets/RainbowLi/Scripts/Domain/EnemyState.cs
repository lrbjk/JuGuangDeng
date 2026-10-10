using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi {
 public enum ActivationCondition { RoundAtLeast, HealthPercentAtMost }
 [Serializable] public sealed class EnemySkill {
  public string Name="攻击";public int Number=1;public CardEffect Effect;public int Modifier;public float Weight=10;public CardEffectSpec Spec;
  public bool Special;public float SpecialWeightRatio=.5f;public ActivationCondition Condition;public int Threshold=3;
 }
 [Serializable] public sealed class ConditionalSequence {
  public ActivationCondition Condition;public int Threshold=3;public SequenceRule Rule;
 }
 public sealed class EnemyState : CombatantState {
  public bool Boss,Elite;public int EliteAttackBonus;public SequenceRule Rule;
  public readonly List<ConditionalSequence> ConditionalRules=new List<ConditionalSequence>();
  private readonly HashSet<int> unlocked=new HashSet<int>();
  private readonly HashSet<int> activeRules=new HashSet<int>();
  public IEnumerable<SequenceRule> ActiveRules {get {if(Rule!=null)yield return Rule;for(int i=0;i<ConditionalRules.Count;i++)if(activeRules.Contains(i))yield return ConditionalRules[i].Rule;}}
  public bool Meets(ActivationCondition condition,int threshold,int round)=>condition==ActivationCondition.RoundAtLeast?round>=threshold:(long)Health*100<=(long)MaxHealth*threshold;
  public int[] UnlockedIndices=>unlocked.ToArray();public int[] ActiveRuleIndices=>activeRules.ToArray();
  public void RestoreActivation(int[] skills,int[] rules){unlocked.Clear();activeRules.Clear();unlocked.UnionWith(skills??Array.Empty<int>());activeRules.UnionWith(rules??Array.Empty<int>());}
  public void CopyActivationFrom(EnemyState source){unlocked.UnionWith(source.unlocked);activeRules.UnionWith(source.activeRules);}
  public void StartEncounter(int maxHealth,bool boss,bool elite=false){MaxHealth=Health=maxHealth;Boss=boss;Elite=elite;EliteAttackBonus=0;unlocked.Clear();activeRules.Clear();ConditionalRules.Clear();Statuses.Clear();ResetRound();}
  private BattleAction Action(EnemySkill skill,GameSetup setup,Random random){
   int attack=setup==null?(Boss?9:5):(Boss?setup.BossAttack:setup.EnemyAttack);
   var spec=skill.Spec;var card=spec==null?null:new Card {Number=skill.Number,Effect=skill.Effect,Spec=spec.Copy(),EffectAmount=random.Next(spec.Min,spec.Max+1),UpgradeBonus=0};
   return new BattleAction {Enemy=true,Number=skill.Number,Effect=skill.Effect,EffectSpec=card?.Spec,Value=card==null?Math.Max(0,attack+skill.Modifier+(Elite?EliteAttackBonus:0)):card.Value,SkillName=skill.Name,Special=skill.Special};
  }
  public List<BattleAction> ActivateSpecials(int round,GameSetup setup,Random random=null){
   for(int i=0;i<ConditionalRules.Count;i++)if(Meets(ConditionalRules[i].Condition,ConditionalRules[i].Threshold,round))activeRules.Add(i);
   var result=new List<BattleAction>();if(setup==null)return result;
   for(int i=0;i<setup.EnemySkills.Count;i++){var s=setup.EnemySkills[i];if(s.Special&&!unlocked.Contains(i)&&Meets(s.Condition,s.Threshold,round)){unlocked.Add(i);result.Add(Action(s,setup,random??new Random(round)));}}
   return result;
  }
  public IEnumerable<BattleAction> GenerateActions(Random random,int round,GameSetup setup){
   // Newly unlocked specials are extra actions; only previously unlocked specials enter this round's random pool.
   var prior=new HashSet<int>(unlocked);var mandatory=ActivateSpecials(round,setup,random);
   int count=setup==null?(Boss?3:2):(Boss?setup.BossActions:setup.EnemyActions);
   if(setup!=null&&setup.EnemySkills.Any(s=>!s.Special)){
    var weights=setup.EnemySkills.Select(s=>(double)Math.Max(.001f,s.Weight)).ToArray();bool specialDrawn=mandatory.Count>0;
    for(int slot=0;slot<count;slot++){
     var candidates=Enumerable.Range(0,weights.Length).Where(i=>!setup.EnemySkills[i].Special||prior.Contains(i)&&!specialDrawn).ToArray();
     double pick=random.NextDouble()*candidates.Sum(i=>weights[i]);int chosen=candidates[candidates.Length-1];foreach(int i in candidates){pick-=weights[i];if(pick<0){chosen=i;break;}}
     var skill=setup.EnemySkills[chosen];if(skill.Special)specialDrawn=true;weights[chosen]*=.5;yield return Action(skill,setup,random);
    }
   }else{
    int guardSlot=setup==null?1:setup.GuardSlot,interval=setup==null?2:setup.GuardInterval;
    for(int i=0;i<count;i++){bool guard=i==guardSlot&&interval>0&&round%interval==0;int number=setup!=null&&setup.EnemyNumbers!=null&&i<setup.EnemyNumbers.Length?setup.EnemyNumbers[i]:0;
     yield return new BattleAction {Enemy=true,Number=number>=1&&number<=6?number:random.Next(1,7),Effect=guard?CardEffect.Guard:CardEffect.Strike,Value=guard?(setup==null?6:setup.EnemyGuard):(setup==null?(Boss?9:5):(Boss?setup.BossAttack:setup.EnemyAttack))+round/3+(Elite?EliteAttackBonus:0)};}
   }
   foreach(var a in mandatory)yield return a;
  }
 }
}
