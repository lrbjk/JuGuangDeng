using UnityEngine;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class GameMusicController:MonoBehaviour {
  public GameView view;
  public GameAudioController audioController;
  public GameMusicProfile profile;
  [Tooltip("专用2D循环音源，不与按钮/打击音效共用。")] public AudioSource first,second;
  public AudioClip CurrentClip {get;private set;}
  readonly float[] gains=new float[2];
  int active=-1;
  void OnEnable(){Configure(first);Configure(second);CurrentClip=null;active=-1;gains[0]=gains[1]=0;}
  void OnDisable(){first?.Stop();second?.Stop();if(first!=null)first.volume=0;if(second!=null)second.volume=0;gains[0]=gains[1]=0;active=-1;CurrentClip=null;}
  static void Configure(AudioSource source){if(source==null)return;source.playOnAwake=false;source.loop=true;source.spatialBlend=0;source.pitch=1;source.volume=0;}
  void LateUpdate(){RefreshMusic(Time.unscaledDeltaTime);}
  public AudioClip SelectClip(){
   if(profile==null)return null;
   var game=view==null?null:view.session?.Game;
   bool menu=view!=null&&view.GetComponent<StartMenuView>()?.IsOpen==true;
   bool battle=!menu&&game!=null&&(game.Phase==GamePhase.Planning||game.Phase==GamePhase.Resolving);
   return battle?profile.Resolve(game.Enemy.Encounter?.Definition?.Id,game.Boss):profile.map;
  }
  // One pair of sources survives room/UI refreshes. Repeated notifications never restart a track.
  public void RefreshMusic(float elapsed){
   var target=SelectClip();
   if(target!=CurrentClip){
    if(target==null){active=-1;CurrentClip=null;}
    else {
     int next=first!=null&&first.clip==target&&first.isPlaying?0:second!=null&&second.clip==target&&second.isPlaying?1:active==0?1:0;
     var source=next==0?first:second;
     if(source!=null){if(source.clip!=target||!source.isPlaying){source.Stop();source.clip=target;gains[next]=0;source.volume=0;source.Play();}active=next;CurrentClip=target;}
    }
   }
   float step=profile==null||profile.fadeSeconds<=0?1:Mathf.Max(0,elapsed)/profile.fadeSeconds;
   float master=audioController!=null&&audioController.uiSource!=null?audioController.uiSource.volume:.3f;
   for(int i=0;i<2;i++){
    var source=i==0?first:second;if(source==null)continue;
    gains[i]=Mathf.MoveTowards(gains[i],i==active?1:0,step);source.volume=Mathf.Clamp01(master)*Mathf.Clamp01(profile==null?0:profile.volume)*gains[i];
    if(i!=active&&gains[i]<=0&&source.isPlaying)source.Stop();
   }
  }
 }
}
