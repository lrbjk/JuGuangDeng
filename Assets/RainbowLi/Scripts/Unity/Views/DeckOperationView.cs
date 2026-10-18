using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 public enum DeckOperation { Browse,Upgrade,Engrave,Remove,Buy,Generate,Reward,DrawPile,DiscardPile,RewardEngrave,RewardSequence }
 [DisallowMultipleComponent]
 public sealed class DeckOperationView:MonoBehaviour {
  public GameView view;
  public bool illustrated;
  public GameObject panel,confirmation;
  public RectTransform content;
  public ScrollRect scroll;
  public CardView cardPrefab,confirmationCard;
  public Text title,instruction,emptyLabel,confirmationTitle,confirmationBody,result;
  public Button exit,confirm,cancel,generate;
  public Button[] operationButtons;
  public Text completedRooms,elapsedTime,growthCount,growthDescription,coins,deckCount;
  public SequenceView[] summaries;
  [Header("卡组排列")][Min(1)] public int columns=6;
  [Min(0)] public float gap=28,padding=12;
  [Range(.5f,2f)] public float maximumCardScale=1.18f;
  readonly List<CardView> cards=new List<CardView>();
  readonly List<Card> displayed=new List<Card>();
  ContainmentGame openedGame,pendingGame;
  Card pendingCard;
  GamePhase openedPhase;
  DeckOperation pendingOperation;
  int pendingNumber;
  string pendingState,pendingDetail;
  bool initialized,executing;
  Button skipReward;
  public DeckOperation Operation {get;private set;}
  public bool IsOpen=>panel!=null&&panel.activeInHierarchy&&openedGame==view.session.Game;
  public bool HasPending=>confirmation!=null&&confirmation.activeSelf&&pendingGame!=null;
  void Awake(){InitializeControls();}
  void OnEnable(){view.session.Changed+=Refresh;}
  void OnDisable(){view.session.Changed-=Refresh;Close();}
  void Update(){if(IsOpen){int seconds=Mathf.Max(0,Mathf.FloorToInt(view.session.ElapsedSeconds));elapsedTime.text="已用时间  "+(seconds/3600).ToString("00")+":"+(seconds/60%60).ToString("00")+":"+(seconds%60).ToString("00");}}
  public void InitializeControls(){if(initialized)return;initialized=true;exit.onClick.AddListener(Close);confirm.onClick.AddListener(Confirm);cancel.onClick.AddListener(Cancel);generate.onClick.AddListener(RequestGeneration);skipReward=Instantiate(generate,generate.transform.parent);skipReward.name="SkipReward";skipReward.onClick=new Button.ButtonClickedEvent();skipReward.onClick.AddListener(SkipCurrentReward);skipReward.GetComponentInChildren<Text>().text="跳过本次卡牌奖励";skipReward.gameObject.SetActive(false);for(int i=0;i<operationButtons.Length;i++){var mode=(DeckOperation)i;operationButtons[i].onClick.AddListener(()=>Open(mode));}}
  public void Open(DeckOperation operation){
   var game=view.session.Game;if(game==null||!Allowed(operation,game.Phase))return;
   InitializeControls();view.GetComponent<SequenceEditorView>()?.Close();view.battleHud.ClosePile();Cancel();openedGame=game;openedPhase=game.Phase;Operation=operation;
   panel.SetActive(true);panel.transform.SetAsLastSibling();view.tooltip.HideAll();result.text="";scroll.StopMovement();scroll.verticalNormalizedPosition=1;Refresh();
  }
  public void OpenCard(DeckOperation operation,Card card){Open(operation);if(IsOpen&&Operation==operation)Select(card);}
  public void OpenReward(int index){var game=view.session.Game;if(game==null||index<0||index>=game.Rewards.Count)return;OpenCard(DeckOperation.Reward,game.Rewards[index]);}
  public void OpenBuy(int number){if(number<1||number>6)return;Open(DeckOperation.Buy);if(IsOpen&&Operation==DeckOperation.Buy)SelectNumber(number);}
  public void OpenGenerate(){Open(DeckOperation.Generate);if(IsOpen&&Operation==DeckOperation.Generate)RequestGeneration();}
  public void Close(){Cancel();if(panel!=null)panel.SetActive(false);openedGame=null;if(view!=null&&view.tooltip!=null)view.tooltip.HideAll();}
  public void SkipCurrentReward(){if(view.session.Game?.SkipReward()!=true)return;Cancel();view.session.Notify();if(view.session.Game.Phase==GamePhase.Reward){openedGame=null;Open(DeckOperation.Reward);}else Close();}
  public void Cancel(){if(confirmation!=null)confirmation.SetActive(false);pendingGame=null;pendingCard=null;pendingState=null;pendingDetail=null;pendingNumber=0;}
  public void Select(Card card){if(!IsOpen||Operation==DeckOperation.Generate||card==null||!displayed.Contains(card))return;BeginConfirmation(card,0);}
  public void OpenRewards(){Open(DeckOperation.Reward);}
  public void OpenSequenceReward(){Open(DeckOperation.RewardSequence);if(IsOpen)BeginConfirmation(null,0);}
  public void SelectNumber(int number){if(!IsOpen||Operation!=DeckOperation.Buy||number<1||number>6)return;BeginConfirmation(null,number);}
  public void RequestGeneration(){if(!IsOpen||Operation!=DeckOperation.Generate)return;BeginConfirmation(null,0);}
  void BeginConfirmation(Card card,int number){
   pendingGame=openedGame;pendingCard=card;pendingNumber=number;pendingOperation=Operation;pendingState=State(card);confirmation.SetActive(true);confirmation.transform.SetAsLastSibling();view.tooltip.HideAll();
   confirmationTitle.text=ReadOnly(Operation)?"卡牌详情":"确认"+Name(Operation);
   confirmationCard.gameObject.SetActive(card!=null);if(card!=null){confirmationCard.Bind(card.Number,card.Effect,card.Description,false,card.Engraved,false,false,null,card.Engraving);}
   string detail=card==null?"":card.Name+" · 数字 "+card.Number+"\n"+card.RulesText+"\n\n";
   if(Operation==DeckOperation.Upgrade){var copy=card.CopyForSnapshot();copy.Upgrade++;detail+="升级后："+copy.RulesText+"\n消耗 "+pendingGame.CardUpgradePrice+" 金币";}
   else if(Operation==DeckOperation.Engrave){var definition=view.engravings.definition;bool fixedEffect=definition!=null&&(definition.effectTable!=null&&!string.IsNullOrWhiteSpace(definition.effectId)||!pendingGame.HasConfiguredEngravings);if(fixedEffect){var template=definition.CreateEffect();detail+=template.RollX&&template.MaxX>template.MinX?"铭刻效果示例（X="+template.MinX+"）："+template.Description+"\n实际 X 将在 "+template.MinX+"～"+template.MaxX+" 内随机生成。":"将获得铭刻："+template.Description;}else detail+="将按铭刻效果池随机生成一个铭刻。";detail+="\n消耗 "+pendingGame.CardEngravingPrice+" 金币，每张牌最多铭刻一次。";}
   else if(Operation==DeckOperation.Remove)detail+="将从当前卡组删除这张卡牌。\n消耗本次休息服务，卡组至少保留 6 张。";
   else if(Operation==DeckOperation.Buy)detail="生成一张数字 "+number+" 的新卡牌并加入卡组。\n效果在确认后随机生成。\n消耗 "+pendingGame.CardPurchasePrice+" 金币。";
   else if(Operation==DeckOperation.Generate)detail="随机生成一张新卡牌并加入卡组。\n消耗本次休息服务。";
   else if(Operation==DeckOperation.RewardEngrave)detail+="按铭刻效果池为此牌获得一次铭刻，免费。\n将消耗本次奖励，不能再选择另外两种。";
   else if(Operation==DeckOperation.RewardSequence)detail="免费获得一个随机牌序效果，加入备用效果列表。\n不会自动替换已装备效果；之后在牌序调整界面装备。\n将消耗本次奖励。";
   else if(Operation==DeckOperation.Reward)detail+="将此奖励加入当前卡组，并完成本关。";
   pendingDetail=detail;string reason=Reason();confirmationBody.text=detail+(reason==null?"":"\n\n"+reason);confirm.gameObject.SetActive(!ReadOnly(Operation));confirm.interactable=reason==null&&!ReadOnly(Operation);
   cancel.GetComponentInChildren<Text>().text=ReadOnly(Operation)?"返回列表":"取消";
  }
  public void Confirm(){
   if(executing||!IsOpen||!HasPending||ReadOnly(pendingOperation))return;string reason=Reason();if(reason!=null){confirmationBody.text+="\n"+reason;confirm.interactable=false;return;}
   var game=pendingGame;var card=pendingCard;var operation=pendingOperation;int number=pendingNumber;executing=true;Cancel();bool changed=false;
   try{
    if(operation==DeckOperation.Upgrade)changed=game.BuyUpgrade(card,false);
    else if(operation==DeckOperation.Engrave)changed=view.engravings.ExecuteEngraving(card);
    else if(operation==DeckOperation.Remove){int before=game.Deck.Count;game.RestRemove(card);changed=game.Deck.Count<before;}
    else if(operation==DeckOperation.Buy)changed=game.BuyCard(number);
    else if(operation==DeckOperation.Generate){int before=game.Deck.Count;game.RestGenerate();changed=game.Deck.Count>before;}
    else if(operation==DeckOperation.Reward){int index=game.Rewards.IndexOf(card);int before=game.Deck.Count;game.TakeReward(index);changed=game.Deck.Count>before;if(changed&&view.progression.levels!=null)view.progression.levels.CompleteNode();if(changed&&game.Phase==GamePhase.Reward)pendingState=null;}
    if(operation==DeckOperation.RewardEngrave)changed=game.TakeEngravingReward(card);
    else if(operation==DeckOperation.RewardSequence)changed=game.TakeSequenceReward();
    if(changed&&(operation==DeckOperation.RewardEngrave||operation==DeckOperation.RewardSequence)){if(view.progression.levels!=null)view.progression.levels.CompleteNode();}
    if(changed&&illustrated&&(operation==DeckOperation.Reward||operation==DeckOperation.RewardEngrave||operation==DeckOperation.RewardSequence))Close();
    view.session.Notify();if(IsOpen)result.text=changed?Name(operation)+"完成":"操作未完成，请重新选择";
   }finally{executing=false;}
  }
  string Reason(){
   var game=view.session.Game;if(pendingGame==null||game!=pendingGame||game.Phase!=openedPhase||pendingOperation!=Operation)return "当前状态已变化，请重新选择。";
   if(!Allowed(pendingOperation,game.Phase))return "当前阶段无法执行此操作。";
   if(pendingCard!=null){var list=Source(game,pendingOperation);if(!list.Contains(pendingCard)||State(pendingCard)!=pendingState)return "这张卡牌已发生变化，请重新选择。";}
   if(ReadOnly(pendingOperation))return null;
   if(pendingOperation==DeckOperation.RewardEngrave){if(!game.CanClaimReward)return "奖励已领取。";if(pendingCard==null||pendingCard.Engraved)return "此卡牌已经铭刻，请选择其他卡牌。";}
   if(pendingOperation==DeckOperation.RewardSequence&&!game.CanClaimReward)return "奖励已领取。";
   if(pendingOperation==DeckOperation.Upgrade){if(!pendingCard.CanUpgrade)return "此卡牌没有可升级的效果。";if(game.Coins<game.CardUpgradePrice)return "金币不足。";}
   if(pendingOperation==DeckOperation.Engrave){if(pendingCard.Engraved)return "此卡牌已经铭刻。";if(game.Coins<game.CardEngravingPrice)return "金币不足。";}
   if(pendingOperation==DeckOperation.Buy&&game.Coins<game.CardPurchasePrice)return "金币不足。";
   if((pendingOperation==DeckOperation.Remove||pendingOperation==DeckOperation.Generate)&&game.ServiceUsed)return "本次休息服务已使用。";
   if(pendingOperation==DeckOperation.Remove&&game.Deck.Count<=6)return "卡组至少保留 6 张，无法删除。";
   return null;
  }
  public void Refresh(){
   if(panel==null||!panel.activeSelf)return;var game=view.session.Game;if(game==null||game!=openedGame||game.Phase!=openedPhase){Close();return;}
   if(HasPending){var reason=Reason();confirm.interactable=reason==null&&!ReadOnly(Operation);confirmationBody.text=pendingDetail+(reason==null?"":"\n\n"+reason);}
   var run=game.Route;completedRooms.text="已完成关卡  "+(run==null?0:run.Completed.Count);coins.text="金币  "+game.Coins;deckCount.text="卡组  "+game.Deck.Count;
   var growth=view.session.enemyGrowth;growthCount.text="敌人增益  "+(growth==null?0:growth.Count(game))+" 次";growthDescription.text=growth==null?"暂无敌人增益":growth.Description(game);
   for(int i=0;i<summaries.Length;i++){if(i<game.PlayerRules.Count)summaries[i].Bind(game.PlayerRules[i],false);else summaries[i].BindEmpty("未激活牌序");}
   title.text=Name(Operation)+" / "+(IsPile(Operation)?Source(game,Operation).Count+" 张":Operation==DeckOperation.Reward?"奖励候选 "+game.Rewards.Count+" 张":"当前卡组 "+game.Deck.Count+" 张");
   instruction.text=IsPile(Operation)?"点击卡牌查看详情。\n此列表仅供查看，不改变抽牌顺序。":ReadOnly(Operation)?"点击卡牌查看详情。\n在商店或休息中选择操作。":"选择卡牌后再次确认。\n取消不会消耗金币或服务。";
   for(int i=0;i<operationButtons.Length;i++){var mode=(DeckOperation)i;operationButtons[i].gameObject.SetActive(!illustrated&&!IsPile(Operation)&&Allowed(mode,game.Phase));operationButtons[i].interactable=mode!=Operation;}
   generate.gameObject.SetActive(Operation==DeckOperation.Generate);generate.interactable=!game.ServiceUsed;
   skipReward.gameObject.SetActive(Operation==DeckOperation.Reward&&game.CanSkipReward);
   displayed.Clear();if(Operation==DeckOperation.Reward)displayed.AddRange(game.Rewards);else if(Operation!=DeckOperation.Buy)displayed.AddRange(Source(game,Operation).OrderBy(c=>c.Number));
   int total=Operation==DeckOperation.RewardSequence?0:Operation==DeckOperation.Buy?6:displayed.Count;while(cards.Count<total){var card=Instantiate(cardPrefab,content);card.name="DeckChoice_"+cards.Count;cards.Add(card);}
   emptyLabel.text=IsPile(Operation)?Name(Operation)+"暂无卡牌":"暂无卡牌";emptyLabel.gameObject.SetActive(total==0);int count=Mathf.Max(1,columns);float scale=Mathf.Clamp((scroll.viewport.rect.width-padding*2-gap*(count-1))/(130*count),.1f,maximumCardScale);float width=130*scale,height=225*scale;int rowCount=(total+count-1)/count;
   content.sizeDelta=new Vector2(scroll.viewport.rect.width,Mathf.Max(scroll.viewport.rect.height,padding*2+rowCount*height+Mathf.Max(0,rowCount-1)*gap));
   for(int i=0;i<cards.Count;i++){
    var tile=cards[i];tile.gameObject.SetActive(i<total);if(i>=total)continue;
    if(Operation==DeckOperation.Buy){int number=i+1;tile.Bind(number,CardEffect.RandomNumbers,"随机效果 / 确认后生成",false,false,false,true,()=>SelectNumber(number));tile.SetDetails(view.tooltip,"购买数字 "+number,"确认后按配表生成随机效果，消耗 "+game.CardPurchasePrice+" 金币。");}
    else{var card=displayed[i];tile.Bind(card.Number,card.Effect,card.Description+(card.Upgrade>0?" · 升级 "+card.Upgrade:""),false,card.Engraved,false,Operation!=DeckOperation.Generate,()=>Select(card),card.Engraving);tile.SetDetails(view.tooltip,card.Name+" · 数字 "+card.Number,card.RulesText);}
    tile.SetLayout(new Vector2(padding+i%count*(width+gap),-padding-i/count*(height+gap)),scale);
   }
  }
  static bool IsPile(DeckOperation operation)=>operation==DeckOperation.DrawPile||operation==DeckOperation.DiscardPile;
  static bool ReadOnly(DeckOperation operation)=>operation==DeckOperation.Browse||IsPile(operation);
  static IReadOnlyList<Card> Source(ContainmentGame game,DeckOperation operation)=>operation==DeckOperation.DrawPile?game.DrawPileCards:operation==DeckOperation.DiscardPile?game.DiscardCards:operation==DeckOperation.Reward?game.Rewards:game.Deck;
  static string State(Card card)=>card==null?null:card.InstanceId+"|"+card.Upgrade+"|"+card.Engraved+"|"+card.RulesText;
  public static bool Allowed(DeckOperation operation,GamePhase phase){if(ReadOnly(operation))return true;if(operation==DeckOperation.Upgrade||operation==DeckOperation.Engrave||operation==DeckOperation.Buy)return phase==GamePhase.Shop;if(operation==DeckOperation.Remove||operation==DeckOperation.Generate)return phase==GamePhase.Rest;return (operation==DeckOperation.Reward||operation==DeckOperation.RewardEngrave||operation==DeckOperation.RewardSequence)&&phase==GamePhase.Reward;}
  public static string Name(DeckOperation operation)=>new[]{"查看卡组","卡牌升级","卡牌铭刻","删除卡牌","购买卡牌","生成卡牌","选择奖励","抽牌堆","弃牌堆","奖励铭刻","牌序效果奖励"}[(int)operation];
 }
}
