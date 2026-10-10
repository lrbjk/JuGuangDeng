using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
[DisallowMultipleComponent]
public sealed class BattlePlaybackView:MonoBehaviour {
 public BattleModule battle;
 public Button pause,step,speed,skip;
 public Text pauseLabel,speedLabel,stateLabel;
 private Button[] controls;
 private void Awake(){controls=new[]{pause,step,speed,skip};}
 private void LateUpdate(){
  if(battle==null||battle.session==null||battle.session.Game==null)return;
  bool resolving=battle.session.Game.Phase==GamePhase.Resolving;
  bool visible=resolving||battle.presentation!=null&&battle.presentation.KeepBattleVisible;
  foreach(var button in controls){button.gameObject.SetActive(visible);button.interactable=visible&&!battle.IsSkipping;}
  step.interactable=resolving&&!battle.autoPlay&&!battle.IsStepping&&!battle.IsSkipping;
  pauseLabel.text=battle.autoPlay?"暂停":"继续";
  speedLabel.text=battle.PlaybackSpeed.ToString("0.#")+"× 倍速";
  stateLabel.gameObject.SetActive(visible);
  stateLabel.text=battle.IsSkipping?"正在结算\n本回合…":battle.IsStepping?"播放单步…":battle.IsPresentationPaused?"已暂停\n可单步查看":("播放中"+(battle.RepeatSpeed>1?"\n连锁 "+battle.RepeatSpeed.ToString("0.#")+"×":""));
 }
}
}
