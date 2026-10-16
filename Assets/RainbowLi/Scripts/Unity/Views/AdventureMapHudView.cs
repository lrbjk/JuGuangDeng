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

  [Header("选关美术")]
  public bool illustrated;
  public Text illustratedCoins,illustratedDeck;
  private void LateUpdate(){if(illustrated&&session.Game!=null){if(illustratedCoins!=null)illustratedCoins.text=session.Game.Coins.ToString();if(illustratedDeck!=null)illustratedDeck.text=session.Game.Deck.Count.ToString();}}
  public Image bossArtwork;
  public Sprite bossLockedSprite,bossUnlockedSprite,markerSprite,checkSprite;
  public RectTransform markerStrip;
  public void RefreshArtwork(){
   var run=levels.Run;if(!illustrated||run==null)return;
   bossArtwork.sprite=run.BossUnlocked?bossUnlockedSprite:bossLockedSprite;
   markerStrip.gameObject.SetActive(!run.BossUnlocked);
   int count=Mathf.Max(1,run.BossRequiredBattles);
   var markers=new System.Collections.Generic.List<Image>(battleMarkers??new Image[0]);
   while(markers.Count<count){var image=Instantiate(markers[0],markerStrip);image.name="BattleMarker_"+markers.Count;markers.Add(image);}
   battleMarkers=markers.ToArray();
   float gap=Mathf.Min(137,740f/Mathf.Max(1,count-1)),size=Mathf.Min(74,gap-6),start=(823-gap*(count-1)-size)*.5f;
   for(int i=0;i<markers.Count;i++){
    var image=markers[i];image.gameObject.SetActive(i<count);if(i>=count)continue;
    image.sprite=markerSprite;image.color=Color.white;image.raycastTarget=false;
    var r=image.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.sizeDelta=Vector2.one*size;r.anchoredPosition=new Vector2(start+i*gap,(size-94)*.5f);
    var label=image.GetComponentInChildren<Text>(true);if(label!=null)label.gameObject.SetActive(false);
    var check=image.transform.Find("Check");if(check==null){var go=new GameObject("Check",typeof(RectTransform),typeof(Image));go.transform.SetParent(image.transform,false);check=go.transform;var cr=(RectTransform)check;cr.anchorMin=Vector2.zero;cr.anchorMax=Vector2.one;cr.offsetMin=cr.offsetMax=Vector2.zero;}
    var tick=check.GetComponent<Image>();tick.sprite=checkSprite;tick.color=Color.white;tick.raycastTarget=false;check.gameObject.SetActive(i<run.CompletedBattleCount);
   }
  }
  private void OnEnable(){session.Changed+=Refresh;Refresh();}
  private void OnDisable(){session.Changed-=Refresh;}
  private void Start(){for(int i=0;i<playerSequences.Length;i++)playerSequences[i].Initialize(sequences,i);Refresh();}
  private void Update(){if(elapsedTime!=null){int seconds=Mathf.Max(0,Mathf.FloorToInt(session.ElapsedSeconds));elapsedTime.text=(illustrated?"":"已用时间  ")+(seconds/3600).ToString("00")+":"+(seconds/60%60).ToString("00")+":"+(seconds%60).ToString("00");}}
  public void Refresh(){
   var game=session.Game;var run=levels.Run;if(game==null||run==null)return;
   completedRooms.text=(illustrated?"":"已完成关卡  ")+run.Completed.Count+(run.Graph.IndependentStacks?"": " / "+run.Graph.Floors);
   growthCount.text=(illustrated?"":"敌人增益  ")+(growth==null?0:growth.Count(game))+" 次";
   growthDescription.text=growth==null?"暂无敌人冒险增益":growth.Description(game);
   battleCount.text="已完成战斗  "+game.Victories+" 次";
   var boss=run.Graph.Nodes.FirstOrDefault(n=>n.kind==LevelKind.Boss);
   bossButton.interactable=game.Phase==GamePhase.Map&&boss!=null&&run.CanEnter(boss.id);
   bossState.text=run.BossUnlocked?"BOSS 已解锁 · 可随时挑战":"再完成 "+Mathf.Max(0,run.BossRequiredBattles-run.CompletedBattleCount)+" 场战斗解锁";
   if(illustrated){completedRooms.text=run.Completed.Count.ToString();growthCount.text=(growth==null?0:growth.Count(game)).ToString();var map=GetComponent<RouteMapView>();if(map!=null)map.resources.text=game.Coins.ToString();}
   RefreshArtwork();
   if(!illustrated)for(int i=0;i<battleMarkers.Length;i++){
    // Show consecutive battle numbers, with one upcoming battle after the visible history.
    long threshold=System.Math.Max(0L,(long)game.Victories-battleMarkers.Length+1)+i+1;
    battleMarkers[i].color=game.Victories>=threshold?new Color(.92f,.75f,.4f):new Color(.12f,.24f,.21f);
    var label=battleMarkers[i].GetComponentInChildren<Text>();if(label!=null)label.text=threshold.ToString();
   }
   for(int i=0;i<playerSequences.Length;i++){if(i<game.PlayerRules.Count)playerSequences[i].Bind(game.PlayerRules[i],false);else playerSequences[i].BindEmpty("未激活牌序");var entry=playerSequences[i].GetComponent<Button>();if(entry!=null)entry.interactable=game.Phase==GamePhase.Map&&i<game.PlayerRules.Count;}
  }
  public void EnterBoss(){levels.SelectBoss();}
 }
}
