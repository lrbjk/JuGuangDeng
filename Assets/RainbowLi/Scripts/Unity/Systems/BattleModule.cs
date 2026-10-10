using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class BattleModule : MonoBehaviour {
 [Tooltip("策划统一动效配置；行动速度按钮仍可独立切换播放倍率。")]
 public BattlePresentationProfile profile;
 public float EffectiveActionInterval=>profile==null?actionInterval:Mathf.Max(.05f,profile.actionInterval)*actionInterval/.95f;
 public float EffectiveRuleInterval=>profile==null?ruleInterval:Mathf.Max(.05f,profile.ruleInterval)*actionInterval/.95f;
 public void ApplyProfile(){if(profile==null)return;accelerateRepeats=profile.accelerateRepeats;accelerateAfter=Mathf.Max(1,profile.accelerateAfter);speedGainPerRepeat=profile.speedGainPerRepeat;maxRepeatSpeed=Mathf.Max(1,profile.maxRepeatSpeed);}
 public GameSession session;
 public CardMotionController presentation;
 public bool autoPlay=true;
 [Min(0.05f)] public float actionInterval=0.95f, ruleInterval=0.16f;
 [Header("长连锁自动加速")]
 public bool accelerateRepeats=true;
 [Min(1)] public int accelerateAfter=6;
 [Range(.05f,1)] public float speedGainPerRepeat=.25f;
 [Range(1,12)] public float maxRepeatSpeed=6;
 public int RepeatedEffects {get;private set;}
 public float RepeatSpeed {get{ApplyProfile();return !accelerateRepeats?1:Mathf.Clamp(1+Mathf.Max(0,RepeatedEffects-Mathf.Max(1,accelerateAfter))*speedGainPerRepeat,1,Mathf.Max(1,maxRepeatSpeed));}}
 private ContainmentGame observed;
 private int observedRound;
 private BattleAction lastCounted;
 private float nextStep;
 private bool stepping,pendingStep,designerPlaying;
 public bool IsSkipping {get;private set;}
 public bool IsStepping=>stepping;
 public float PlaybackSpeed=>Mathf.Clamp(.95f/Mathf.Max(.05f,actionInterval),.25f,8);
 public bool HasPresentation=>(presentation!=null&&presentation.BlocksResolution)||(Effects!=null&&Effects.ActiveBursts>0);
 private BattleEffects Effects=>presentation==null?GetComponentInChildren<BattleEffects>():presentation.effects;
 private bool InPlayback=>session!=null&&session.Game!=null&&(session.Game.Phase==GamePhase.Resolving||presentation!=null&&presentation.KeepBattleVisible);
 public bool IsPresentationPaused=>InPlayback&&!autoPlay&&!stepping&&!(designerPlaying&&HasPresentation)&&!IsSkipping;
 private readonly BattlePlaybackClock clock=new BattlePlaybackClock();
 public float PresentationTime=>clock.Tick(Time.frameCount,Time.unscaledDeltaTime,!IsPresentationPaused,PlaybackSpeed*(InPlayback?RepeatSpeed:1));
 public void BeginDesignerPreview(){designerPlaying=true;}
 public void EndDesignerPreview(){designerPlaying=false;}
 public void CycleSpeed(){SetSpeed(PlaybackSpeed<1.5f?2:PlaybackSpeed<3?4:1);}
 public void SetSpeed(float speed){actionInterval=.95f/Mathf.Clamp(speed,.25f,8);session?.Notify();}
 private void ResetPlayback(){stepping=pendingStep=designerPlaying=IsSkipping=false;nextStep=0;clock.Reset();ResetRepeatSpeed();presentation?.ClearDesignerPreview();Effects?.ClearDesignerPreview();}
 private void OnEnable(){if(session!=null){session.Resolved+=TrackResolution;session.Changed+=TrackReset;}}
 private void OnDisable(){if(session!=null){session.Resolved-=TrackResolution;session.Changed-=TrackReset;}ResetPlayback();}
 private void ResetRepeatSpeed(){RepeatedEffects=0;lastCounted=null;}
 private void TrackReset(){var g=session.Game;if(g==null)return;if(observed!=g)ResetPlayback();else if(observedRound!=g.Round||g.Phase!=GamePhase.Resolving)ResetRepeatSpeed();observed=g;observedRound=g.Round;}
 public void TrackResolution(ResolutionEvent e){
  ApplyProfile();
  if(session==null||session.Game==null||session.Game.Phase!=GamePhase.Resolving)return;
  var a=e.Action;if(a==null||ReferenceEquals(a,lastCounted))return;
  if(observed!=session.Game||observedRound!=e.Round){ResetRepeatSpeed();observed=session.Game;observedRound=e.Round;}
  lastCounted=a;
  if(a.IsStatusTick){RepeatedEffects=0;return;}
  if(a.Rule!=null||a.IsRepeat||a.IsNumberOnly)RepeatedEffects=Mathf.Min(RepeatedEffects+1,4096);
  else RepeatedEffects=0;
 }
 private void Update(){
  if(session==null||session.Game==null)return;
  float now=PresentationTime;
  if(IsSkipping){for(int i=0;i<32&&session.Game.Phase==GamePhase.Resolving;i++){if(!session.ResolveOne())break;}if(session.Game.Phase!=GamePhase.Resolving)FinishSkip();return;}
  if(designerPlaying&&!HasPresentation)designerPlaying=false;
  if(stepping){
   if(pendingStep&&!HasPresentation){pendingStep=false;if(session.Game.Phase==GamePhase.Resolving)ResolveNext();}
   else if(!pendingStep&&!HasPresentation){stepping=false;session.Notify();}
   return;
  }
  if(autoPlay&&session.Game.Phase==GamePhase.Resolving&&now>=nextStep&&!HasPresentation)ResolveNext();
 }
 public void Confirm(){if(session!=null&&session.Game!=null&&session.Game.Confirm()){nextStep=PresentationTime+.45f;session.Notify();}}
 public void ToggleAutoPlay(){if(IsSkipping)return;autoPlay=!autoPlay;stepping=pendingStep=designerPlaying=false;session?.Notify();}
 // A single step drains earlier flights, resolves exactly one event, then plays its full feedback.
 public void Step(){
  if(session==null||session.Game==null||session.Game.Phase!=GamePhase.Resolving||stepping||IsSkipping)return;
  autoPlay=false;stepping=pendingStep=true;designerPlaying=false;session.Notify();
 }
 private void ResolveNext(){
  session.ResolveOne();var a=session.Game.LastResolvedAction;
  float interval=a!=null&&(a.Rule!=null||a.IsNumberOnly)?(profile==null?ruleInterval:profile.ruleInterval):(profile==null?.95f:profile.actionInterval);
  nextStep=PresentationTime+Mathf.Max(.05f,interval);
 }
 public void Skip(){
  if(session==null||session.Game==null||IsSkipping||session.Game.Phase!=GamePhase.Resolving&&!(presentation!=null&&presentation.KeepBattleVisible))return;
  IsSkipping=true;stepping=pendingStep=designerPlaying=false;presentation?.ClearDesignerPreview();Effects?.ClearDesignerPreview();session.Notify();
  if(session.Game.Phase!=GamePhase.Resolving)FinishSkip();
 }
 private void FinishSkip(){presentation?.ClearDesignerPreview();Effects?.ClearDesignerPreview();IsSkipping=false;ResetRepeatSpeed();nextStep=0;session.Notify();}
}}
