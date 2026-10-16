using UnityEngine;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class BattleArtView : MonoBehaviour {
  public GameView view;
  [Tooltip("美术背景，仅战斗与战斗结算演出期间显示。")] public GameObject backdrop;
  [Tooltip("战斗专用侧栏，仅战斗和结算演出期间显示。")] public GameObject sidebar;
  public GameObject battleSettings;
  public void Refresh(){if(view==null)return;if(backdrop!=null)backdrop.SetActive(view.battlePanel.activeSelf);if(sidebar!=null)sidebar.SetActive(view.battlePanel.activeSelf);if(battleSettings!=null)battleSettings.SetActive(view.battlePanel.activeSelf);}
 }
}
