using UnityEngine;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class GameSettings:MonoBehaviour {
  public GameView view;
  [Tooltip("玩家偏好键前缀，与本局存档独立。")] public string keyPrefix="RainbowLi.Settings.";
  public float Volume {get;private set;}=.3f;
  public float Speed {get;private set;}=1;
  public bool ScreenShake {get;private set;}=true;
  bool loaded;
  void OnEnable(){view.session.Changed+=ObserveSpeed;}
  void OnDisable(){view.session.Changed-=ObserveSpeed;}
  void Start(){Load();}
  public void Load(){loaded=false;Volume=Finite(PlayerPrefs.GetFloat(keyPrefix+"Volume",.3f),.3f,0,1);Speed=Finite(PlayerPrefs.GetFloat(keyPrefix+"Speed",1),1,.25f,8);ScreenShake=PlayerPrefs.GetInt(keyPrefix+"Shake",1)!=0;view.battleHud.SetVolume(Volume);view.battle.SetSpeed(Speed);ApplyShake();loaded=true;}
  public void SetVolume(float value){Volume=Finite(value,.3f,0,1);view.battleHud.SetVolume(Volume);Save();}
  public void SetSpeed(float value){Speed=Finite(value,1,.25f,8);view.battle.SetSpeed(Speed);Save();}
  public void SetScreenShake(bool enabled){ScreenShake=enabled;ApplyShake();Save();}
  void ApplyShake(){var effects=view.session.GetComponentInChildren<BattleEffects>();if(effects!=null)effects.playerScreenShake=ScreenShake;}
  void ObserveSpeed(){if(!loaded||Mathf.Approximately(Speed,view.battle.PlaybackSpeed))return;Speed=view.battle.PlaybackSpeed;Save();}
  void Save(){PlayerPrefs.SetFloat(keyPrefix+"Volume",Volume);PlayerPrefs.SetFloat(keyPrefix+"Speed",Speed);PlayerPrefs.SetInt(keyPrefix+"Shake",ScreenShake?1:0);PlayerPrefs.Save();}
  static float Finite(float value,float fallback,float min,float max)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp(value,min,max);
 }
}
