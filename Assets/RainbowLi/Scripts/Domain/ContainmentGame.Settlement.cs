using System;
using System.Collections.Generic;

namespace RainbowLi
{
    public sealed partial class ContainmentGame
    {
        private readonly List<BattleEvent> settlementEvents=new List<BattleEvent>();
        private long nextEventId;
        private BattleAction settlingAction;
        private int settlingEffectIndex=-1;
        private BattleEventTiming settlingTiming;
        // One operation's events only: consumers can copy the list if retaining it.
        public IReadOnlyList<BattleEvent> LastSettlementEvents => settlementEvents.AsReadOnly();

        private void BeginSettlement(BattleEventTiming timing)
        { settlementEvents.Clear(); settlingAction=null; settlingEffectIndex=-1; settlingTiming=timing; }
        private void Emit(BattleEventKind kind, bool sourceEnemy, bool targetEnemy, string code,
            BattleEventSource source, int requested, int resolved, int amount, int before, int after,
            int healthBefore, int shieldBefore, int blocked=0, bool parried=false,
            CardEffect? status=null, EffectLifetime? lifetime=null)
        {
            var actor=Actor(targetEnemy); var action=settlingAction;
            settlementEvents.Add(new BattleEvent(++nextEventId,kind,
                sourceEnemy?BattleSide.Enemy:BattleSide.Player,targetEnemy?BattleSide.Enemy:BattleSide.Player,
                source,settlingTiming,code,action?.Card?.InstanceId,requested,resolved,amount,before,after,
                blocked,parried,healthBefore,actor.Health,shieldBefore,actor.Shield,Round,
                action==null?-1:Timeline.IndexOf(action.SourceAction??action),action==null||action.IsStatusTick?0:processedActions,
                settlingEffectIndex,action!=null&&action.IsRepeat,action?.SequenceTrigger??0,status,lifetime));
        }
        private void SettleDamage(bool sourceEnemy, bool targetEnemy, int value, string code,
            BattleEventSource source, bool outgoing=true)
        {
            var target=Actor(targetEnemy); int parry=target.Statuses[CardEffect.Parry];
            var result=CombatResolver.Damage(outgoing?Actor(sourceEnemy):null,target,value);
            Emit(BattleEventKind.Damage,sourceEnemy,targetEnemy,code,source,value,result.ResolvedAmount,
                result.Damage,result.HealthBefore,result.HealthAfter,result.HealthBefore,result.ShieldBefore,result.Blocked,result.Parried);
            if(result.ShieldAfter!=result.ShieldBefore)
                Emit(BattleEventKind.ShieldChanged,sourceEnemy,targetEnemy,code,source,value,result.Blocked,result.Blocked,
                    result.ShieldBefore,result.ShieldAfter,result.HealthBefore,result.ShieldBefore);
            if(result.Parried){
                Emit(BattleEventKind.Parry,sourceEnemy,targetEnemy,code,source,value,result.ResolvedAmount,0,
                    result.HealthBefore,result.HealthAfter,result.HealthBefore,result.ShieldBefore,0,true);
                EmitStatusDifference(sourceEnemy,targetEnemy,CardEffect.Parry,parry,code,source);
            }
            ReportDeath(sourceEnemy,targetEnemy,code,source,result.HealthBefore,result.ShieldBefore);
        }
        private void SettleHealthLoss(bool enemy,int value,string code,BattleEventSource source)
        {
            var actor=Actor(enemy); int hp=actor.Health,shield=actor.Shield;
            int lost=CombatResolver.LoseHealth(actor,value);
            Emit(BattleEventKind.HealthLoss,enemy,enemy,code,source,value,Math.Max(0,value),lost,hp,actor.Health,hp,shield);
            ReportDeath(enemy,enemy,code,source,hp,shield);
        }
        private void ReportDeath(bool sourceEnemy,bool targetEnemy,string code,BattleEventSource source,int before,int shieldBefore)
        {
            if(before>0&&Actor(targetEnemy).Health<=0){
                Emit(BattleEventKind.Death,sourceEnemy,targetEnemy,code,source,0,0,0,before,0,before,shieldBefore);
                CheckBattleEnd();
            }
        }
        private void SettleHealing(bool sourceEnemy,bool targetEnemy,int value,string code,BattleEventSource source)
        {
            var actor=Actor(targetEnemy); int hp=actor.Health,shield=actor.Shield;
            int healed=CombatResolver.Heal(actor,value);
            Emit(BattleEventKind.Healing,sourceEnemy,targetEnemy,code,source,value,Math.Max(0,value),healed,hp,actor.Health,hp,shield);
        }
        private void SettleShield(bool sourceEnemy,bool targetEnemy,int value,string code,BattleEventSource source)
        {
            var actor=Actor(targetEnemy); int hp=actor.Health,shield=actor.Shield;
            int gained=CombatResolver.GainShield(actor,value);
            Emit(BattleEventKind.ShieldChanged,sourceEnemy,targetEnemy,code,source,value,gained,gained,shield,actor.Shield,hp,shield);
        }
        private void SettlePower(bool enemy,int value,string code,BattleEventSource source)
        {
            var actor=Actor(enemy); int before=actor.Power;
            actor.Power=CombatResolver.Clamp((long)before+value);
            Emit(BattleEventKind.PowerChanged,enemy,enemy,code,source,value,actor.Power-before,Math.Abs(actor.Power-before),before,actor.Power,actor.Health,actor.Shield);
        }
        private void ApplyStatus(bool sourceEnemy,bool targetEnemy,CardEffect effect,int value,string code,
            BattleEventSource source,EffectLifetime? lifetime=null)
        {
            if(value<=0)return;
            var actor=Actor(targetEnemy); int before=actor.Statuses[effect];
            actor.Statuses.Add(effect,value,lifetime);
            Emit(BattleEventKind.StatusApplied,sourceEnemy,targetEnemy,code,source,value,actor.Statuses[effect]-before,
                Math.Abs(actor.Statuses[effect]-before),before,actor.Statuses[effect],actor.Health,actor.Shield,
                status:effect,lifetime:actor.Statuses.LifetimeOf(effect));
        }
        private void ChangeStatus(bool enemy,CardEffect effect,int value,string code)
        {
            int before=Actor(enemy).Statuses[effect]; Actor(enemy).Statuses[effect]=value;
            EmitStatusDifference(enemy,enemy,effect,before,code,BattleEventSource.Status);
        }
        private void EmitStatusDifference(bool sourceEnemy,bool targetEnemy,CardEffect effect,int before,string code,BattleEventSource source)
        {
            var actor=Actor(targetEnemy); int after=actor.Statuses[effect]; if(before==after)return;
            Emit(after==0?BattleEventKind.StatusRemoved:BattleEventKind.StatusChanged,sourceEnemy,targetEnemy,code,
                source,0,after-before,Math.Abs(after-before),before,after,actor.Health,actor.Shield,
                status:effect,lifetime:actor.Statuses.LifetimeOf(effect));
        }
        private void ClearStatuses(bool enemy)
        {
            var actor=Actor(enemy); var before=actor.Statuses.Snapshot(); actor.Statuses.Clear();
            foreach(var entry in before)EmitStatusDifference(enemy,enemy,entry.Key,entry.Value,"EncounterReset",BattleEventSource.Encounter);
        }
        private void EndStatuses(bool enemy)
        {
            var actor=Actor(enemy); var before=actor.Statuses.Snapshot(); actor.Statuses.EndRound();
            foreach(var entry in before)EmitStatusDifference(enemy,enemy,entry.Key,entry.Value,"RoundEnd",BattleEventSource.Round);
        }
        private void ResetRoundState(bool enemy)
        {
            var actor=Actor(enemy); int shield=actor.Shield,power=actor.Power; actor.ResetRound();
            var source=settlingTiming==BattleEventTiming.EncounterStart?BattleEventSource.Encounter:BattleEventSource.Round;
            if(shield!=0)Emit(BattleEventKind.ShieldChanged,enemy,enemy,"RoundReset",source,0,0,shield,shield,0,actor.Health,shield);
            if(power!=0)Emit(BattleEventKind.PowerChanged,enemy,enemy,"RoundReset",source,0,0,Math.Abs(power),power,0,actor.Health,actor.Shield);
        }
    }
}
