using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi {
 // Numeric order matches the authored tables. The domain never references Unity assets.
 public enum EncounterOperation { Unconfigured,Damage,Shield,Heal,Strength,Burn,Fortify,IronWall,Parry,Bleed,Vulnerable,Poison,Regeneration,Weaken,FillNumber,Power,RandomAfterAction,RepeatFollowing,MutateFollowing,Disorder,RandomCardEffects,RandomOwnSequence,LoseHealth,HalveDebuff,RandomBasicSkill,RandomAnySkill,Invulnerable,DrawNumbers }
 public enum EncounterTarget { Self,Opponent,Numbers }
 public enum EncounterNumber { None,Random,Current,BattleFixed,Any,Fixed,Sequential }
 public enum EncounterTrigger { None,HealthThreshold,UsedSkill,SequenceCount,RoundInterval,PhaseInterval }
 public enum EncounterTiming { RoundStart,FirstOwnActionAfter,EveryOwnActionAfter,Appearance }
 [Serializable] public sealed class EncounterEffect {
  public int ShieldBonusPercent=25,ReductionPercent=25;public EncounterOperation Operation;public EncounterTarget Target;public bool UsesAttack;
  public float Multiplier=1;public int Amount=1,Offset,NumberMin=1,NumberMax=6;public EncounterNumber NumberMode;
  public int Resolve(int attack){double value=Math.Floor(Math.Max(0,(UsesAttack?attack:Amount)*(double)Multiplier+Offset));return (int)Math.Min(int.MaxValue,value);}
  public EncounterEffect Copy()=>(EncounterEffect)MemberwiseClone();
  public static EncounterEffect[] Copies(EncounterEffect[] effects)=>(effects??Array.Empty<EncounterEffect>()).Select(e=>e.Copy()).ToArray();
 }
 [Serializable] public sealed class EncounterSkill {
  public string Slot,Name,Description,RequiredSkill;public float Weight;public bool Special,FixedTrigger,PriorityOnAppearance,LockInPhase;
  public int NumberMin=1,NumberMax=6,ThresholdPercent,RequiredStage,RoundInterval,SequenceCountThreshold;
  public EncounterTrigger Trigger;public EncounterEffect[] Effects=Array.Empty<EncounterEffect>();
  public EncounterSkill Copy(){var c=(EncounterSkill)MemberwiseClone();c.Effects=EncounterEffect.Copies(Effects);return c;}
 }
 [Serializable] public sealed class EncounterSequence {
  public string Slot,Name,Description;public int RequiredNumber;public int[] FixedNumbers=Array.Empty<int>();public EncounterEffect[] Effects=Array.Empty<EncounterEffect>();
  public EncounterSequence Copy(){var c=(EncounterSequence)MemberwiseClone();c.FixedNumbers=(int[])FixedNumbers.Clone();c.Effects=EncounterEffect.Copies(Effects);return c;}
 }
 [Serializable] public sealed class EncounterPassive {
  public EncounterTiming Timing;public EncounterEffect[] Effects=Array.Empty<EncounterEffect>();
  public EncounterPassive Copy()=>new EncounterPassive {Timing=Timing,Effects=EncounterEffect.Copies(Effects)};
 }
 [Serializable] public sealed class EncounterPhase {
  public int Index,ThresholdPercent;public string Label;public EncounterPassive[] Passives=Array.Empty<EncounterPassive>();
  public EncounterPhase Copy()=>new EncounterPhase {Index=Index,ThresholdPercent=ThresholdPercent,Label=Label,Passives=Passives.Select(p=>p.Copy()).ToArray()};
 }
 [Serializable] public sealed class EncounterReward {
  public int Kind,Min,Max,ChoiceCount;public bool CanSkip;public float Probability;
  public EncounterReward Copy()=>(EncounterReward)MemberwiseClone();
 }
 [Serializable] public sealed class EncounterDefinition {
  public string Id,Title;public bool Boss;public int Health,ActionMin,ActionMax,AttackMin,AttackMax,LayerMin=1,LayerMax=1;
  public EncounterSkill[] Skills=Array.Empty<EncounterSkill>();public EncounterSequence[] Sequences=Array.Empty<EncounterSequence>();
  public EncounterPhase[] Phases=Array.Empty<EncounterPhase>();public EncounterReward[] Rewards=Array.Empty<EncounterReward>();
  public EncounterDefinition Copy(){var c=(EncounterDefinition)MemberwiseClone();c.Skills=Skills.Select(s=>s.Copy()).ToArray();c.Sequences=Sequences.Select(s=>s.Copy()).ToArray();c.Phases=Phases.Select(p=>p.Copy()).ToArray();c.Rewards=Rewards.Select(r=>r.Copy()).ToArray();return c;}
 }
 [Serializable] public sealed class EnemyEncounterState {
  public EncounterDefinition Definition;public int BaseAttack,PhaseIndex,PhaseStartRound=1,TotalSequenceTriggers,MinHealthPercent=100,OwnActionsThisRound,RepeatFollowing,RandomAfterAction,MutationRemaining;
  public bool RandomizeAllNumbers,ImmuneThisRound;
  public List<string> Unlocked=new List<string>(),UsedSkills=new List<string>(),FixedTriggers=new List<string>();
  public EnemyEncounterState Copy(){var c=(EnemyEncounterState)MemberwiseClone();c.Definition=Definition.Copy();c.Unlocked=new List<string>(Unlocked);c.UsedSkills=new List<string>(UsedSkills);c.FixedTriggers=new List<string>(FixedTriggers);return c;}
 }
 [Serializable] public sealed class EncounterRewardBatch {
  public List<Card> Cards=new List<Card>();public int Remaining;public bool CanSkip;
 }
}
