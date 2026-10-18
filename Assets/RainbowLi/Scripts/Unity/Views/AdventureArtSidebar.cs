using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class AdventureArtSidebar:MonoBehaviour {
  public GameSession session;
  public Text completed,elapsed,growthCount,growthDescription,coins,deckCount,health;
  public SequenceView[] summaries;
  void LateUpdate(){
   var g=session==null?null:session.Game;if(g==null)return;
   completed.text=g.CompletedRoomCount.ToString();int s=Mathf.Max(0,Mathf.FloorToInt(session.ElapsedSeconds));elapsed.text=(s/3600).ToString("00")+":"+(s/60%60).ToString("00")+":"+(s%60).ToString("00");
   var growth=session.enemyGrowth;growthCount.text=(growth==null?0:growth.Count(g)).ToString();growthDescription.text=growth==null?"暂无敌人增益":growth.Description(g);
   coins.text=g.Coins.ToString();deckCount.text=g.Deck.Count.ToString();health.text="生命 "+g.Health+" / "+g.HealthLimit;
   for(int i=0;i<summaries.Length;i++)if(i<g.PlayerRules.Count)summaries[i].Bind(g.PlayerRules[i],false);else summaries[i].BindEmpty("未激活牌序");
  }
 }
}
