using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class NonCombatRoomView:MonoBehaviour {
  public GameView view;
  public GameObject backdrop;
  public Text roomTitle,progress,health,resources,elapsed;
  public Text[] sequenceSummaries;
  public Button deck,settings;
  private void Awake(){deck.onClick.AddListener(()=>view.GetComponent<DeckOperationView>().Open(DeckOperation.Browse));settings.onClick.AddListener(view.battleHud.ToggleSettings);}
  private void Update(){if(backdrop!=null&&backdrop.activeSelf)RefreshTime();}
  private void RefreshTime(){int seconds=Mathf.Max(0,Mathf.FloorToInt(view.session.ElapsedSeconds));elapsed.text="已用时间  "+(seconds/3600).ToString("00")+":"+(seconds/60%60).ToString("00")+":"+(seconds%60).ToString("00");}
  public void Refresh(){
   if(view==null||view.session.Game==null)return;var game=view.session.Game;
   bool active=game.Phase==GamePhase.Shop||game.Phase==GamePhase.Rest||game.Phase==GamePhase.Event;
   backdrop.SetActive(active);if(!active)return;
   roomTitle.text=game.Phase==GamePhase.Shop?"档案交易":game.Phase==GamePhase.Rest?"维护装置":"档案遗址";
   progress.text="已完成关卡  "+game.CompletedRoomCount+"\n已完成战斗  "+game.Victories+" 次";
   health.text="生命  "+game.Health+" / "+game.HealthLimit;
   resources.text="金币  "+game.Coins+"    牌组  "+game.Deck.Count;
   for(int i=0;i<sequenceSummaries.Length;i++)sequenceSummaries[i].text=i<game.PlayerRules.Count?game.PlayerRules[i].Name+"\n"+game.PlayerRules[i].Pattern+"\n"+game.PlayerRules[i].Description:"未激活牌序";
   RefreshTime();
  }
 }
}
