using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
public sealed class EndPanelView:MonoBehaviour {
 public GameView view;
 public Button restart,mainMenu;
 public EndScreenCopy copy;
 public Text statistics;
 private void Awake(){if(restart!=null)restart.onClick.AddListener(Restart);if(mainMenu!=null)mainMenu.onClick.AddListener(ReturnToMenu);}
 public void Restart(){view.session.NewGame();view.GetComponent<StartMenuView>()?.ResumeGame();}
 public void ReturnToMenu(){view.GetComponent<StartMenuView>()?.ReturnToMenu();}
 public void Refresh(ContainmentGame g){
  bool won=g.Phase==GamePhase.Won;
  string stats="已完成战斗 "+g.Victories+" 次 · 最终牌组 "+g.Deck.Count+" 张";
  view.endTitle.text=copy!=null?(won?copy.successTitle:copy.failureTitle):won?"核心实体 / 收容完成":"装置离线 / 收容失败";
  view.endSummary.text=copy!=null?(won?copy.successBody:copy.failureBody):stats+"\n本次冒险已结束。";
  if(statistics!=null){statistics.gameObject.SetActive(copy!=null);statistics.text=stats;}
  if(view.endPanel.activeSelf)view.endPanel.transform.SetAsLastSibling();
 }
}
}
