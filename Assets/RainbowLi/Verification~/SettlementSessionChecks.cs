using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RainbowLi;
using RainbowLi.UnityGame;

// Checks only managed session methods, using the actual Unity assemblies.
// It does not construct a native GameObject or replace editor scene testing.
public static class SettlementSessionChecks
{
    static int checks;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    static GameSession Session(){
        var session=(GameSession)RuntimeHelpers.GetUninitializedObject(typeof(GameSession));
        session.seed=42;session.NewGame();return session;
    }
    static void Arrange(GameSession session,params CardEffectInstance[] effects){
        var game=session.Game;game.StartBattle();game.PlayerRules.Clear();foreach(var rule in game.ArenaRules)rule.Numbers=new[]{0,0,0};game.EnemyRule.Numbers=new[]{0,0,0};
        var card=new Card {Number=3};card.Effects.AddRange(effects);game.Hand.Clear();game.Hand.Add(card);game.InsertCard(card,0);session.Notify();game.Confirm();
    }
    static CardEffectInstance Effect(CardEffect effect,int amount)=>new CardEffectInstance {
        BaseX=amount,Spec=new CardEffectSpec {Code="session-"+effect,Effect=effect,Name=effect.ToString(),Lifetime=EffectLifetime.Direct,Polarity=EffectPolarity.Positive}
    };
    public static void Main(){
        var session=Session();var received=new List<BattleEvent>();ResolutionEvent summary=default;int summaries=0;
        session.Settled+=received.Add;session.Resolved+=e=>{summary=e;summaries++;};
        Arrange(session,Effect(CardEffect.Mend,10));session.Game.Health=40;session.Game.Player.Statuses.Add(CardEffect.Bleed,5);received.Clear();
        Check(session.ResolveOne(),"session resolves card");
        Check(summary.PlayerDamage==5&&summary.PlayerHealing==10&&session.Game.Health==45,"compatibility summary does not lose damage to net healing");
        Check(summary.Events.Count==received.Count&&received.Count>0&&summary.Events.SequenceEqual(received),"summary and individual stream contain same immutable events");
        int count=received.Count;session.Notify();session.Notify();
        Check(received.Count==count&&summaries==1,"repeated notify does not republish settlements");
        int startEvents=received.Count;session.Game.Forecast();session.Notify();
        Check(received.Count==startEvents,"preview does not dispatch real events");
        session.NewGame();Arrange(session,Effect(CardEffect.Strike,20));session.Game.Enemy.Statuses.Add(CardEffect.Parry,1);session.Game.EnemyShield=12;received.Clear();session.ResolveOne();
        Check(summary.EnemyParried&&summary.EnemyDamage==0&&summary.EnemyBlocked==0&&session.Game.EnemyShield==12,"parry summary uses actual events");
        Check(received.Count(e=>e.Kind==BattleEventKind.Parry)==1,"parry stream emitted after restarting ID sequence");
        session.NewGame();Arrange(session,Effect(CardEffect.Guard,20),Effect(CardEffect.Strike,5));session.Game.Player.Statuses.Add(CardEffect.Bleed,2);received.Clear();session.ResolveOne();
        Check(summary.PlayerDamage==2&&summary.PlayerBlocked==0&&summary.EnemyDamage==5,"shield gain cannot hide bleed in compatibility summary");
        Check(summary.Slot==0&&summary.Events.Where(e=>e.Source==BattleEventSource.Card).All(e=>e.Slot==0),"event and old summary slots agree");

        // The next step can finish a round without resolving a queued behavior.
        while(session.Game.Phase==GamePhase.Resolving&&session.Game.Round==1&&session.Game.Timeline.Count>0){
            received.Clear();bool resolved=session.ResolveOne();
            if(!resolved){Check(received.Any(e=>e.Kind==BattleEventKind.ShieldChanged&&e.Timing==BattleEventTiming.RoundStart),"round transition publishes events even when Step returns false");break;}
        }
        count=received.Count;session.Notify();Check(received.Count==count,"round transition also dispatches once");

        session=Session();Arrange(session,Effect(CardEffect.Guard,1));
        bool restart=true;int staleEvents=0;
        session.Changed+=()=>{if(restart){restart=false;session.NewGame();}};
        session.Settled+=e=>staleEvents++;session.ResolveOne();
        Check(staleEvents==0&&session.Game.Phase==GamePhase.Map,"reentrant restart does not publish previous game's events");
        Console.WriteLine("PASS: "+checks+" managed session settlement assertions");
    }
}
