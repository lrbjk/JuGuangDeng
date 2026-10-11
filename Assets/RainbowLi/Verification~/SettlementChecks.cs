using System;
using System.Collections.Generic;
using System.Linq;
using RainbowLi;

public static class SettlementChecks
{
    static int checks;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    static CardEffectInstance Effect(CardEffect effect,int amount,bool negative=false)=>new CardEffectInstance {
        BaseX=amount, Spec=new CardEffectSpec {Code="test-"+effect,Effect=effect,Name=effect.ToString(),
            Polarity=negative?EffectPolarity.Negative:EffectPolarity.Positive,Lifetime=CombatStatuses.IsDirect(effect)?EffectLifetime.Direct:EffectLifetime.Temporary}
    };
    static ContainmentGame Battle(params CardEffectInstance[] effects){
        var game=new ContainmentGame(42,new GameSetup {PlayerHealth=300,EnemyHealth=300,EnemyAttack=0,GuardInterval=0});
        var card=new Card {Number=3};card.Effects.AddRange(effects);game.Deck[0]=card;
        game.StartBattle();game.PlayerRules.Clear();foreach(var rule in game.ArenaRules)rule.Numbers=new[]{0,0,0};game.EnemyRule.Numbers=new[]{0,0,0};
        game.Hand.Clear();game.Hand.Add(card);Check(game.InsertCard(card,0),"arrange settlement card");return game;
    }
    static List<BattleEvent> FinishRound(ContainmentGame game){
        var events=new List<BattleEvent>();int steps=0;
        while(game.Phase==GamePhase.Resolving&&game.Round==1&&steps++<5000){game.Step();events.AddRange(game.LastSettlementEvents);}
        Check(steps<5000,"settlement round terminates");return events;
    }
    public static void Main(){
        var source=new PlayerState(100);var target=new PlayerState(100);
        source.Statuses.Add(CardEffect.Strength,2);source.Statuses.Add(CardEffect.Weaken,1);
        target.Statuses.Add(CardEffect.Vulnerable,2);target.Statuses.Add(CardEffect.Fortify,1);target.Statuses.Add(CardEffect.DamageCap,6);target.Shield=4;
        var hit=CombatResolver.Damage(source,target,10);
        Check(hit.RequestedAmount==10&&hit.ResolvedAmount==6&&hit.Blocked==4&&hit.Damage==2,"damage order strength, weaken, vulnerable, fortify, cap, shield");
        Check(hit.HealthBefore==100&&hit.HealthAfter==98&&hit.ShieldAfter==0,"damage result captures before and after");
        target.Statuses.Add(CardEffect.Parry,1);target.Shield=8;hit=CombatResolver.Damage(source,target,10);
        Check(hit.Parried&&hit.Damage==0&&hit.Blocked==0&&target.Health==98&&target.Shield==8&&target.Statuses[CardEffect.Parry]==0,"parry consumes a stack and preserves shield");
        target.Statuses.Add(CardEffect.Parry,1);hit=CombatResolver.Damage(source,target,0);
        Check(!hit.Parried&&hit.Damage==0&&target.Statuses[CardEffect.Parry]==1,"zero damage cannot gain strength/vulnerability damage or consume parry");
        Check(CombatResolver.LoseHealth(target,3)==3&&target.Health==95&&target.Shield==8,"direct health loss bypasses defenses");
        Check(CombatResolver.Heal(target,25)==5&&target.Health==100,"overheal reports actual healing");
        target.Health=0;Check(CombatResolver.Heal(target,25)==0&&target.Health==0,"healing does not revive dead actors");
        target.Health=100;target.Statuses.Add(CardEffect.IronWall,1);target.Shield=0;
        Check(CombatResolver.GainShield(target,5)==6&&target.Shield==6,"shield gain uses iron wall with integer rounding");
        target.Statuses.Add(CardEffect.Poison,int.MaxValue);target.Statuses.Add(CardEffect.Poison,int.MaxValue);
        Check(target.Statuses[CardEffect.Poison]==int.MaxValue,"status stacking cannot wrap negative");
        target.Shield=int.MaxValue-1;Check(CombatResolver.GainShield(target,int.MaxValue)==1&&target.Shield==int.MaxValue,"shield addition saturates");
        target.Statuses[CardEffect.Parry]=0;target.Statuses.CopyFrom(target.Statuses);
        Check(target.Statuses[CardEffect.Poison]==int.MaxValue&&target.Statuses.LifetimeOf(CardEffect.Parry)==null,"self copy and removed lifetimes remain consistent");

        var game=Battle(Effect(CardEffect.Mend,10));game.Health=200;game.Player.Statuses.Add(CardEffect.Bleed,5);
        var beforePreview=game.LastSettlementEvents.ToArray();var preview=game.Forecast();game.Forecast();
        Check(game.Health==200&&game.Player.Statuses[CardEffect.Bleed]==5&&game.LastSettlementEvents.SequenceEqual(beforePreview),"forecast does not alter live event batch or state");
        Check(game.Confirm(),"confirm healing and bleed");game.Step();var batch=game.LastSettlementEvents.ToArray();
        var bleed=batch.Single(e=>e.Kind==BattleEventKind.HealthLoss);var heal=batch.Single(e=>e.Kind==BattleEventKind.Healing);
        Check(game.Health==205&&bleed.Amount==5&&heal.Amount==10,"events preserve damage and healing even when net health increases");
        Check(bleed.Timing==BattleEventTiming.BeforeAction&&heal.Timing==BattleEventTiming.Action&&bleed.EffectCode=="Bleed"&&heal.EffectCode=="test-Mend","effect provenance and timing");
        Check(heal.CardInstanceId==game.Hand[0].InstanceId&&heal.Slot==0&&heal.EffectIndex==0&&heal.ActionOrder==1,"card identity, slot, effect index and action order");
        Check(heal.HealthBefore==195&&heal.HealthAfter==205&&heal.SourceSide==BattleSide.Player&&heal.TargetSide==BattleSide.Player,"healing event actor and absolute values");
        Check(batch.Zip(batch.Skip(1),(a,b)=>a.Id<b.Id).All(v=>v),"events are ordered with monotonic IDs");
        FinishRound(game);Check(heal.HealthAfter==205&&bleed.Amount==5,"retained events remain immutable after subsequent steps");
        Check(game.Health==preview.Health,"healing and bleed forecast matches actual");

        game=Battle(Effect(CardEffect.Strike,10));game.EnemyShield=3;game.Enemy.Statuses.Add(CardEffect.Vulnerable,2);game.Confirm();game.Step();
        hit=default;var damage=game.LastSettlementEvents.Single(e=>e.Kind==BattleEventKind.Damage);
        Check(damage.Amount==9&&damage.Blocked==3&&damage.ResolvedAmount==12&&game.EnemyHealth==291,"damage event reports shield and vulnerability accurately");
        Check(game.LastSettlementEvents.Any(e=>e.Kind==BattleEventKind.ShieldChanged&&e.Delta==-3),"shield consumption emits a distinct change");
        game=Battle(Effect(CardEffect.Strike,10));game.EnemyShield=7;game.Enemy.Statuses.Add(CardEffect.Parry,1);game.Confirm();game.Step();
        Check(game.LastSettlementEvents.Count(e=>e.Kind==BattleEventKind.Parry)==1&&game.EnemyShield==7&&game.EnemyHealth==300,"parry emits exactly one explicit event");
        Check(game.LastSettlementEvents.Any(e=>e.Kind==BattleEventKind.StatusRemoved&&e.Status==CardEffect.Parry&&e.Before==1&&e.After==0),"parry consumption emits stack removal");

        game=Battle(Effect(CardEffect.Vulnerable,2,true),Effect(CardEffect.Strike,4));game.Confirm();game.Step();batch=game.LastSettlementEvents.ToArray();
        Check(batch.First(e=>e.Kind==BattleEventKind.StatusApplied).EffectIndex==0&&batch.First(e=>e.Kind==BattleEventKind.Damage).EffectIndex==1,"compound effects emit independent ordered events");
        Check(game.EnemyHealth==294&&batch.First().Status==CardEffect.Vulnerable&&batch.First().Lifetime==EffectLifetime.Temporary,"first compound status affects second effect");

        game=Battle(Effect(CardEffect.Strike,20),Effect(CardEffect.Guard,50));game.EnemyHealth=3;game.Confirm();game.Step();batch=game.LastSettlementEvents.ToArray();
        Check(batch.Single(e=>e.Kind==BattleEventKind.Damage).Amount==3&&batch.Count(e=>e.Kind==BattleEventKind.Death)==1,"overkill clamps actual damage and emits one death");
        Check(game.Phase==GamePhase.Reward&&game.Coins==25&&game.Shield==0&&batch.All(e=>e.EffectIndex!=1),"death interrupts composite card and awards victory once");
        game.Step();Check(game.Coins==25,"repeated stepping after death cannot duplicate rewards");

        game=Battle(Effect(CardEffect.Guard,1));game.Health=2;game.Player.Statuses.Add(CardEffect.Bleed,3);game.Shield=100;game.Confirm();game.Step();
        Check(game.Phase==GamePhase.Lost&&game.LastSettlementEvents.Single(e=>e.Kind==BattleEventKind.HealthLoss).Amount==2&&game.LastSettlementEvents.Count(e=>e.Kind==BattleEventKind.Death)==1,"fatal bleed records actual health lost");
        Check(game.Shield==100&&game.NumberStream.Count==0&&game.LastSettlementEvents.All(e=>e.Source!=BattleEventSource.Card),"fatal bleed cancels the card before execution");

        game=Battle(Effect(CardEffect.Guard,3));game.Player.Statuses.Add(CardEffect.Poison,8);game.Player.Statuses.Add(CardEffect.Strength,5,EffectLifetime.Persistent);
        game.Player.Statuses.Add(CardEffect.Weaken,2);game.Player.Statuses.Add(CardEffect.Mutation,1);game.Confirm();var roundEvents=FinishRound(game);
        Check(roundEvents.Any(e=>e.EffectCode=="Poison"&&e.Kind==BattleEventKind.Damage&&e.Amount==5&&e.Timing==BattleEventTiming.RoundEnd),"poison uses incoming defenses, not owner's outgoing strength");
        Check(roundEvents.Any(e=>e.Status==CardEffect.Poison&&e.Kind==BattleEventKind.StatusChanged&&e.After==4),"poison halves after ticking");
        Check(roundEvents.Any(e=>e.Status==CardEffect.Weaken&&e.After==1&&e.Timing==BattleEventTiming.RoundEnd)&&game.Player.Statuses[CardEffect.Strength]==5,"duration decay and persistent status semantics");
        Check(roundEvents.Any(e=>e.Status==CardEffect.Mutation&&e.Kind==BattleEventKind.StatusRemoved),"number-state consumption emits removal");
        game=Battle(Effect(CardEffect.Guard,3));game.Confirm();roundEvents=FinishRound(game);
        Check(roundEvents.Any(e=>e.Kind==BattleEventKind.ShieldChanged&&e.Before==3&&e.After==0&&e.Timing==BattleEventTiming.RoundStart),"round reset emits shield loss");

        game=Battle(Effect(CardEffect.Guard,1));game.Player.Statuses.Add(CardEffect.Burn,2);game.Player.Statuses.Add(CardEffect.Regeneration,3);game.Player.Statuses.Add(CardEffect.Barrier,5);game.Health=200;
        game.Confirm();roundEvents=FinishRound(game);
        Check(roundEvents.Any(e=>e.EffectCode=="Burn"&&e.Kind==BattleEventKind.Damage&&e.Amount==2&&e.Timing==BattleEventTiming.RoundStart),"burn round-start damage");
        Check(roundEvents.Any(e=>e.EffectCode=="Regeneration"&&e.Kind==BattleEventKind.Healing&&e.Amount==3)&&roundEvents.Any(e=>e.EffectCode=="Barrier"&&e.Kind==BattleEventKind.ShieldChanged&&e.Amount==5),"regeneration and barrier round-start events");
        game=Battle(Effect(CardEffect.Guard,1));game.Health=1;game.Player.Statuses.Add(CardEffect.Burn,3);game.Player.Statuses.Add(CardEffect.Regeneration,9);game.Confirm();roundEvents=FinishRound(game);
        Check(game.Phase==GamePhase.Lost&&roundEvents.Count(e=>e.Kind==BattleEventKind.Death)==1&&roundEvents.All(e=>e.Kind!=BattleEventKind.Healing),"fatal round-start burn cancels later healing");

        game=Battle(Effect(CardEffect.Ignite,2),Effect(CardEffect.Envenom,3));game.Confirm();game.Step();
        Check(game.LastSettlementEvents.Any(e=>e.EffectCode=="Ignite"&&e.Status==CardEffect.Burn&&e.Timing==BattleEventTiming.AfterAction)&&game.LastSettlementEvents.Any(e=>e.EffectCode=="Envenom"&&e.Status==CardEffect.Poison),"on-behavior status applications share settlement path");

        game=Battle(Effect(CardEffect.Guard,1));game.PlayerRules.Add(ContainmentGame.Rule("sequence-test",new[]{3,3,3},RuleEffect.Power,2));
        game.Timeline[1].Number=3;game.Timeline[2].Number=3;game.Confirm();game.Step();game.Step();game.Step();game.Step();
        Check(game.LastSettlementEvents.Any(e=>e.Kind==BattleEventKind.PowerChanged&&e.Source==BattleEventSource.Sequence&&e.Amount==2&&e.RuleTriggerCount==1),"sequence power uses settlement events");

        game=Battle(Effect(CardEffect.Guard,1));game.PlayerRules.Add(ContainmentGame.Rule("chant-events",new[]{3,3,3},RuleEffect.Power,1));
        game.Player.Statuses.Add(CardEffect.Chant,2);game.Timeline[1].Number=3;game.Timeline[2].Number=3;game.Confirm();game.Step();game.Step();game.Step();
        Check(game.PlayerRules[0].Triggers==3,"chant queues three sequence activations");
        for(int trigger=1;trigger<=3;trigger++){game.Step();Check(game.LastSettlementEvents.Single(e=>e.Kind==BattleEventKind.PowerChanged).RuleTriggerCount==trigger,"sequence event keeps its own trigger number");}

        game=Battle(Effect(CardEffect.Guard,1));game.Hand[0].Engraved=true;
        game.ArenaRules[0].Numbers=new[]{3,3,3};game.ArenaRules[0].Effect=RuleEffect.Shield;
        game.Timeline[1].Number=3;game.Timeline[2].Number=3;
        var selected=game.Timeline[0];game.Timeline.RemoveAt(0);game.Timeline.Add(selected);game.Confirm();game.Step();game.Step();game.Step();
        Check(game.LastSettlementEvents.Any(e=>e.Source==BattleEventSource.Engraving&&e.TargetSide==BattleSide.Enemy&&e.Amount==5),"legacy engraving uses unified damage");

        game=new ContainmentGame(10);game.Health=65;game.OpenService(false);game.RestHeal();
        Check(game.LastSettlementEvents.Single().Kind==BattleEventKind.Healing&&game.LastSettlementEvents.Single().Amount==5&&game.Health==70,"rest healing capped and recorded");
        game=new ContainmentGame(10);game.Health=65;game.OpenRouteEvent();game.ResolveRouteEvent(true,20);
        Check(game.LastSettlementEvents.Single().Source==BattleEventSource.Service&&game.LastSettlementEvents.Single().Amount==5,"route event uses same healing");
        game.Player.Statuses.Add(CardEffect.Poison,4);game.Enemy.Shield=3;game.StartBattle();
        Check(game.LastSettlementEvents.Any(e=>e.Source==BattleEventSource.Encounter&&e.Kind==BattleEventKind.StatusRemoved&&e.Status==CardEffect.Poison)&&game.LastSettlementEvents.Any(e=>e.Source==BattleEventSource.Encounter&&e.Kind==BattleEventKind.ShieldChanged&&e.Delta==-3),"encounter clears produce explicit events");
        Console.WriteLine("PASS: "+checks+" unified settlement and event assertions");
    }
}
