using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class BattleModule : MonoBehaviour {
 public GameSession session;
 public bool autoPlay=true;
 [Min(0.05f)] public float actionInterval=0.95f, ruleInterval=0.16f;
 private float nextStep;
 private void Update(){if(autoPlay && session.Game.Phase==GamePhase.Resolving && Time.unscaledTime>=nextStep)Step();}
 public void Confirm(){if(session.Game.Confirm()){nextStep=Time.unscaledTime+0.45f;session.Notify();}}
 public void ToggleAutoPlay(){autoPlay=!autoPlay;session.Notify();}
 public void Step(){
  if(session.Game.Phase!=GamePhase.Resolving)return;
  session.ResolveOne(); var a=session.Game.LastResolvedAction;
  nextStep=Time.unscaledTime+(a!=null && (a.Rule!=null||a.IsNumberOnly)?ruleInterval:actionInterval);
 }
}}
