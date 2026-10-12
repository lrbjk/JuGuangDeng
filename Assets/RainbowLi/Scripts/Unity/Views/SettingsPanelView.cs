using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class SettingsPanelView:MonoBehaviour {
  public GameView view;
  public Slider volume;
  public Text volumeValue,speedValue;
  public Button speed,close;
  public GameSettings preferences;
  public Button shake,returnMenu;
  public Text shakeValue;
  void Awake(){volume.onValueChanged.AddListener(ChangeVolume);speed.onClick.AddListener(ChangeSpeed);close.onClick.AddListener(()=>gameObject.SetActive(false));if(shake!=null)shake.onClick.AddListener(()=>{preferences.SetScreenShake(!preferences.ScreenShake);Refresh();});if(returnMenu!=null)returnMenu.onClick.AddListener(()=>view.GetComponent<StartMenuView>().ReturnToMenu());}
  void OnEnable(){transform.SetAsLastSibling();var effects=view.session.GetComponentInChildren<BattleEffects>();volume.SetValueWithoutNotify(effects!=null&&effects.audioSource!=null?effects.audioSource.volume:.3f);Refresh();}
  void Update(){
#if ENABLE_INPUT_SYSTEM
   var keyboard=UnityEngine.InputSystem.Keyboard.current;
   if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)gameObject.SetActive(false);
#elif ENABLE_LEGACY_INPUT_MANAGER
   if(Input.GetKeyDown(KeyCode.Escape))gameObject.SetActive(false);
#endif
  }
  void ChangeVolume(float value){if(preferences!=null)preferences.SetVolume(value);else view.battleHud.SetVolume(value);Refresh();}
  void ChangeSpeed(){if(preferences!=null)preferences.SetSpeed(view.battle.PlaybackSpeed<1.5f?2:view.battle.PlaybackSpeed<3?4:1);else view.battle.CycleSpeed();Refresh();}
  void Refresh(){volumeValue.text=Mathf.RoundToInt(volume.value*100)+"%";speedValue.text="播放速度  "+view.battle.PlaybackSpeed.ToString("0.#")+"×";if(shakeValue!=null)shakeValue.text="震屏  "+(preferences.ScreenShake?"开启":"关闭");if(returnMenu!=null)returnMenu.gameObject.SetActive(!view.GetComponent<StartMenuView>().IsOpen);}
 }
}
