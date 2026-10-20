using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi {
 public sealed partial class ContainmentGame {
  private bool preparingContentRound;
  private EncounterDefinition ChooseEncounter(bool boss){
   var definitions=boss?setup?.BossEncounters:setup?.NormalEncounters;if(definitions==null||definitions.Count==0)return null;
   AdventureLayer=Route?.AdventureLayer??AdventureLayer;
   var candidates=definitions.Where(d=>!boss||AdventureLayer>=d.LayerMin&&AdventureLayer<=d.LayerMax).ToArray();
   if(candidates.Length==0)throw new InvalidOperationException("第 "+AdventureLayer+" 层没有配置可出现的BOSS。");
   return candidates[random.Next(candidates.Length)].Copy();
  }
  private void InitializeEncounter(EncounterDefinition definition){
   Enemy.Encounter=new EnemyEncounterState {Definition=definition,BaseAttack=random.Next(definition.AttackMin,definition.AttackMax+1)};
   foreach(var skill in definition.Skills)RollBattleNumbers(skill.Effects);
   foreach(var sequence in definition.Sequences){
    RollBattleNumbers(sequence.Effects);int[] numbers=sequence.FixedNumbers.Length==3?(int[])sequence.FixedNumbers.Clone():RandomPattern();
    if(sequence.RequiredNumber>0&&!numbers.Contains(sequence.RequiredNumber))numbers[random.Next(3)]=sequence.RequiredNumber;
    Enemy.EncounterRules.Add(new SequenceRule {Name=sequence.Name,Code=definition.Id+"/sequence/"+sequence.Slot,Numbers=numbers,EncounterDescription=sequence.Description,EncounterEffects=EncounterEffect.Copies(sequence.Effects)});
   }
   foreach(var phase in definition.Phases)foreach(var passive in phase.Passives)RollBattleNumbers(passive.Effects);
   EnemyRule=Enemy.EncounterRules.FirstOrDefault();
  }
  private void RollBattleNumbers(EncounterEffect[] effects){foreach(var effect in effects)if(effect.NumberMode==EncounterNumber.BattleFixed){effect.NumberMin=random.Next(effect.NumberMin,effect.NumberMax+1);effect.NumberMax=effect.NumberMin;}}
  private static CardEffect ContentIcon(EncounterOperation operation){
   switch(operation){case EncounterOperation.Shield:return CardEffect.Guard;case EncounterOperation.Heal:return CardEffect.Mend;case EncounterOperation.Strength:return CardEffect.Strength;case EncounterOperation.Burn:return CardEffect.Burn;case EncounterOperation.Poison:return CardEffect.Poison;case EncounterOperation.Bleed:return CardEffect.Bleed;case EncounterOperation.Fortify:return CardEffect.Fortify;case EncounterOperation.IronWall:return CardEffect.IronWall;case EncounterOperation.Parry:return CardEffect.Parry;case EncounterOperation.Weaken:return CardEffect.Weaken;case EncounterOperation.Regeneration:return CardEffect.Regeneration;case EncounterOperation.Vulnerable:return CardEffect.Vulnerable;case EncounterOperation.Invulnerable:return CardEffect.Invulnerable;case EncounterOperation.Damage:return CardEffect.Strike;default:return CardEffect.RandomNumbers;}
  }
  private BattleAction ContentAction(EncounterSkill skill,bool effectsOnly=false,bool passive=false){
   return new BattleAction {Enemy=true,Number=random.Next(skill.NumberMin,skill.NumberMax+1),Effect=ContentIcon(skill.Effects[0].Operation),Value=skill.Effects[0].Resolve(Enemy.Encounter.BaseAttack),SkillName=skill.Name,Special=skill.Special,
    EncounterSlot=skill.Slot,EncounterDescription=skill.Description,EncounterEffects=EncounterEffect.Copies(skill.Effects),EffectOnly=effectsOnly,IsRepeat=effectsOnly,PassiveAction=passive,
    LockedPosition=skill.LockInPhase&&Enemy.Encounter.PhaseIndex>0};
  }
  private void UpdateEncounterPhase(){
   var state=Enemy.Encounter;if(state==null||Enemy.Health<=0)return;
   state.MinHealthPercent=Math.Min(state.MinHealthPercent,(int)(((long)Enemy.Health*100+Math.Max(1,Enemy.MaxHealth)-1)/Math.Max(1,Enemy.MaxHealth)));
   var next=state.Definition.Phases.Where(p=>p.Index>state.PhaseIndex&&p.ThresholdPercent>=state.MinHealthPercent).OrderByDescending(p=>p.Index).FirstOrDefault();
   if(next==null)return;state.PhaseIndex=next.Index;state.PhaseStartRound=Math.Max(1,Round);Log("BOSS阶段切换："+next.Label);
   foreach(var action in Timeline){var skill=state.Definition.Skills.FirstOrDefault(s=>s.Slot==action.EncounterSlot);if(skill?.LockInPhase==true)action.LockedPosition=true;}
  }
  private bool ContentTriggerMet(EncounterSkill skill){
   var state=Enemy.Encounter;
   switch(skill.Trigger){
    case EncounterTrigger.HealthThreshold:return state.MinHealthPercent<=skill.ThresholdPercent;
    case EncounterTrigger.UsedSkill:return state.UsedSkills.Contains(skill.RequiredSkill);
    case EncounterTrigger.SequenceCount:return state.TotalSequenceTriggers>skill.SequenceCountThreshold;
    case EncounterTrigger.RoundInterval:return skill.RoundInterval>0&&Round%skill.RoundInterval==0;
    case EncounterTrigger.PhaseInterval:return state.PhaseIndex==skill.RequiredStage&&skill.RoundInterval>0&&(Round-state.PhaseStartRound+1)%skill.RoundInterval==0;
    default:return false;
   }
  }
  private List<BattleAction> ActivateContentSpecials(){
   var state=Enemy.Encounter;var result=new List<BattleAction>();UpdateEncounterPhase();
   foreach(var skill in state.Definition.Skills.Where(s=>s.Special)){
    if(!ContentTriggerMet(skill))continue;
    if(skill.FixedTrigger){string key=skill.Slot+"/"+Round;if(state.FixedTriggers.Contains(key))continue;state.FixedTriggers.Add(key);}
    else {if(state.Unlocked.Contains(skill.Slot))continue;state.Unlocked.Add(skill.Slot);}
    var action=ContentAction(skill);result.Add(action);
   }
   return result;
  }
  private IEnumerable<BattleAction> GenerateContentActions(){
   var state=Enemy.Encounter;var prior=new HashSet<string>(state.Unlocked);var mandatory=ActivateContentSpecials();
   int count=random.Next(state.Definition.ActionMin,state.Definition.ActionMax+1);bool specialDrawn=mandatory.Count>0;
   for(int i=0;i<count;i++){
    var candidates=state.Definition.Skills.Where(s=>s.Weight>0&&!s.FixedTrigger&&(!s.Special||prior.Contains(s.Slot)&&!specialDrawn)).ToArray();
    if(candidates.Length==0)throw new InvalidOperationException("敌人没有可抽取的基础技能："+state.Definition.Title);
    var skill=candidates[WeightedChoice(candidates.Select(s=>(double)s.Weight).ToArray())];if(skill.Special)specialDrawn=true;yield return ContentAction(skill);
   }
   foreach(var action in mandatory)yield return action;
  }
  private void QueueContentSpecials(){
   foreach(var action in ActivateContentSpecials()){pending.Add(action);Timeline.Add(action);Log("特殊技能触发："+action.SkillName);ApplyAppearance(action);}
  }
  private void ApplyAppearance(BattleAction action){
   var skill=Enemy.Encounter.Definition.Skills.FirstOrDefault(s=>s.Slot==action.EncounterSlot);
   if(skill?.PriorityOnAppearance!=true||action.AppearanceApplied)return;action.AppearanceApplied=true;
   var previous=settlingAction;settlingAction=action;ApplyEncounterEffects(action,action.EncounterEffects,BattleEventSource.EnemySkill);settlingAction=previous;
  }
  private void RunContentPassives(EncounterTiming timing,BattleAction source=null){
   var state=Enemy.Encounter;var phase=state.Definition.Phases.FirstOrDefault(p=>p.Index==state.PhaseIndex);if(phase==null)return;
   foreach(var passive in phase.Passives.Where(p=>p.Timing==timing)){
    var action=new BattleAction {Enemy=true,EffectOnly=true,IsRepeat=true,PassiveAction=true,EncounterEffects=EncounterEffect.Copies(passive.Effects),EncounterDescription=phase.Label,SkillName="被动 / "+phase.Label,SourceAction=source};
    var previous=settlingAction;settlingAction=action;ApplyEncounterEffects(action,action.EncounterEffects,BattleEventSource.Encounter);settlingAction=previous;
    if(Health<=0||EnemyHealth<=0)break;
   }
  }
  private void BeginContentRound(){
   var state=Enemy.Encounter;state.OwnActionsThisRound=0;state.RepeatFollowing=0;state.RandomAfterAction=0;state.MutationRemaining=0;state.RandomizeAllNumbers=false;state.ImmuneThisRound=false;state.FixedTriggers.Clear();
   UpdateEncounterPhase();RunContentPassives(EncounterTiming.RoundStart);
   // Resolve round-start inserted numbers/replays before players arrange actions.
   preparingContentRound=true;try{while(Phase==GamePhase.Resolving&&pending.Count>0)Step();}finally{preparingContentRound=false;}
  }
  private void AfterContentAction(BattleAction action,int priorRepeat){
   var state=Enemy.Encounter;if(state==null||!action.Enemy||action.EffectOnly||action.Rule!=null||action.IsNumberOnly||action.PassiveAction)return;
   if(action.EncounterSlot!=null&&!state.UsedSkills.Contains(action.SkillName))state.UsedSkills.Add(action.SkillName);
   if(priorRepeat>0&&!action.IsRepeat){for(int i=0;i<Math.Min(priorRepeat,99);i++)QueueLimited(new[]{ContentReplay(action)});}
   if(state.OwnActionsThisRound++==0)RunContentPassives(EncounterTiming.FirstOwnActionAfter,action);
   RunContentPassives(EncounterTiming.EveryOwnActionAfter,action);
   if(state.RandomAfterAction>0)QueueContentNumbers(action,new EncounterEffect {NumberMode=EncounterNumber.Random,NumberMin=1,NumberMax=6},state.RandomAfterAction);
  }
  private BattleAction ContentReplay(BattleAction source)=>new BattleAction {Enemy=source.Enemy,Number=source.Number,Effect=source.Effect,Value=source.Value,EncounterEffects=EncounterEffect.Copies(source.EncounterEffects),EncounterSlot=source.EncounterSlot,EncounterDescription=source.EncounterDescription,SkillName=source.SkillName,Special=source.Special,IsRepeat=true,PassiveAction=source.PassiveAction,SourceAction=source.SourceAction??source};
  private void CompleteContentAction(BattleAction source){
   if(Health<=0||EnemyHealth<=0)return;settlingAction=source;
   if(source.Rule!=null||!source.EffectOnly)AfterBehavior(source);
   if(source.Rule==null&&!source.EffectOnly)FillNumber(source);
   AfterContentAction(source,source.PriorContentRepeat);ActivateEnemyConditions();
  }
  private void ApplyEncounterEffects(BattleAction action,EncounterEffect[] effects,BattleEventSource source){
   var state=Enemy.Encounter;var insertions=new List<BattleAction>();int index=0;
   foreach(var effect in effects){
    if(Health<=0||EnemyHealth<=0)break;settlingEffectIndex=index++;
    bool target=effect.Target==EncounterTarget.Opponent?!action.Enemy:action.Enemy;
    int attack=CombatResolver.Clamp((long)(state?.BaseAttack??0)+EliteAttackBonus);int value=effect.Resolve(attack);
    bool scalable=effect.Operation>=EncounterOperation.Damage&&effect.Operation<=EncounterOperation.Weaken||effect.Operation==EncounterOperation.Power;
    if(source==BattleEventSource.EnemySkill&&scalable)value=CombatResolver.Clamp((long)value+CardEffectGrowthFor(action)+(effect.Operation==EncounterOperation.Power?0:Actor(action.Enemy).Power));
    string code=state?.Definition.Id+"/"+(action.EncounterSlot??action.Rule?.Code??"passive")+"/"+effect.Operation;
    switch(effect.Operation){
     case EncounterOperation.Damage:SettleDamage(action.Enemy,target,value,code,source);break;
     case EncounterOperation.Shield:SettleShield(action.Enemy,target,value,code,source);break;
     case EncounterOperation.Heal:SettleHealing(action.Enemy,target,value,code,source);break;
     case EncounterOperation.Strength:ApplyStatus(action.Enemy,target,CardEffect.Strength,value,code,source,EffectLifetime.Persistent);break;
     case EncounterOperation.Burn:ApplyStatus(action.Enemy,target,CardEffect.Burn,value,code,source);break;
     case EncounterOperation.Fortify:ApplyStatus(action.Enemy,target,CardEffect.Fortify,value,code,source,reductionPercent:effect.ReductionPercent);break;
     case EncounterOperation.IronWall:ApplyStatus(action.Enemy,target,CardEffect.IronWall,value,code,source,shieldBonusPercent:effect.ShieldBonusPercent);break;
     case EncounterOperation.Parry:ApplyStatus(action.Enemy,target,CardEffect.Parry,value,code,source);break;
     case EncounterOperation.Bleed:ApplyStatus(action.Enemy,target,CardEffect.Bleed,value,code,source);break;
     case EncounterOperation.Vulnerable:ApplyStatus(action.Enemy,target,CardEffect.Vulnerable,value,code,source);break;
     case EncounterOperation.Poison:ApplyStatus(action.Enemy,target,CardEffect.Poison,value,code,source);break;
     case EncounterOperation.Regeneration:ApplyStatus(action.Enemy,target,CardEffect.Regeneration,value,code,source);break;
     case EncounterOperation.Weaken:ApplyStatus(action.Enemy,target,CardEffect.Weaken,value,code,source,reductionPercent:effect.ReductionPercent);break;
     case EncounterOperation.Power:SettlePower(target,value,code,source);break;
     case EncounterOperation.FillNumber:case EncounterOperation.DrawNumbers:insertions.AddRange(ContentNumbers(action,effect,value));break;
     case EncounterOperation.RandomAfterAction:state.RandomAfterAction=CombatResolver.Clamp((long)state.RandomAfterAction+value);break;
     case EncounterOperation.RepeatFollowing:state.RepeatFollowing=CombatResolver.Clamp((long)state.RepeatFollowing+value);break;
     case EncounterOperation.MutateFollowing:state.MutationRemaining=CombatResolver.Clamp((long)state.MutationRemaining+value);action.ContentNumberExempt=true;if(action.EncounterContinuation&&action.SourceAction!=null)action.SourceAction.ContentNumberExempt=true;break;
     case EncounterOperation.Disorder:state.RandomizeAllNumbers=true;break;
     case EncounterOperation.RandomCardEffects:
      for(int i=0;i<Math.Min(value,99);i++){var card=GenerateCard();insertions.Add(new BattleAction {Enemy=action.Enemy,Card=card,Number=card.Number,Effect=card.Effect,EffectOnly=true,IsRepeat=true,PassiveAction=true,SourceAction=action});}break;
     case EncounterOperation.RandomOwnSequence:
      for(int i=0;i<Math.Min(value,99)&&Enemy.EncounterRules.Count>0;i++){var rule=Enemy.EncounterRules[random.Next(Enemy.EncounterRules.Count)];if(rule.Triggers>=99)continue;insertions.Add(new BattleAction {Enemy=true,Rule=rule,SequenceTrigger=++rule.Triggers,EffectOnly=true,IsRepeat=true,PassiveAction=true,SourceAction=action});}break;
     case EncounterOperation.LoseHealth:SettleHealthLoss(target,value,code,source);break;
     case EncounterOperation.HalveDebuff:
      var debuffs=new[]{CardEffect.Burn,CardEffect.Poison,CardEffect.Bleed,CardEffect.Vulnerable,CardEffect.Weaken,CardEffect.Disorder}.Where(e=>Actor(target).Statuses[e]>0).ToArray();
      if(debuffs.Length>0){var status=debuffs[random.Next(debuffs.Length)];ChangeStatus(target,status,(int)Math.Floor(Actor(target).Statuses[status]*effect.Multiplier),code);}break;
     case EncounterOperation.RandomBasicSkill:case EncounterOperation.RandomAnySkill:
      var skills=state.Definition.Skills.Where(s=>effect.Operation==EncounterOperation.RandomAnySkill||!s.Special).ToArray();
      for(int i=0;i<Math.Min(value,99)&&skills.Length>0;i++)insertions.Add(ContentAction(skills[random.Next(skills.Length)],true,true));break;
     case EncounterOperation.Invulnerable:ApplyStatus(action.Enemy,target,CardEffect.Invulnerable,1,code,source,EffectLifetime.Temporary);break;
     default:throw new InvalidOperationException("未接入敌人效果："+effect.Operation);
    }
    UpdateEncounterPhase();
    if(insertions.Count>0&&Health>0&&EnemyHealth>0){
     // A nested skill/sequence resolves before the next authored effect. Keep
     // the card's number and after-action hooks until the entire group finishes.
     if(index<effects.Length)insertions.Add(new BattleAction {Enemy=action.Enemy,EffectOnly=true,IsRepeat=action.IsRepeat,PassiveAction=action.PassiveAction,EncounterContinuation=true,EncounterSource=source,EncounterEffects=EncounterEffect.Copies(effects.Skip(index).ToArray()),EncounterSlot=action.EncounterSlot,EncounterDescription=action.EncounterDescription,SkillName=action.SkillName,Number=action.Number,SourceAction=action.SourceAction??action});
     if(!action.EncounterContinuation){action.ContentDeferred=true;insertions.Add(new BattleAction {Enemy=action.Enemy,EffectOnly=true,PassiveAction=true,ContentCompletion=true,SourceAction=action});}
     QueueLimited(insertions);return;
    }
   }
   if(Health>0&&EnemyHealth>0)QueueLimited(insertions);
  }
  private IEnumerable<BattleAction> ContentNumbers(BattleAction source,EncounterEffect effect,int count){
   for(int i=0;i<Math.Min(count,ChainLimit);i++){
    int number=effect.NumberMode==EncounterNumber.Current?source.Number:effect.NumberMode==EncounterNumber.Sequential?effect.NumberMin+i%(effect.NumberMax-effect.NumberMin+1):random.Next(effect.NumberMin,effect.NumberMax+1);
    yield return new BattleAction {Enemy=source.Enemy,Number=number,IsNumberOnly=true,SourceAction=source,NumberCandidates=effect.NumberMode==EncounterNumber.Any?Enumerable.Range(effect.NumberMin,effect.NumberMax-effect.NumberMin+1).ToArray():null};
   }
  }
  private void QueueContentNumbers(BattleAction source,EncounterEffect effect,int count)=>QueueLimited(ContentNumbers(source,effect,count));
  private void FinishContentBattle(){
   pending.Clear();Victories++;RestAvailable=true;rewardBatches.Clear();Rewards.Clear();nextAdventureLayer=0;finalBossReward=false;
   var state=Enemy.Encounter;var rewards=state.Definition.Rewards;
   foreach(var reward in rewards){
    if(reward.Probability<=0||reward.Probability<100&&random.NextDouble()*100>=reward.Probability)continue;
    int amount=random.Next(reward.Min,reward.Max+1);
    switch(reward.Kind){
     case 1:Coins=CombatResolver.Clamp((long)Coins+amount);Log("战利品：金币 +"+amount);break;
     case 2:
      for(int i=0;i<amount;i++){var batch=new EncounterRewardBatch {Remaining=1,CanSkip=reward.CanSkip};for(int j=0;j<reward.ChoiceCount;j++)batch.Cards.Add(GenerateCard());rewardBatches.Add(batch);}break;
     case 3:for(int i=0;i<amount;i++)AddAvailableEffect(DrawSequenceEffect(setup?.RewardSequencePool));Log("战利品：牌序效果 +"+amount);break;
     case 4:
      for(int i=0;i<amount;i++){var candidates=Deck.Where(c=>!c.Engraved).ToArray();if(candidates.Length==0)break;var card=candidates[random.Next(candidates.Length)];card.Engraved=true;card.Engraving=DrawEngraving();card.EngravingCode=card.Engraving.Code;}Log("战利品：随机铭刻");break;
     default:throw new InvalidOperationException("未接入敌人奖励类型："+reward.Kind);
    }
   }
   if(Boss){int next=(setup?.BossEncounters??new List<EncounterDefinition>()).Where(d=>d.LayerMax>AdventureLayer).Select(d=>Math.Max(AdventureLayer+1,d.LayerMin)).DefaultIfEmpty(0).Min();nextAdventureLayer=next;finalBossReward=next==0;}
   ShowNextContentReward();
  }
  private void ShowNextContentReward(){
   Rewards.Clear();if(rewardBatches.Count>0){Rewards.AddRange(rewardBatches[0].Cards);Phase=GamePhase.Reward;Message="战利品已结算，选择一张卡牌"+(CanSkipReward?"，也可跳过。":"。");return;}
   Phase=finalBossReward?GamePhase.Won:GamePhase.Map;
   if(nextAdventureLayer>0){
    AdventureLayer=nextAdventureLayer;nextAdventureLayer=0;
    if(Route!=null){if(Route.ActiveId>=0&&Route.Graph.Find(Route.ActiveId)?.kind==LevelKind.Boss)Route.Complete();completedRoomsBeforeLayer+=Route.Completed.Count;var graph=Route.Graph;int lanes=Math.Max(2,graph.Nodes.Max(n=>n.lane)+1);var generated=RouteGenerator.Generate(random,Math.Min(16,Math.Max(4,graph.Floors)),lanes);Route=new RouteRun(new RouteGraph(generated.Nodes,graph.Stacked,graph.CardDungeon,graph.StartLane,graph.IndependentStacks));Route.ConfigureBossUnlock(AdventureLayer);EnsureBossRouteBattles();}
    Message="BOSS已收容，进入第 "+AdventureLayer+" 层。";
   }else Message=Phase==GamePhase.Won?"全部BOSS已收容！":"奖励已结算，继续选择任务。";
  }
  public bool SkipReward(){if(!CanSkipReward)return false;rewardBatches.RemoveAt(0);ShowNextContentReward();return true;}
  public void EnsureBossRouteBattles(){
   if(setup==null||setup.BossEncounters.Count==0||Route==null||Route.Completed.Count>0||Route.ActiveId>=0)return;
   var graph=Route.Graph;var nodes=graph.Nodes.Select(n=>n.Copy()).ToList();var boss=nodes.Single(n=>n.kind==LevelKind.Boss);int lanes=nodes.Max(n=>n.lane)+1;
   // Count the minimum battles over reachable paths. Preserve service rooms and
   // append only missing combat rows, so no path stalls at a locked boss.
   var counts=new Dictionary<int,int>();
   foreach(var node in nodes.Where(n=>n.kind!=LevelKind.Boss).OrderBy(n=>n.floor)){
    var previous=nodes.Where(n=>n.floor==node.floor-1&&counts.ContainsKey(n.id)&&(graph.CardDungeon?Math.Abs(n.lane-node.lane)<=1:graph.Stacked?n.lane==node.lane:n.next.Contains(node.id))).ToArray();
    if(node.floor==0){if(graph.CardDungeon&&Math.Abs(node.lane-graph.StartLane)>1)continue;counts[node.id]=node.kind==LevelKind.Combat||node.kind==LevelKind.Elite?1:0;}
    else if(previous.Length>0)counts[node.id]=previous.Min(n=>counts[n.id])+(node.kind==LevelKind.Combat||node.kind==LevelKind.Elite?1:0);
   }
   int minimum=nodes.Where(n=>n.floor==boss.floor-1&&counts.ContainsKey(n.id)).Select(n=>counts[n.id]).DefaultIfEmpty(0).Min();int missing=Math.Max(0,Route.BossRequiredBattles-minimum);if(missing==0)return;
   int firstId=nodes.Max(n=>n.id)+1,oldFloor=boss.floor;boss.floor+=missing;
   foreach(var node in nodes.Where(n=>n.floor==oldFloor-1)){if(!graph.Stacked){node.next.Remove(boss.id);node.next.Add(firstId+node.lane);}}
   for(int row=0;row<missing;row++)for(int lane=0;lane<lanes;lane++){
    var node=new LevelNodeData {id=firstId+row*lanes+lane,floor=oldFloor+row,lane=lane,kind=LevelKind.Combat};
    if(!graph.Stacked){if(row==missing-1)node.next.Add(boss.id);else{node.next.Add(firstId+(row+1)*lanes+lane);node.next.Add(firstId+(row+1)*lanes+(lane==0?1:lane-1));}}nodes.Add(node);
   }
   var updated=new RouteRun(new RouteGraph(nodes,graph.Stacked,graph.CardDungeon,graph.StartLane,graph.IndependentStacks));updated.ConfigureBossUnlock(Route.AdventureLayer);Route=updated;
  }
 }
}
