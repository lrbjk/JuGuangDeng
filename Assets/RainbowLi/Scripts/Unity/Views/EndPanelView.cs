using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class EndPanelView:MonoBehaviour {
 public GameView view;
 public void Refresh(ContainmentGame g){view.endTitle.text=g.Phase==GamePhase.Won?"核心实体 / 收容完成":"装置离线 / 收容失败";view.endSummary.text="完成收容 "+g.Victories+" 次 · 最终牌组 "+g.Deck.Count+" 张\n点击左侧新的一局重新开始。";}
}
}
