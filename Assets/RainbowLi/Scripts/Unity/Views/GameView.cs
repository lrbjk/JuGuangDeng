using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
public sealed class GameView : MonoBehaviour {
 public GameSession session;
 public CardModule cards; public BattleModule battle; public ProgressionModule progression;public SequenceModule sequences;public EngravingModule engravings;
 public GameObject mapPanel,battlePanel,rewardPanel,servicePanel,endPanel;
 public Text playerStats,enemyStats,roundStats,forecast,feedback,arena,enemyIntent,serviceTitle,endTitle,endSummary;
 public Slider playerHealth,enemyHealth;
 public CardView[] hand,actions,rewards;
 public Text[] actionLabels;
 public Button[] insertButtons,leftButtons,rightButtons,removeButtons,rewardButtons,buyNumberButtons,rebuildButtons;
 public Button normalTask,bossTask,restTask,confirm,pause,step,heal,generate;
 public Text pauseLabel,statusSummary;
 public SequenceView[] ruleViews;
 public RectTransform deckContent;
 public DeckRowView deckRowPrefab;
 public BattleHudView battleHud;
 public CardMotionController cardMotion;
 public BattlePanelView battleView;public RewardPanelView rewardView;public ServicePanelView serviceView;public EndPanelView endView;public TooltipView tooltip;
 private bool initialized;
 public bool IsReady => initialized;
 private void OnEnable(){session.Changed+=Refresh;}
 private void OnDisable(){session.Changed-=Refresh;}
 private void Start(){
  battleView.Initialize();rewardView.Initialize();serviceView.Initialize();
  for(int i=0;i<ruleViews.Length;i++)ruleViews[i].Initialize(sequences,i);
  initialized=true;Refresh();
 }
 public void Refresh(){
  if(!initialized||session.Game==null)return; var g=session.Game;battleView.EnsureCapacity(g);if(cardMotion!=null)cardMotion.PrepareRefresh(g);
  bool planning=g.Phase==GamePhase.Planning, fighting=planning||g.Phase==GamePhase.Resolving;
  bool holdBattle=cardMotion!=null&&cardMotion.KeepBattleVisible;
  mapPanel.SetActive(g.Phase==GamePhase.Map);battlePanel.SetActive(fighting||holdBattle);rewardPanel.SetActive(g.Phase==GamePhase.Reward&&!holdBattle);servicePanel.SetActive(g.Phase==GamePhase.Shop||g.Phase==GamePhase.Rest);endPanel.SetActive((g.Phase==GamePhase.Won||g.Phase==GamePhase.Lost)&&!holdBattle);
  if(!fighting&&tooltip!=null)tooltip.HideAll();
  battleView.Refresh(g);rewardView.Refresh(g);serviceView.Refresh(g);endView.Refresh(g);
  if(battleHud!=null)battleHud.Refresh(g);
  if(cardMotion!=null)cardMotion.Observe(g);
 }
}}
