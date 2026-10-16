using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class GameAudioController:MonoBehaviour {
  public GameView view;
  public GameAudioProfile profile;
  public AudioSource uiSource,battleSource;
  public bool HasConfiguredAudio=>isActiveAndEnabled&&profile!=null;
  public event Action<GameAudioCue> Played;
  private readonly Dictionary<GameAudioCue,float> lastPlayed=new Dictionary<GameAudioCue,float>();
  private readonly HashSet<long> consumedEvents=new HashSet<long>();
  private readonly Queue<long> eventOrder=new Queue<long>();
  private readonly List<BattleAction> timeline=new List<BattleAction>();
  private ContainmentGame observed;
  private int room,layer,coins,deck,round,hand;
  private bool serviceUsed,menuOpen;
  private void Awake(){if(profile!=null)foreach(var entry in profile.entries)if(entry!=null&&entry.clip!=null)entry.clip.LoadAudioData();}
  private bool Consume(long id){if(!consumedEvents.Add(id))return false;eventOrder.Enqueue(id);while(eventOrder.Count>8192)consumedEvents.Remove(eventOrder.Dequeue());return true;}
  private void OnEnable(){view.session.Changed+=Observe;view.session.Settled+=OnSettled;}
  private void OnDisable(){view.session.Changed-=Observe;view.session.Settled-=OnSettled;uiSource?.Stop();battleSource?.Stop();}
  private void Start(){menuOpen=view.GetComponent<StartMenuView>()?.IsOpen==true;Observe();}
  private void LateUpdate(){
   bool open=view.GetComponent<StartMenuView>()?.IsOpen==true;if(open!=menuOpen){menuOpen=open;Play(GameAudioCue.SceneChange);}
   if(battleSource!=null){if(view.battle.IsPresentationPaused||view.session.PauseElapsedTime)battleSource.Pause();else battleSource.UnPause();}
  }
  public void SetVolume(float value){float volume=Mathf.Clamp01(value);if(uiSource!=null)uiSource.volume=volume;if(battleSource!=null)battleSource.volume=volume;}
  public void Play(GameAudioCue cue,bool battle=false){
   if(!HasConfiguredAudio)return;var entry=profile.Find(cue);var source=battle?battleSource:uiSource;if(entry==null||source==null)return;
   float now=Time.unscaledTime;if(cue!=GameAudioCue.SceneChange&&cue!=GameAudioCue.MainMenuButton&&cue!=GameAudioCue.Kill&&lastPlayed.TryGetValue(cue,out var previous)&&now-previous<profile.minimumInterval)return;
   lastPlayed[cue]=now;source.pitch=entry.pitch;source.PlayOneShot(entry.clip,entry.volume);Played?.Invoke(cue);
  }
  private static int Room(GamePhase phase)=>phase==GamePhase.Planning||phase==GamePhase.Resolving?1:phase==GamePhase.Map?0:phase==GamePhase.Reward?2:phase==GamePhase.Shop?3:phase==GamePhase.Rest?4:phase==GamePhase.Event?5:6;
  private void Observe(){
   var game=view.session.Game;if(game==null)return;bool fresh=game!=observed;int nextRoom=Room(game.Phase);
   if(fresh){observed=game;consumedEvents.Clear();eventOrder.Clear();timeline.Clear();lastPlayed.Clear();}
   else{
    if(nextRoom!=room||game.AdventureLayer!=layer)Play(GameAudioCue.SceneChange);
    bool service=room==3||room==4;
    if(room==5&&nextRoom==0||service&&(game.Coins!=coins||game.Deck.Count!=deck||!serviceUsed&&game.ServiceUsed))Play(GameAudioCue.GainEffect);
   }
   bool motion=view.cardMotion!=null&&view.cardMotion.isActiveAndEnabled&&view.cardMotion.enableMotion;
   if(!motion&&nextRoom==1){
    if(fresh||room!=1||round!=game.Round){if(hand>0&&round>0)Play(GameAudioCue.Click);foreach(var card in game.Hand)Play(GameAudioCue.Click);}
    else if(!timeline.SequenceEqual(game.Timeline))Play(GameAudioCue.Button);
   }
   room=nextRoom;layer=game.AdventureLayer;coins=game.Coins;deck=game.Deck.Count;round=game.Round;hand=game.Hand.Count;serviceUsed=game.ServiceUsed;
   timeline.Clear();timeline.AddRange(game.Timeline);
  }
  private void OnSettled(BattleEvent e){
   if(e.Source==BattleEventSource.Service)return;
   if(view.battle.IsSkipping){if(e.Kind==BattleEventKind.Death&&Consume(e.Id))Play(GameAudioCue.Kill);return;}
   if(e.Timing==BattleEventTiming.RoundStart||e.Timing==BattleEventTiming.RoundEnd||e.Timing==BattleEventTiming.EncounterStart)PlayEvents(new[]{e});
  }
  private void PlayEvents(IEnumerable<BattleEvent> events){
   var hits=new HashSet<BattleSide>();var gains=new HashSet<BattleSide>();
   foreach(var e in events){
    if(!Consume(e.Id))continue;
    if((e.Kind==BattleEventKind.Damage||e.Kind==BattleEventKind.HealthLoss)&&(e.Amount>0||e.Blocked>0))hits.Add(e.TargetSide);
    if(e.Kind==BattleEventKind.Death)Play(GameAudioCue.Kill,true);
    if(e.Kind==BattleEventKind.Healing&&e.Amount>0||(e.Kind==BattleEventKind.ShieldChanged||e.Kind==BattleEventKind.PowerChanged||e.Kind==BattleEventKind.StatusApplied||e.Kind==BattleEventKind.StatusChanged)&&e.Delta>0)gains.Add(e.TargetSide);
   }
   foreach(var side in hits)Play(GameAudioCue.Hit,true);foreach(var side in gains)Play(GameAudioCue.GainEffect,true);
  }
  public void PlayResolution(ResolutionEvent e){
   if(view.battle.IsSkipping)return;
   var action=e.Action;
   if(action!=null&&!action.IsStatusTick&&!action.IsNumberOnly&&!action.ContentCompletion)Play(action.Rule!=null?GameAudioCue.SequenceTrigger:GameAudioCue.CardTrigger,true);
   if(e.Events!=null)PlayEvents(e.Events);
   else if(e.PlayerDamage+e.EnemyDamage+e.PlayerBlocked+e.EnemyBlocked>0)Play(GameAudioCue.Hit,true);
  }
 }
}
