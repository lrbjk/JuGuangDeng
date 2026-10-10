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
 private bool initialized;
 public bool IsReady => initialized;
 private void OnEnable(){session.Changed+=Refresh;}
 private void OnDisable(){session.Changed-=Refresh;}
 private void Start(){
  for(int i=0;i<insertButtons.Length;i++){int n=i;insertButtons[i].onClick.AddListener(()=>cards.Insert(n));}
  for(int i=0;i<actions.Length;i++){int n=i;leftButtons[i].onClick.AddListener(()=>cards.Move(n,-1));rightButtons[i].onClick.AddListener(()=>cards.Move(n,1));removeButtons[i].onClick.AddListener(()=>cards.Remove(n));}
  for(int i=0;i<rewards.Length;i++){int n=i;rewardButtons[i].onClick.AddListener(()=>progression.Reward(n));}
  for(int i=0;i<ruleViews.Length;i++){ruleViews[i].Initialize(sequences,i);int n=i;rebuildButtons[i].onClick.AddListener(()=>{if(session.Game.Phase==GamePhase.Shop)sequences.BuyEffect(n);else sequences.Rebuild(n);});}
  for(int i=0;i<buyNumberButtons.Length;i++){int n=i+1;buyNumberButtons[i].onClick.AddListener(()=>progression.BuyCard(n));}
  initialized=true;Refresh();
 }
 private void EnsureCapacity(ContainmentGame g){
  if(g.Hand.Count>hand.Length){var list=hand.ToList();while(list.Count<g.HandLimit){var card=Instantiate(hand[0],hand[0].transform.parent);card.name="HandCard_"+list.Count;list.Add(card);}hand=list.ToArray();for(int i=0;i<hand.Length;i++)hand[i].SetLayout(new Vector2(25+i*115,-344),.8f);}
  if(g.Timeline.Count>actions.Length){int count=g.Timeline.Count;var cs=actions.ToList();var labels=actionLabels.ToList();var left=leftButtons.ToList();var right=rightButtons.ToList();var remove=removeButtons.ToList();var inserts=insertButtons.ToList();
   while(cs.Count<count){int n=cs.Count;var c=Instantiate(actions[0],actions[0].transform.parent);c.name="ActionCard_"+n;cs.Add(c);labels.Add(Instantiate(actionLabels[0],actionLabels[0].transform.parent));var l=Instantiate(leftButtons[0],leftButtons[0].transform.parent);l.onClick=new Button.ButtonClickedEvent();l.onClick.AddListener(()=>cards.Move(n,-1));left.Add(l);var rr=Instantiate(rightButtons[0],rightButtons[0].transform.parent);rr.onClick=new Button.ButtonClickedEvent();rr.onClick.AddListener(()=>cards.Move(n,1));right.Add(rr);var x=Instantiate(removeButtons[0],removeButtons[0].transform.parent);x.onClick=new Button.ButtonClickedEvent();x.onClick.AddListener(()=>cards.Remove(n));remove.Add(x);}
   while(inserts.Count<=count){int n=inserts.Count;var b=Instantiate(insertButtons[0],insertButtons[0].transform.parent);b.onClick=new Button.ButtonClickedEvent();b.onClick.AddListener(()=>cards.Insert(n));inserts.Add(b);}actions=cs.ToArray();actionLabels=labels.ToArray();leftButtons=left.ToArray();rightButtons=right.ToArray();removeButtons=remove.ToArray();insertButtons=inserts.ToArray();
   float spacing=1010f/count;for(int i=0;i<count;i++){float x=36+i*spacing;actions[i].SetLayout(new Vector2(x,-85),Mathf.Min(.82f,spacing/140));((RectTransform)actionLabels[i].transform).anchoredPosition=new Vector2(x,-50);((RectTransform)leftButtons[i].transform).anchoredPosition=new Vector2(x,-288);((RectTransform)rightButtons[i].transform).anchoredPosition=new Vector2(x+32,-288);((RectTransform)removeButtons[i].transform).anchoredPosition=new Vector2(x+64,-288);}for(int i=0;i<inserts.Count;i++)((RectTransform)inserts[i].transform).anchoredPosition=new Vector2(8+i*spacing,-145);
  }
 }
 public void Refresh(){
  if(!initialized||session.Game==null)return; var g=session.Game;EnsureCapacity(g);if(cardMotion!=null)cardMotion.PrepareRefresh(g);
  bool planning=g.Phase==GamePhase.Planning, fighting=planning||g.Phase==GamePhase.Resolving, shop=g.Phase==GamePhase.Shop;
  bool holdBattle=cardMotion!=null&&cardMotion.KeepBattleVisible;
  mapPanel.SetActive(g.Phase==GamePhase.Map);battlePanel.SetActive(fighting||holdBattle);rewardPanel.SetActive(g.Phase==GamePhase.Reward&&!holdBattle);servicePanel.SetActive(shop||g.Phase==GamePhase.Rest);endPanel.SetActive((g.Phase==GamePhase.Won||g.Phase==GamePhase.Lost)&&!holdBattle);
  playerHealth.maxValue=g.HealthLimit;playerHealth.value=g.Health;enemyHealth.maxValue=Mathf.Max(1,g.EnemyMaxHealth);enemyHealth.value=g.EnemyHealth;
  playerStats.text="玩家 / 攻击 "+g.BaseAttack+"\n生命 "+g.Health+" / "+g.HealthLimit+"   护盾 "+g.Shield+"   增伤 "+g.Power;
  enemyStats.text=fighting?g.EnemyName+"\n生命 "+g.EnemyHealth+" / "+g.EnemyMaxHealth+"   护盾 "+g.EnemyShield+"   增伤 "+g.EnemyPower:"完成收容 "+g.Victories+" 次";
  roundStats.text="回合 "+g.Round+"   行动 "+g.PlayerActionCount+" / "+g.PlayerActionLimit+"\n金币 "+g.Coins+"   牌组 "+g.Deck.Count;
  feedback.text=g.LastAction+"\n"+g.Message;
  if(statusSummary!=null)statusSummary.text="你："+(g.Player.Statuses.Any?g.Player.Statuses.Brief:"无状态")+"\n敌方："+(g.Enemy.Statuses.Any?g.Enemy.Statuses.Brief:"无状态");
  var predicted=planning?g.Forecast():null;
  forecast.text=predicted==null?"数字流："+string.Join(" → ",g.NumberStream.Skip(Mathf.Max(0,g.NumberStream.Count-12))):"预计：你 "+predicted.Health+" / 盾 "+predicted.Shield+"\n敌人 "+predicted.EnemyHealth+" / 盾 "+predicted.EnemyShield+"\n数字："+string.Join("→",predicted.Numbers.Take(6));
  normalTask.interactable=true;bossTask.interactable=g.Victories>=2;restTask.interactable=g.RestAvailable;
  confirm.gameObject.SetActive(planning);confirm.interactable=g.CanConfirm;pause.gameObject.SetActive(!planning);step.gameObject.SetActive(!planning);step.interactable=!battle.autoPlay;pauseLabel.text=battle.autoPlay?"暂停":"继续";
  for(int i=0;i<ruleViews.Length;i++){ruleViews[i].gameObject.SetActive(i<g.PlayerRules.Count);if(i<g.PlayerRules.Count)ruleViews[i].Bind(g.PlayerRules[i],g.Phase==GamePhase.Map);}
  if(fighting){var r=g.ArenaRules[g.ArenaIndex];arena.text="场地牌序："+r.Pattern+"\n"+r.Description+" · 最后一个数字所属方触发";enemyIntent.text="敌方牌序："+string.Join(" / ",g.Enemy.ActiveRules.Select(rule=>rule.Pattern+" "+rule.Description));}
  for(int i=0;i<hand.Length;i++){
   hand[i].gameObject.SetActive(i<g.Hand.Count);if(i>=g.Hand.Count)continue;var c=g.Hand[i];bool placed=g.Timeline.Any(a=>a.Card==c);
   hand[i].Bind(c.Number,c.Effect,c.Description,false,c.Engraved,cards.Selected==c,planning&&!placed&&g.PlayerActionCount<g.PlayerActionLimit,()=>cards.Select(c));
  }
  for(int i=0;i<actions.Length;i++){
   bool exists=i<g.Timeline.Count;actions[i].gameObject.SetActive(exists);actionLabels[i].gameObject.SetActive(exists);
   leftButtons[i].gameObject.SetActive(exists&&planning);rightButtons[i].gameObject.SetActive(exists&&planning);removeButtons[i].gameObject.SetActive(exists&&planning);
   if(!exists)continue;var a=g.Timeline[i];actions[i].Bind(a.Number,a.Effect,a.Card==null?(a.Special?"特殊 / ":"")+CombatStatuses.Name(a.Effect)+" "+a.Value:a.Card.Description,a.Enemy,a.Card!=null&&a.Card.Engraved,false,false,null);
   actionLabels[i].text=(i+1)+" / "+(a.Enemy?"敌人":"你");leftButtons[i].interactable=i>0;rightButtons[i].interactable=i<g.Timeline.Count-1;removeButtons[i].interactable=!a.Enemy;
  }
  for(int i=0;i<insertButtons.Length;i++){insertButtons[i].gameObject.SetActive(planning&&i<=g.Timeline.Count);insertButtons[i].interactable=cards.Selected!=null&&g.PlayerActionCount<g.PlayerActionLimit;}
  for(int i=0;i<rewards.Length;i++){bool exists=i<g.Rewards.Count;rewards[i].gameObject.SetActive(exists);rewardButtons[i].gameObject.SetActive(exists);if(exists){var c=g.Rewards[i];rewards[i].Bind(c.Number,c.Effect,c.Description,false,c.Engraved,false,false,null);}}
  if(servicePanel.activeSelf){
   serviceTitle.text=shop?"档案交易 / 升级与铭刻":"维护装置 / 每场之间执行一次";
   heal.gameObject.SetActive(!shop);generate.gameObject.SetActive(!shop);heal.interactable=generate.interactable=!g.ServiceUsed;
   foreach(var b in buyNumberButtons){b.gameObject.SetActive(shop);b.interactable=g.Coins>=15;}
   for(int i=0;i<rebuildButtons.Length;i++){rebuildButtons[i].interactable=shop?g.Coins>=18:!g.ServiceUsed;rebuildButtons[i].GetComponentInChildren<Text>().text=shop?"牌序 "+(i+1)+" 换效果 / 18 金币":"重构牌序 "+(i+1);}
   foreach(Transform child in deckContent){child.gameObject.SetActive(false);Destroy(child.gameObject);}
   for(int i=0;i<g.Deck.Count;i++){var row=Instantiate(deckRowPrefab,deckContent);row.Bind(g.Deck[i],i,session,engravings,progression);}
  }
  endTitle.text=g.Phase==GamePhase.Won?"核心实体 / 收容完成":"装置离线 / 收容失败";endSummary.text="完成收容 "+g.Victories+" 次 · 最终牌组 "+g.Deck.Count+" 张\n点击左侧新的一局重新开始。";
  if(battleHud!=null)battleHud.Refresh(g);
  if(cardMotion!=null)cardMotion.Observe(g);
 }
}}
