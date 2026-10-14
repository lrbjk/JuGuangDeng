using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
[DisallowMultipleComponent]
public sealed class BattleHudView : MonoBehaviour {
 public GameView view;
 public RectTransform handArea, actionArea;
 public Image enemyPortrait;
 public Text portraitPlaceholder,enemyName,playerShield,enemyShield,coins,deckCount,drawCount,discardCount,turnLabel;
 public Text[] enemyRules;
 public SequenceView[] enemySequenceViews;
 public SequenceView arenaSequenceView;
 public CardView[] enemySkills;
 public GameObject settingsPanel,pilePanel;
 public Text pileTitle,pileBody,speedLabel;
 public DeckBrowserView deckBrowser;
 public Text mapDeckCount;
 public Slider volume;
 public BattleStatusPanelView playerStatuses,enemyStatuses;
 public ScrollRect handScroll,actionScroll;
 public PokerActionLayout pokerLayout;
 [Range(.1f,1.2f)] public float handScale=.88f,actionScale=.57f;
 [Min(0)] public float cardGap=12;
 private int displayedPile=-1;
 private Sprite initialPortrait;
 private bool portraitInitialized;
 public void RefreshEnemyPortrait(ContainmentGame game){
  if(enemyPortrait==null)return;
  if(!portraitInitialized){initialPortrait=enemyPortrait.sprite;portraitInitialized=true;}
  var encounter=game?.Enemy.Encounter?.Definition;
  var definition=view?.session?.enemy?.definition??view?.session?.definition?.enemy;
  enemyPortrait.sprite=encounter==null?initialPortrait:definition?.GetPortrait(encounter.Id,encounter.Boss);
  enemyPortrait.preserveAspect=true;enemyPortrait.raycastTarget=false;
  enemyPortrait.enabled=enemyPortrait.sprite!=null;
  if(portraitPlaceholder!=null)portraitPlaceholder.gameObject.SetActive(enemyPortrait.sprite==null);
 }
 private ScrollRect enemySequenceScroll;
 private void EnsureEnemySequences(int count){
  if(enemySequenceViews==null||enemySequenceViews.Length==0||enemySequenceViews[0]==null||count<=enemySequenceViews.Length)return;
  var rows=enemySequenceViews.ToList();var parent=(RectTransform)rows[0].transform.parent;
  if(enemySequenceScroll==null){
   var viewport=new GameObject("EnemySequenceScroll",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));viewport.transform.SetParent(parent,false);
   var rect=(RectTransform)viewport.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(12,-44);rect.sizeDelta=new Vector2(parent.rect.width-24,parent.rect.height-50);viewport.GetComponent<Image>().color=Color.clear;
   var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(rect,false);var contentRect=(RectTransform)content.transform;contentRect.anchorMin=new Vector2(0,1);contentRect.anchorMax=new Vector2(1,1);contentRect.pivot=new Vector2(.5f,1);contentRect.anchoredPosition=Vector2.zero;
   enemySequenceScroll=viewport.GetComponent<ScrollRect>();enemySequenceScroll.viewport=rect;enemySequenceScroll.content=contentRect;enemySequenceScroll.horizontal=false;enemySequenceScroll.movementType=ScrollRect.MovementType.Clamped;enemySequenceScroll.scrollSensitivity=35;
   foreach(var row in rows)row.transform.SetParent(contentRect,false);
  }
  while(rows.Count<count){var row=Instantiate(rows[0],enemySequenceScroll.content);row.name="EnemySequenceView_"+rows.Count;rows.Add(row);}
  for(int i=0;i<rows.Count;i++){var rect=(RectTransform)rows[i].transform;rect.anchoredPosition=new Vector2(0,-i*82);}
  enemySequenceScroll.content.sizeDelta=new Vector2(0,rows.Count*82);enemySequenceViews=rows.ToArray();
 }
 public void Refresh(ContainmentGame g) {
  if(playerStatuses!=null)playerStatuses.Refresh(g.Player.Statuses);
  if(enemyStatuses!=null)enemyStatuses.Refresh(g.Enemy.Statuses);
  if(speedLabel!=null)speedLabel.text="播放速度  "+view.battle.PlaybackSpeed.ToString("0.#")+"×";
  enemyName.text=g.EnemyName;
  RefreshEnemyPortrait(g);
  playerShield.text="护盾\n"+g.Shield;enemyShield.text="护盾\n"+g.EnemyShield;
  view.playerStats.text="玩家   "+g.Health+" / "+g.HealthLimit+"    增伤 "+g.Power;
  view.enemyStats.text=g.EnemyHealth+" / "+g.EnemyMaxHealth+"    增伤 "+g.EnemyPower;
  coins.text="金币  "+g.Coins;deckCount.text="牌组  "+g.Deck.Count;if(mapDeckCount!=null)mapDeckCount.text="牌组  "+g.Deck.Count+" · 查看";
  drawCount.text="抽牌堆\n"+g.DrawPileCards.Count;discardCount.text="弃牌堆\n"+g.DiscardCards.Count;
  turnLabel.text="回合 "+g.Round+"   行动 "+g.PlayerActionCount+" / "+g.PlayerActionLimit;
  var rules=g.Enemy.ActiveRules.ToArray();
  EnsureEnemySequences(rules.Length);
  for(int i=0;i<enemyRules.Length;i++)enemyRules[i].text=i<rules.Length?rules[i].Pattern+"\n"+rules[i].Description:"—";
  if(enemySequenceViews!=null)for(int i=0;i<enemySequenceViews.Length;i++){
   if(enemySequenceViews[i]==null)continue;
   if(i<rules.Length)enemySequenceViews[i].Bind(rules[i],false);else enemySequenceViews[i].BindEmpty("未激活牌序");
  }
  if(arenaSequenceView!=null){if(g.ArenaRules.Count>0&&g.ArenaIndex>=0&&g.ArenaIndex<g.ArenaRules.Count)arenaSequenceView.Bind(g.ArenaRules[g.ArenaIndex],false);else arenaSequenceView.BindEmpty("场地牌序");}

  var definition=view.session.enemy==null?null:view.session.enemy.definition;
  var configured=g.Enemy.Encounter?.Definition.Skills;
  if(configured!=null&&enemySkills.Length>0&&configured.Length>enemySkills.Length){var cards=enemySkills.ToList();while(cards.Count<configured.Length){var card=Instantiate(cards[0],cards[0].transform.parent);card.name="EnemySkill_"+cards.Count;cards.Add(card);}enemySkills=cards.ToArray();}
  if(configured!=null&&enemySkills.Length>0){int columns=configured.Length>8?5:4;float scale=configured.Length>8?.42f:.52f;for(int i=0;i<configured.Length;i++)enemySkills[i].SetLayout(new Vector2(24+i%columns*(columns==5?68:86),-46-i/columns*126),scale);}
  var rows=definition==null||definition.cardTable==null?null:definition.cardTable.rows.Where(r=>r.enabled).ToArray();
  var skills=definition==null?null:definition.skills;
  for(int i=0;i<enemySkills.Length;i++){
   bool valid=configured!=null?i<configured.Length:rows!=null?i<rows.Length:skills!=null&&i<skills.Length;
   enemySkills[i].gameObject.SetActive(valid);if(!valid)continue;
   if(configured!=null){var s=configured[i];enemySkills[i].Bind(s.NumberMin,CardEffect.Strike,s.Name+(s.Special?"\n特殊技能":""),true,false,false,false,null);enemySkills[i].SetDetails(view.tooltip,"敌人技能 · "+s.Name,s.Description+"\n数字范围："+s.NumberMin+"～"+s.NumberMax+"\n基础攻击："+CombatResolver.Clamp((long)g.Enemy.Encounter.BaseAttack+g.EliteAttackBonus)+" · 抽取权重："+s.Weight+(s.FixedTrigger?"\n按固定回合触发":""));}
   else if(rows!=null){var r=rows[i];int bonus=g.EnemyCardEffectBonus;enemySkills[i].Bind(r.number,r.effect,r.name+(r.special?"\n特殊技能":""),true,false,false,false,null);enemySkills[i].SetDetails(view.tooltip,"敌人技能 · "+r.name,string.Join("\n",r.EffectSpecs().Select((spec,n)=>(n+1)+". "+spec.Text+" ["+spec.Min+"～"+spec.Max+"]"))+"\n主效果数值："+CombatResolver.Clamp((long)r.min+bonus)+"～"+CombatResolver.Clamp((long)r.max+bonus)+(bonus>0?"\n全局敌人卡牌效果 +"+bonus:""));}
   else {var s=skills[i];enemySkills[i].Bind(s.Number,s.Effect,s.Name+(s.Special?"\n特殊技能":""),true,false,false,false,null);enemySkills[i].SetDetails(view.tooltip,"敌人技能 · "+s.Name,CombatStatuses.Name(s.Effect)+" · 数字 "+s.Number+" · 修正 "+s.Modifier);}
  }
  LayoutCards(g);
  if(pilePanel.activeSelf)UpdatePile(g);
 }
 public void LayoutCards(ContainmentGame g) {LayoutCards(g.Hand.Count,g.Timeline.Count);}
 public void LayoutCardsPreview(){LayoutCards(view.hand.Length,view.actions.Length);}
 private void LayoutCards(int count,int timelineCount) {
  float handWidth=handScroll==null?handArea.rect.width:handScroll.viewport.rect.width;
  float scale=Mathf.Min(handScale,(handWidth-32-cardGap*Mathf.Max(0,count-1))/Mathf.Max(1,count*130f));
  if(handScroll!=null){scale=Mathf.Max(.65f,scale);handArea.sizeDelta=new Vector2(Mathf.Max(handWidth,count*130*scale+Mathf.Max(0,count-1)*cardGap+32),handArea.sizeDelta.y);}
  if(handScroll!=null)scale=Mathf.Min(scale,(handArea.rect.height-32)/225f);
  float width=count*130*scale+Mathf.Max(0,count-1)*cardGap,start=(handArea.rect.width-width)/2;
  for(int i=0;i<view.hand.Length;i++)PlaceCard(view.hand[i],new Vector2(start+i*(130*scale+cardGap),handScroll==null?-34:-24),scale);
  if(pokerLayout==null)pokerLayout=actionArea.GetComponent<PokerActionLayout>();
  if(pokerLayout!=null){pokerLayout.Layout(timelineCount);return;}
  int slots=Mathf.Max(1,timelineCount);if(actionScroll!=null)actionArea.sizeDelta=new Vector2(Mathf.Max(actionScroll.viewport.rect.width,slots*148+52),actionArea.sizeDelta.y);float spacing=(actionArea.rect.width-52)/slots;
  scale=Mathf.Min(actionScale,(spacing-28)/130);
  for(int i=0;i<view.actions.Length;i++){
   float x=26+i*spacing+(spacing-130*scale)/2;
   PlaceCard(view.actions[i],new Vector2(x,-28),scale);
   Place(view.actionLabels[i],x,4,130*scale,22);
   Place(view.leftButtons[i],x,158,24,24);Place(view.rightButtons[i],x+28,158,24,24);Place(view.removeButtons[i],x+56,158,24,24);
  }
  for(int i=0;i<view.insertButtons.Length;i++)Place(view.insertButtons[i],8+i*spacing,69,24,36);
 }
 private static void PlaceCard(CardView card,Vector2 p,float scale){var r=(RectTransform)card.transform;if((r.anchoredPosition-p).sqrMagnitude>1||Mathf.Abs(r.localScale.x-scale)>.001f)card.SetLayout(p,scale);}
 private static void Place(Component c,float x,float y,float w,float h){var r=(RectTransform)c.transform;r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
 public void ToggleSettings(){settingsPanel.SetActive(!settingsPanel.activeSelf);if(settingsPanel.activeSelf)settingsPanel.transform.SetAsLastSibling();}
 public void CloseSettings(){settingsPanel.SetActive(false);}
 public void SetVolume(float value){var fx=view.session.GetComponentInChildren<BattleEffects>();if(fx!=null&&fx.audioSource!=null)fx.audioSource.volume=value;}
 public void CycleSpeed(){view.battle.CycleSpeed();speedLabel.text="播放速度  "+view.battle.PlaybackSpeed.ToString("0.#")+"×";}
 public void ShowDrawPile(){if(deckOperations!=null){deckOperations.Open(DeckOperation.DrawPile);return;}displayedPile=0;pilePanel.SetActive(true);UpdatePile(view.session.Game);}
 public void ShowDiscardPile(){if(deckOperations!=null){deckOperations.Open(DeckOperation.DiscardPile);return;}displayedPile=1;pilePanel.SetActive(true);UpdatePile(view.session.Game);}
 public DeckOperationView deckOperations;
 public void ShowDeck(){if(deckOperations!=null){deckOperations.Open(DeckOperation.Browse);return;}displayedPile=2;pilePanel.SetActive(true);pilePanel.transform.SetAsLastSibling();UpdatePile(view.session.Game);if(deckBrowser!=null)deckBrowser.ResetScroll();}
 public void ClosePile(){pilePanel.SetActive(false);}
 private void UpdatePile(ContainmentGame g){
  if(g==null)return;
  bool fullDeck=displayedPile==2;
  if(deckBrowser!=null)deckBrowser.SetVisible(fullDeck);
  if(fullDeck){pileTitle.text="当前牌组 / "+g.Deck.Count+" 张";if(deckBrowser!=null){deckBrowser.Refresh(g);return;}}
  var cards=fullDeck?g.Deck:displayedPile==0?g.DrawPileCards:g.DiscardCards;
  pileTitle.text=(fullDeck?"当前牌组":displayedPile==0?"抽牌堆":"弃牌堆")+" / "+cards.Count+" 张";
  pileBody.text=cards.Count==0?"暂无卡牌":string.Join("\n",cards.OrderBy(c=>c.Number).Select(c=>c.Number+"  ·  "+c.Description));
 }
}
}
