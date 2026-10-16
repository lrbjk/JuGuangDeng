using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
public sealed class EndPanelView:MonoBehaviour {
 public GameView view;
 public Button restart,mainMenu;
 private void Awake(){if(restart!=null)restart.onClick.AddListener(Restart);if(mainMenu!=null)mainMenu.onClick.AddListener(ReturnToMenu);}
 public void Restart(){view.session.NewGame();view.GetComponent<StartMenuView>()?.ResumeGame();}
 public void ReturnToMenu(){view.GetComponent<StartMenuView>()?.ReturnToMenu();}
 public void Refresh(ContainmentGame g){
  view.endTitle.text=g.Phase==GamePhase.Won?"核心实体 / 收容完成":"装置离线 / 收容失败";
  view.endSummary.text="已完成战斗 "+g.Victories+" 次 · 最终牌组 "+g.Deck.Count+" 张\n本次冒险已结束。";
  if(view.endPanel.activeSelf)view.endPanel.transform.SetAsLastSibling();
 }
}
}
