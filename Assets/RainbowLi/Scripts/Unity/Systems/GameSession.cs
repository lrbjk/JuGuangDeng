using System;
using UnityEngine;
namespace RainbowLi.UnityGame {
public struct ResolutionEvent {
 public BattleAction Action;
 public bool PlayerParried,EnemyParried;
 public int PlayerDamage, EnemyDamage, PlayerBlocked, EnemyBlocked, Slot, Round;
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
 private void Awake() { NewGame(); }
 public void NewGame() { var setup=definition==null?new GameSetup():definition.CreateSetup();player?.Apply(setup);enemy?.Apply(setup);cardSystem?.Apply(setup);player?.definition?.cardTable?.Apply(setup);Game=new ContainmentGame(seed,setup); Notify(); }
 public void Notify() { Changed?.Invoke(); }
 public bool ResolveOne() {
  var g=Game; int hp=g.Health,ehp=g.EnemyHealth,s=g.Shield,es=g.EnemyShield,round=g.Round;
  int pp=g.Player.Statuses[CardEffect.Parry],ep=g.Enemy.Statuses[CardEffect.Parry];
  bool resolved=g.Step(); Notify();
  if(resolved) Resolved?.Invoke(new ResolutionEvent { Action=g.LastResolvedAction, Slot=g.LastResolvedAction==null?-1:g.Timeline.IndexOf(g.LastResolvedAction.SourceAction??g.LastResolvedAction),Round=round,
   PlayerParried=g.Player.Statuses[CardEffect.Parry]<pp,EnemyParried=g.Enemy.Statuses[CardEffect.Parry]<ep,
   PlayerDamage=Mathf.Max(0,hp-g.Health), EnemyDamage=Mathf.Max(0,ehp-g.EnemyHealth),PlayerBlocked=Mathf.Max(0,s-g.Shield),EnemyBlocked=Mathf.Max(0,es-g.EnemyShield) });
  return resolved;
 }
}}
