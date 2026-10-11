using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class SettingsPanelView:MonoBehaviour {
  public GameView view;
  public Slider volume;
  public Text volumeValue,speedValue;
  public Button speed,close;
  void Awake(){volume.onValueChanged.AddListener(ChangeVolume);speed.onClick.AddListener(ChangeSpeed);close.onClick.AddListener(()=>gameObject.SetActive(false));}
  void OnEnable(){transform.SetAsLastSibling();var effects=view.session.GetComponentInChildren<BattleEffects>();volume.SetValueWithoutNotify(effects!=null&&effects.audioSource!=null?effects.audioSource.volume:.3f);Refresh();}
  void Update(){if(Input.GetKeyDown(KeyCode.Escape))gameObject.SetActive(false);}
  void ChangeVolume(float value){view.battleHud.SetVolume(value);Refresh();}
  void ChangeSpeed(){view.battle.CycleSpeed();Refresh();}
  void Refresh(){volumeValue.text=Mathf.RoundToInt(volume.value*100)+"%";speedValue.text="播放速度  "+view.battle.PlaybackSpeed.ToString("0.#")+"×";}
 }
}
