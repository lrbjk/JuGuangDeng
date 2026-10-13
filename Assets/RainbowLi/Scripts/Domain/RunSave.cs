using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi {
 [Serializable] public sealed class SavedStatus {public CardEffect Effect;public int Value;public EffectLifetime Lifetime;}
 [Serializable] public sealed class SavedActor {public int Health,MaxHealth,Shield,Power;public List<SavedStatus> Statuses=new List<SavedStatus>();}
 [Serializable] public sealed class SavedCard {public string InstanceId;public Card Card;public bool HasSpec,HasEngraving;}
 [Serializable] public sealed class SavedAction {
  public bool ContentDeferred,ContentCompletion,EncounterContinuation;public int PriorContentRepeat;public BattleEventSource EncounterSource=BattleEventSource.EnemySkill;
  public EncounterEffect[] EncounterEffects;public string EncounterSlot,EncounterDescription;public bool PassiveAction,AppearanceApplied,LockedPosition,ContentNumberExempt;public int[] NumberCandidates;
  public int Card=-1,Rule=-1,Source=-1;public bool HasSpec;public bool Enemy,IsNumberOnly,Special,IsRepeat,IsStatusTick,EndTick,EffectOnly,BonusOnly;
  public int Number,Value,SequenceTrigger,EffectBonus,EffectMultiplier=1;public CardEffect Effect;public CardEffectSpec EffectSpec;public string SkillName;public int[] Participants=Array.Empty<int>();
 }
 [Serializable] public sealed class SavedNumber {public int Number,Action=-1;public bool Enemy,Blocked;public int[] Candidates;}
 [Serializable] public sealed class SavedRewardBatch {public int[] Cards;public int Remaining;public bool CanSkip;}
 [Serializable] public sealed class RunSave {
  public float ElapsedSeconds;
  public int Version=3;public uint RandomState;public GamePhase Phase;public SavedActor Player,Enemy;
  public bool HasEnemyEncounter;public EnemyEncounterState EnemyEncounter;public int AdventureLayer=1,NextAdventureLayer,CompletedRoomsBeforeLayer;public bool FinalBossReward;
  public List<SavedRewardBatch> RewardBatches=new List<SavedRewardBatch>();
  public int EnemyCardEffectBonus;
  public int Coins,Victories,Round,ArenaIndex,ArenaTriggerCount,EliteAttackBonus,EliteGold,ProcessedActions;
  public bool Boss,Elite,ServiceUsed,RestAvailable,RotateArena,EndTicksQueued,RandomizeFollowing;
  public string Message,LastAction;public int LastResolved=-1;public long NextEventId;
  public bool[] AdditionalEffects;
  public List<SavedCard> Cards=new List<SavedCard>();public List<SequenceRule> Rules=new List<SequenceRule>();public List<SavedAction> Actions=new List<SavedAction>();
  public int[] Deck,Hand,Draw,Discard,Rewards,RoundDiscard,Timeline,Pending,Required,Inherited,PlayerRules,ArenaRules,EnemyRules;
  public List<ConditionalSequence> Conditions=new List<ConditionalSequence>();public int EnemyRule=-1;public int[] Unlocked,ActiveRules;
  public List<SequenceEffect> AvailableEffects=new List<SequenceEffect>();public List<SavedNumber> Numbers=new List<SavedNumber>();public List<string> History=new List<string>();
  public List<string> PositionBonuses=new List<string>();
  public bool RouteBossUnlockByBattles;public int RouteAdventureLayer=1;
  public bool RouteStacked,RouteCardDungeon;public int RouteStartLane;
  public List<LevelNodeData> RouteNodes=new List<LevelNodeData>();public int RouteCurrent=-1,RouteActive=-1;public bool RouteFinished,RouteFailed;public int[] Completed=Array.Empty<int>();
 }
 public sealed partial class ContainmentGame {
  public RunSave CaptureSave(){
   var result=new RunSave {EnemyCardEffectBonus=EnemyCardEffectBonus,RandomState=random.State,Phase=Phase,Player=SaveActor(Player),Enemy=SaveActor(Enemy),Coins=Coins,Victories=Victories,Round=Round,ArenaIndex=ArenaIndex,ArenaTriggerCount=ArenaTriggerCount,EliteAttackBonus=EliteAttackBonus,EliteGold=EliteGold,ProcessedActions=processedActions,Boss=Boss,Elite=Elite,ServiceUsed=ServiceUsed,RestAvailable=RestAvailable,RotateArena=rotateArena,EndTicksQueued=endTicksQueued,RandomizeFollowing=randomizeFollowing,Message=Message,LastAction=LastAction,NextEventId=nextEventId};
   result.HasEnemyEncounter=Enemy.Encounter!=null;result.EnemyEncounter=Enemy.Encounter?.Copy();result.AdventureLayer=AdventureLayer;result.NextAdventureLayer=nextAdventureLayer;result.FinalBossReward=finalBossReward;result.CompletedRoomsBeforeLayer=completedRoomsBeforeLayer;
   var cards=new List<Card>();var rules=new List<SequenceRule>();var actions=new List<BattleAction>();
   int CardId(Card c){if(c==null)return -1;int id=cards.IndexOf(c);if(id<0){id=cards.Count;cards.Add(c);}return id;}
   int RuleId(SequenceRule r){if(r==null)return -1;int id=rules.IndexOf(r);if(id<0){id=rules.Count;rules.Add(r);}return id;}
   int ActionId(BattleAction a){if(a==null)return -1;int id=actions.IndexOf(a);if(id<0){id=actions.Count;actions.Add(a);}return id;}
   result.Deck=Deck.Select(CardId).ToArray();result.Hand=Hand.Select(CardId).ToArray();result.Draw=drawPile.Select(CardId).ToArray();result.Discard=discard.Select(CardId).ToArray();result.Rewards=Rewards.Select(CardId).ToArray();result.RoundDiscard=roundDiscard.Select(CardId).ToArray();
   result.PlayerRules=PlayerRules.Select(RuleId).ToArray();result.ArenaRules=ArenaRules.Select(RuleId).ToArray();result.EnemyRule=RuleId(EnemyRule);
   result.EnemyRules=Enemy.EncounterRules.Select(RuleId).ToArray();result.RewardBatches=rewardBatches.Select(b=>new SavedRewardBatch {Cards=b.Cards.Select(CardId).ToArray(),Remaining=b.Remaining,CanSkip=b.CanSkip}).ToList();
   result.Conditions=Enemy.ConditionalRules.Select(c=>new ConditionalSequence {Condition=c.Condition,Threshold=c.Threshold,Rule=CloneRule(c.Rule)}).ToList();
   result.Unlocked=Enemy.UnlockedIndices;result.ActiveRules=Enemy.ActiveRuleIndices;
   result.AvailableEffects=Player.AvailableEffects.Select(CopyEffect).ToList();
   result.Timeline=Timeline.Select(ActionId).ToArray();result.Pending=pending.Select(ActionId).ToArray();result.Required=requiredEnemyActions.Select(ActionId).ToArray();result.Inherited=inheritedEffects.Select(ActionId).ToArray();result.LastResolved=ActionId(LastResolvedAction);
   result.Numbers=NumberEntries.Select(n=>new SavedNumber {Number=n.Number,Enemy=n.Enemy,Blocked=n.Blocked,Candidates=n.Candidates==null||n.Candidates.Length==0?null:(int[])n.Candidates.Clone(),Action=ActionId(n.Action)}).ToList();
   for(int i=0;i<actions.Count;i++){var a=actions[i];result.Actions.Add(new SavedAction {ContentDeferred=a.ContentDeferred,ContentCompletion=a.ContentCompletion,EncounterContinuation=a.EncounterContinuation,PriorContentRepeat=a.PriorContentRepeat,EncounterSource=a.EncounterSource,EncounterEffects=EncounterEffect.Copies(a.EncounterEffects),EncounterSlot=a.EncounterSlot,EncounterDescription=a.EncounterDescription,PassiveAction=a.PassiveAction,AppearanceApplied=a.AppearanceApplied,LockedPosition=a.LockedPosition,ContentNumberExempt=a.ContentNumberExempt,NumberCandidates=a.NumberCandidates==null?null:(int[])a.NumberCandidates.Clone(),HasSpec=a.EffectSpec!=null,Card=CardId(a.Card),Rule=RuleId(a.Rule),Source=ActionId(a.SourceAction),Enemy=a.Enemy,Number=a.Number,Value=a.Value,Effect=a.Effect,EffectSpec=a.EffectSpec?.Copy(),SkillName=a.SkillName,IsNumberOnly=a.IsNumberOnly,Special=a.Special,IsRepeat=a.IsRepeat,IsStatusTick=a.IsStatusTick,EndTick=a.EndTick,EffectOnly=a.EffectOnly,BonusOnly=a.BonusOnly,SequenceTrigger=a.SequenceTrigger,EffectBonus=a.EffectBonus,EffectMultiplier=a.EffectMultiplier,Participants=a.Participants.Select(ActionId).ToArray()});}
   result.PositionBonuses=positionBonuses.ToList();
   result.Cards=cards.Select(c=>new SavedCard {InstanceId=c.InstanceId,HasSpec=c.Spec!=null,HasEngraving=c.Engraving!=null,Card=c.CopyForSnapshot()}).ToList();result.Rules=rules.Select(CloneRule).ToList();result.AdditionalEffects=rules.Select(r=>r.AdditionalEffect!=null).ToArray();result.History=new List<string>(History);
   if(Route!=null){result.RouteBossUnlockByBattles=Route.BossUnlockByBattles;result.RouteAdventureLayer=Route.AdventureLayer;result.RouteStacked=Route.Graph.Stacked;result.RouteCardDungeon=Route.Graph.CardDungeon;result.RouteStartLane=Route.Graph.StartLane;result.RouteNodes=Route.Graph.Nodes.Select(n=>n.Copy()).ToList();result.RouteCurrent=Route.CurrentId;result.RouteActive=Route.ActiveId;result.RouteFinished=Route.Finished;result.RouteFailed=Route.Failed;result.Completed=Route.Completed.ToArray();}
   return result;
  }
  private static SavedActor SaveActor(CombatantState a)=>new SavedActor {Health=a.Health,MaxHealth=a.MaxHealth,Shield=a.Shield,Power=a.Power,Statuses=a.Statuses.Snapshot().Select(s=>new SavedStatus {Effect=s.Key,Value=s.Value,Lifetime=a.Statuses.LifetimeOf(s.Key)??EffectLifetime.Persistent}).ToList()};
  private static void RestoreActor(CombatantState a,SavedActor s,bool allowEmpty=false){if(s==null||(s.MaxHealth<0||s.MaxHealth==0&&!allowEmpty)||s.Health<0||s.Health>s.MaxHealth||s.Shield<0||s.Power<0)throw new ArgumentException("Invalid saved actor");a.Health=s.Health;a.MaxHealth=s.MaxHealth;a.Shield=s.Shield;a.Power=s.Power;a.Statuses.Clear();if(s.Statuses==null)throw new ArgumentException("Missing saved statuses");foreach(var status in s.Statuses)a.Statuses.Add(status.Effect,status.Value,status.Lifetime);}
  public static ContainmentGame RestoreSave(RunSave s,GameSetup setup=null){
   if(s==null||s.Version<0||s.Version>3||!Enum.IsDefined(typeof(GamePhase),s.Phase))throw new ArgumentException("Unsupported save version or phase");
   if(s.Cards==null||s.Cards.Count>10000||s.Actions==null||s.Actions.Count>20000||s.Rules==null||s.Conditions==null||s.AvailableEffects==null||s.Numbers==null||s.History==null||s.PositionBonuses==null||s.RouteNodes==null)throw new ArgumentException("Invalid save collections");
   if(s.RandomState==0&&s.Version>=2||s.Coins<0||s.Round<0||s.ProcessedActions<0||s.ProcessedActions>ChainLimit)throw new ArgumentException("Invalid saved run state");
   if(s.Pending!=null&&s.Pending.Length>ChainLimit-s.ProcessedActions)throw new ArgumentException("Invalid saved queue size");
   var g=new ContainmentGame(1,setup);RestoreActor(g.Player,s.Player);RestoreActor(g.Enemy,s.Enemy,s.Phase!=GamePhase.Planning&&s.Phase!=GamePhase.Resolving);
   var cards=s.Cards.Select(c=>{if(c==null||c.Card==null)throw new ArgumentException("Missing card");if(s.Version>=2&&string.IsNullOrWhiteSpace(c.InstanceId))throw new ArgumentException("Missing current save card ID");var card=c.Card.CopyForSnapshot();if(s.Version>=2){if(!c.HasSpec)card.Spec=null;if(!c.HasEngraving)card.Engraving=null;}card.RestoreIdentity(c.InstanceId);return card;}).ToArray();
   if(cards.Select(c=>c.InstanceId).Distinct().Count()!=cards.Length)throw new ArgumentException("Duplicate saved card IDs");
   var rules=s.Rules.Select(CloneRule).ToArray();if(s.Version>=2&&s.AdditionalEffects!=null){if(s.AdditionalEffects.Length!=rules.Length)throw new ArgumentException("Invalid optional rule flags");for(int i=0;i<rules.Length;i++)if(!s.AdditionalEffects[i])rules[i].AdditionalEffect=null;}var actions=s.Actions.Select(a=>new BattleAction {ContentDeferred=a.ContentDeferred,ContentCompletion=a.ContentCompletion,EncounterContinuation=a.EncounterContinuation,PriorContentRepeat=a.PriorContentRepeat,EncounterSource=a.EncounterSource==BattleEventSource.Card?BattleEventSource.EnemySkill:a.EncounterSource,EncounterEffects=EncounterEffect.Copies(a.EncounterEffects),EncounterSlot=a.EncounterSlot,EncounterDescription=a.EncounterDescription,PassiveAction=a.PassiveAction,AppearanceApplied=a.AppearanceApplied,LockedPosition=a.LockedPosition,ContentNumberExempt=a.ContentNumberExempt,NumberCandidates=a.NumberCandidates==null||a.NumberCandidates.Length==0?null:(int[])a.NumberCandidates.Clone(),Enemy=a.Enemy,Number=a.Number,Value=a.Value,Effect=a.Effect,EffectSpec=s.Version>=2&&!a.HasSpec?null:a.EffectSpec?.Copy(),SkillName=a.SkillName,IsNumberOnly=a.IsNumberOnly,Special=a.Special,IsRepeat=a.IsRepeat,IsStatusTick=a.IsStatusTick,EndTick=a.EndTick,EffectOnly=a.EffectOnly,BonusOnly=a.BonusOnly,SequenceTrigger=a.SequenceTrigger,EffectBonus=a.EffectBonus,EffectMultiplier=s.Version==0?1:a.EffectMultiplier}).ToArray();
   T At<T>(T[] list,int id) where T:class {if(id==-1)return null;if(id<0||id>=list.Length)throw new ArgumentException("Invalid saved reference");return list[id];}
   for(int i=0;i<actions.Length;i++){var a=s.Actions[i];actions[i].Card=At(cards,a.Card);actions[i].Rule=At(rules,a.Rule);actions[i].SourceAction=At(actions,a.Source);actions[i].Participants=(a.Participants??Array.Empty<int>()).Select(id=>At(actions,id)).ToList();}
   void Cards(List<Card> target,int[] ids){target.Clear();target.AddRange((ids??Array.Empty<int>()).Select(id=>At(cards,id)??throw new ArgumentException("Missing card reference")));}
   void Actions(List<BattleAction> target,int[] ids){target.Clear();target.AddRange((ids??Array.Empty<int>()).Select(id=>At(actions,id)??throw new ArgumentException("Missing action reference")));}
   Cards(g.Deck,s.Deck);Cards(g.Hand,s.Hand);Cards(g.drawPile,s.Draw);Cards(g.discard,s.Discard);Cards(g.Rewards,s.Rewards);Cards(g.roundDiscard,s.RoundDiscard);
   Actions(g.Timeline,s.Timeline);g.pending.Clear();g.pending.AddRange((s.Pending??Array.Empty<int>()).Select(id=>At(actions,id)??throw new ArgumentException("Missing pending action")));Actions(g.requiredEnemyActions,s.Required);Actions(g.inheritedEffects,s.Inherited);g.LastResolvedAction=At(actions,s.LastResolved);
   g.PlayerRules.Clear();g.PlayerRules.AddRange((s.PlayerRules??Array.Empty<int>()).Select(id=>At(rules,id)));g.ArenaRules.Clear();g.ArenaRules.AddRange((s.ArenaRules??Array.Empty<int>()).Select(id=>At(rules,id)));g.EnemyRule=At(rules,s.EnemyRule);
   g.Enemy.ConditionalRules.Clear();g.Enemy.ConditionalRules.AddRange(s.Conditions.Select(c=>new ConditionalSequence {Condition=c.Condition,Threshold=c.Threshold,Rule=CloneRule(c.Rule)}));g.Enemy.RestoreActivation(s.Unlocked,s.ActiveRules);
   g.Enemy.Encounter=s.Version>=3&&s.HasEnemyEncounter?s.EnemyEncounter?.Copy()??throw new ArgumentException("Missing enemy encounter"):null;g.Enemy.EncounterRules.Clear();g.Enemy.EncounterRules.AddRange((s.EnemyRules??Array.Empty<int>()).Select(id=>At(rules,id)));
   g.AdventureLayer=s.Version>=3?Math.Max(1,s.AdventureLayer):1;g.nextAdventureLayer=s.NextAdventureLayer;g.finalBossReward=s.FinalBossReward;g.completedRoomsBeforeLayer=Math.Max(0,s.CompletedRoomsBeforeLayer);
   g.rewardBatches.Clear();foreach(var batch in s.RewardBatches??new List<SavedRewardBatch>())g.rewardBatches.Add(new EncounterRewardBatch {Cards=batch.Cards.Select(id=>At(cards,id)).ToList(),Remaining=batch.Remaining,CanSkip=batch.CanSkip});
   g.Player.AvailableEffects.Clear();g.Player.AvailableEffects.AddRange(s.AvailableEffects.Select(CopyEffect));
   g.NumberStream.Clear();g.NumberEntries.Clear();foreach(var n in s.Numbers){g.NumberStream.Add(n.Number);g.NumberEntries.Add(new NumberEntry {Number=n.Number,Enemy=n.Enemy,Blocked=n.Blocked,Candidates=n.Candidates==null||n.Candidates.Length==0?null:(int[])n.Candidates.Clone(),Action=At(actions,n.Action)});}
   g.positionBonuses.UnionWith(s.PositionBonuses);
   g.EnemyCardEffectBonus=Math.Max(0,s.EnemyCardEffectBonus);g.random.State=s.RandomState==0?0x9e3779b9:s.RandomState;g.Phase=s.Phase;g.Coins=s.Coins;g.Victories=s.Victories;g.Round=s.Round;g.ArenaIndex=s.ArenaIndex;g.ArenaTriggerCount=s.ArenaTriggerCount;g.EliteAttackBonus=s.EliteAttackBonus;g.EliteGold=s.EliteGold;g.Boss=s.Boss;g.Enemy.Elite=s.Elite;g.ServiceUsed=s.ServiceUsed;g.RestAvailable=s.RestAvailable;g.rotateArena=s.RotateArena;g.endTicksQueued=s.EndTicksQueued;g.processedActions=s.ProcessedActions;g.pending.Budget=Math.Max(0,ChainLimit-g.processedActions);g.randomizeFollowing=s.RandomizeFollowing;g.Message=s.Message;g.LastAction=s.LastAction;g.nextEventId=s.NextEventId;g.History.Clear();g.History.AddRange(s.History);
   if(s.RouteNodes.Count>0)g.Route=RouteRun.Restore(new RouteGraph(s.RouteNodes,s.RouteStacked,s.RouteCardDungeon,s.RouteStartLane),s.RouteCurrent,s.RouteActive,s.Completed,s.RouteFinished,s.RouteFailed,s.RouteBossUnlockByBattles,s.RouteAdventureLayer);
   if(g.Phase==GamePhase.Planning||g.Phase==GamePhase.Resolving){if(g.ArenaRules.Count==0||g.ArenaIndex<0||g.ArenaIndex>=g.ArenaRules.Count)throw new ArgumentException("Invalid saved arena");}
   return g;
  }
  public void ConfigureGeneratedRoute(int floors=8,int lanes=3){if(Route==null)Route=new RouteRun(RouteGenerator.Generate(random,floors,lanes));}
  public void ConfigureRoute(RouteGraph graph){if(Route==null)Route=new RouteRun(graph);}
 }
}
