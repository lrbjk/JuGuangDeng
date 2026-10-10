using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi.UnityGame {
public struct ResolutionEvent {
 public BattleAction Action;
 public bool PlayerParried,EnemyParried;
 public int PlayerDamage, EnemyDamage, PlayerBlocked, EnemyBlocked, Slot, Round;
 public int PlayerHealing,EnemyHealing;
 public IReadOnlyList<BattleEvent> Events;
}
[DisallowMultipleComponent]
public sealed class GameSession : MonoBehaviour {
 public GameDefinition definition;
 public CardModule cardSystem;
 public PlayerModule player;
 public EnemyModule enemy;
 [Tooltip("0 为随机，每次测试复现可设置非零种子")] public int seed;
 public ContainmentGame Game {get; private set;}
 public event Action Changed;
 public event Action<ResolutionEvent> Resolved;
 public event Action<BattleEvent> Settled;
 private ContainmentGame publishedGame;
 private long publishedEventId;
 private void Awake() { NewGame(); }
 public void NewGame() { var setup=definition==null?new GameSetup():definition.CreateSetup();player?.Apply(setup);enemy?.Apply(setup);cardSystem?.Apply(setup);player?.definition?.cardTable?.Apply(setup);Game=new ContainmentGame(seed,setup); Notify(); }
 public void Notify() {
  var game=Game;
  if(publishedGame!=game){publishedGame=game;publishedEventId=0;}
  var events=game==null?Array.Empty<BattleEvent>():game.LastSettlementEvents.Where(e=>e.Id>publishedEventId).ToArray();
  if(events.Length>0)publishedEventId=events[events.Length-1].Id;
  Changed?.Invoke();
  foreach(var entry in events){if(Game!=game)break;Settled?.Invoke(entry);}
 }
 public bool ResolveOne() {
  var g=Game; int round=g.Round;
  bool resolved=g.Step(); var events=g.LastSettlementEvents.ToArray(); var action=g.LastResolvedAction;
  int slot=action==null?-1:g.Timeline.IndexOf(action.SourceAction??action); Notify();
  if(resolved&&Game==g) Resolved?.Invoke(new ResolutionEvent { Action=action, Slot=slot,Round=round,
   PlayerParried=events.Any(e=>e.Kind==BattleEventKind.Parry&&e.TargetSide==BattleSide.Player),EnemyParried=events.Any(e=>e.Kind==BattleEventKind.Parry&&e.TargetSide==BattleSide.Enemy),
   PlayerDamage=Total(events,BattleSide.Player,false),EnemyDamage=Total(events,BattleSide.Enemy,false),
   PlayerHealing=Total(events,BattleSide.Player,true),EnemyHealing=Total(events,BattleSide.Enemy,true),
   PlayerBlocked=CombatResolver.Clamp(events.Where(e=>e.Kind==BattleEventKind.Damage&&e.TargetSide==BattleSide.Player).Sum(e=>(long)e.Blocked)),
   EnemyBlocked=CombatResolver.Clamp(events.Where(e=>e.Kind==BattleEventKind.Damage&&e.TargetSide==BattleSide.Enemy).Sum(e=>(long)e.Blocked)),Events=Array.AsReadOnly(events) });
  return resolved;
 }
 private static int Total(BattleEvent[] events,BattleSide side,bool healing)=>CombatResolver.Clamp(events.Where(e=>e.TargetSide==side&&(healing?e.Kind==BattleEventKind.Healing:e.Kind==BattleEventKind.Damage||e.Kind==BattleEventKind.HealthLoss)).Sum(e=>(long)e.Amount));
}}
