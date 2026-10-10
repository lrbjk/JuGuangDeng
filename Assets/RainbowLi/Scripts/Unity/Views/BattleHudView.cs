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
 public CardView[] enemySkills;
 public GameObject settingsPanel,pilePanel;
 public Text pileTitle,pileBody,speedLabel;
 public Slider volume;
 [Range(.1f,1.2f)] public float handScale=.88f,actionScale=.57f;
 [Min(0)] public float cardGap=12;
 private int displayedPile=-1;
 public void Refresh(ContainmentGame g) {
  enemyName.text=g.EnemyName;
  enemyPortrait.enabled=enemyPortrait.sprite!=null;
  portraitPlaceholder.gameObject.SetActive(enemyPortrait.sprite==null);
  playerShield.text="护盾\n"+g.Shield;enemyShield.text="护盾\n"+g.EnemyShield;
  view.playerStats.text="玩家   "+g.Health+" / "+g.HealthLimit+"    增伤 "+g.Power;
  view.enemyStats.text=g.EnemyHealth+" / "+g.EnemyMaxHealth+"    增伤 "+g.EnemyPower;
  coins.text="金币  "+g.Coins;deckCount.text="牌组  "+g.Deck.Count;
  drawCount.text="抽牌堆\n"+g.DrawPileCards.Count;discardCount.text="弃牌堆\n"+g.DiscardCards.Count;
  turnLabel.text="回合 "+g.Round+"   行动 "+g.PlayerActionCount+" / "+g.PlayerActionLimit;
  var rules=g.Enemy.ActiveRules.ToArray();
  for(int i=0;i<enemyRules.Length;i++)enemyRules[i].text=i<rules.Length?rules[i].Pattern+"\n"+rules[i].Description:"—";
  var definition=view.session.enemy==null?null:view.session.enemy.definition;
  var rows=definition==null||definition.cardTable==null?null:definition.cardTable.rows.Where(r=>r.enabled).ToArray();
  var skills=definition==null?null:definition.skills;
  for(int i=0;i<enemySkills.Length;i++){
   bool valid=rows!=null?i<rows.Length:skills!=null&&i<skills.Length;
   enemySkills[i].gameObject.SetActive(valid);if(!valid)continue;
   if(rows!=null){var r=rows[i];enemySkills[i].Bind(r.number,r.effect,r.name+(r.special?"\n特殊技能":""),true,false,false,false,null);}
   else {var s=skills[i];enemySkills[i].Bind(s.Number,s.Effect,s.Name+(s.Special?"\n特殊技能":""),true,false,false,false,null);}
  }
  LayoutCards(g);
  if(pilePanel.activeSelf)UpdatePile(g);
 }
 public void LayoutCards(ContainmentGame g) {LayoutCards(g.Hand.Count,g.Timeline.Count);}
 public void LayoutCardsPreview(){LayoutCards(view.hand.Length,view.actions.Length);}
 private void LayoutCards(int count,int timelineCount) {
  float scale=Mathf.Min(handScale,(handArea.rect.width-32-cardGap*Mathf.Max(0,count-1))/Mathf.Max(1,count*130f));
  float width=count*130*scale+Mathf.Max(0,count-1)*cardGap,start=(handArea.rect.width-width)/2;
  for(int i=0;i<view.hand.Length;i++)PlaceCard(view.hand[i],new Vector2(start+i*(130*scale+cardGap),-34),scale);
  int slots=Mathf.Max(1,timelineCount);float spacing=(actionArea.rect.width-52)/slots;
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
 public void ToggleSettings(){settingsPanel.SetActive(!settingsPanel.activeSelf);}
 public void CloseSettings(){settingsPanel.SetActive(false);}
 public void SetVolume(float value){var fx=view.session.GetComponentInChildren<BattleEffects>();if(fx!=null&&fx.audioSource!=null)fx.audioSource.volume=value;}
 public void CycleSpeed(){view.battle.actionInterval=view.battle.actionInterval>.7f?.45f:view.battle.actionInterval>.3f?.22f:.95f;speedLabel.text="播放速度  "+(view.battle.actionInterval>.7f?"1×":view.battle.actionInterval>.3f?"2×":"4×");}
 public void ShowDrawPile(){displayedPile=0;pilePanel.SetActive(true);UpdatePile(view.session.Game);}
 public void ShowDiscardPile(){displayedPile=1;pilePanel.SetActive(true);UpdatePile(view.session.Game);}
 public void ClosePile(){pilePanel.SetActive(false);}
 private void UpdatePile(ContainmentGame g){if(g==null)return;var cards=displayedPile==0?g.DrawPileCards:g.DiscardCards;pileTitle.text=(displayedPile==0?"抽牌堆":"弃牌堆")+" / "+cards.Count+" 张";pileBody.text=cards.Count==0?"暂无卡牌":string.Join("\n",cards.OrderBy(c=>c.Number).Select(c=>c.Number+"  ·  "+c.Description));}
}
}
