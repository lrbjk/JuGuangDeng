using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class AdventureMapHudView : MonoBehaviour {
  public GameSession session;
  public LevelModule levels;
  public SequenceModule sequences;
  public EnemyGrowthModule growth;
  public Text completedRooms,elapsedTime,growthCount,growthDescription,battleCount,bossState;
  public Button bossButton;
  public Image[] battleMarkers;
  public SequenceView[] playerSequences;

  private void OnEnable(){session.Changed+=Refresh;Refresh();}
  private void OnDisable(){session.Changed-=Refresh;}
  private void Start(){for(int i=0;i<playerSequences.Length;i++)playerSequences[i].Initialize(sequences,i);Refresh();}
  private void Update(){if(elapsedTime!=null){int seconds=Mathf.Max(0,Mathf.FloorToInt(session.ElapsedSeconds));elapsedTime.text="已用时间  "+(seconds/3600).ToString("00")+":"+(seconds/60%60).ToString("00")+":"+(seconds%60).ToString("00");}}
  public void Refresh(){
   var game=session.Game;var run=levels.Run;if(game==null||run==null)return;
   completedRooms.text="已完成关卡  "+run.Completed.Count+(run.Graph.IndependentStacks?"": " / "+run.Graph.Floors);
   growthCount.text="敌人增益  "+(growth==null?0:growth.Count(game))+" 次";
   growthDescription.text=growth==null?"暂无敌人冒险增益":growth.Description(game);
   battleCount.text="战斗进度  "+run.CompletedBattleCount+" / "+run.BossRequiredBattles+"  ·  第 "+run.AdventureLayer+" 层";
   var boss=run.Graph.Nodes.FirstOrDefault(n=>n.kind==LevelKind.Boss);
   bossButton.interactable=game.Phase==GamePhase.Map&&boss!=null&&run.CanEnter(boss.id);
   bossState.text=run.BossUnlocked?"BOSS 已解锁 · 可随时挑战":"再完成 "+Mathf.Max(0,run.BossRequiredBattles-run.CompletedBattleCount)+" 场战斗解锁";
   for(int i=0;i<battleMarkers.Length;i++){
    int threshold=Mathf.CeilToInt((i+1)*run.BossRequiredBattles/(float)battleMarkers.Length);
    battleMarkers[i].color=run.CompletedBattleCount>=threshold?new Color(.92f,.75f,.4f):new Color(.12f,.24f,.21f);
    var label=battleMarkers[i].GetComponentInChildren<Text>();if(label!=null)label.text=threshold.ToString();
   }
   for(int i=0;i<playerSequences.Length;i++){if(i<game.PlayerRules.Count)playerSequences[i].Bind(game.PlayerRules[i],false);else playerSequences[i].BindEmpty("未激活牌序");var entry=playerSequences[i].GetComponent<Button>();if(entry!=null)entry.interactable=game.Phase==GamePhase.Map&&i<game.PlayerRules.Count;}
  }
  public void EnterBoss(){levels.SelectBoss();}
 }
}
