using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
public sealed class BattlePanelView:MonoBehaviour {
 public GameView view;
 [Tooltip("拖放接收区域；未指定时使用行动条的可见区域。")]
 public RectTransform dropArea;
 public Color insertionColor=new Color(1f,.8f,.35f,.95f);
 [Range(.1f,1f)] public float draggedHandOpacity=.35f;
 private Image insertionMarker;
 public bool CanDrag(Card card){var g=view.session.Game;return card!=null&&g!=null&&g.Phase==GamePhase.Planning&&g.Hand.Contains(card)&&g.PlayerActionCount<g.PlayerActionLimit&&!g.Timeline.Any(a=>a.Card==card);}
 public bool CanDragAction(BattleAction action){var g=view.session.Game;return action!=null&&!action.Enemy&&action.Card!=null&&g!=null&&g.Phase==GamePhase.Planning&&g.Timeline.Contains(action);}
 public bool IsHandDrop(Vector2 screen,Camera camera){var hud=view.battleHud;var area=hud==null?null:hud.handScroll!=null?hud.handScroll.viewport:hud.handArea;return area!=null&&area.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(area,screen,camera);}
 public RectTransform DropArea=>dropArea!=null?dropArea:view.battleHud!=null?(view.battleHud.actionScroll!=null?view.battleHud.actionScroll.viewport:view.battleHud.actionArea):view.actions.Length>0?view.actions[0].transform.parent as RectTransform:null;
 public bool TryGetDropSlot(Vector2 screen,Camera camera,out int slot){
  slot=-1;var area=DropArea;var g=view.session.Game;
  if(area==null||!area.gameObject.activeInHierarchy||g==null||g.Phase!=GamePhase.Planning||!RectTransformUtility.RectangleContainsScreenPoint(area,screen,camera))return false;
  if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(area,screen,camera,out var local))return false;
  slot=g.Timeline.Count;
  for(int i=0;i<g.Timeline.Count&&i<view.actions.Length;i++){var r=(RectTransform)view.actions[i].transform;float center=area.InverseTransformPoint(r.TransformPoint(r.rect.center)).x;if(local.x<center){slot=i;break;}}
  return true;
 }
 public void ShowInsertion(int slot){
  var area=DropArea;if(area==null||slot<0){HideInsertion();return;}
  if(insertionMarker==null){var go=new GameObject("DragInsertionMarker",typeof(RectTransform),typeof(Image));insertionMarker=go.GetComponent<Image>();insertionMarker.raycastTarget=false;}
  var rect=insertionMarker.rectTransform;rect.SetParent(area,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
  float x=area.rect.xMin+12;
  if(slot<view.insertButtons.Length){var r=(RectTransform)view.insertButtons[slot].transform;x=area.InverseTransformPoint(r.TransformPoint(r.rect.center)).x;}
  rect.sizeDelta=new Vector2(6,Mathf.Max(20,area.rect.height-16));rect.localPosition=new Vector3(Mathf.Clamp(x,area.rect.xMin+4,area.rect.xMax-4),area.rect.center.y,0);rect.SetAsLastSibling();insertionMarker.color=insertionColor;insertionMarker.gameObject.SetActive(true);
 }
 public void HideInsertion(){if(insertionMarker!=null)insertionMarker.gameObject.SetActive(false);}
 private void OnDisable(){HideInsertion();}
 public void Initialize(){
  foreach(var text in view.GetComponentsInChildren<Text>(true))if(text.name=="Tutorial")text.text=text.text.Replace("选择手牌 → 点击 + 插入敌我行动之间","拖动手牌到行动条，插入敌我行动之间");
  for(int i=0;i<view.insertButtons.Length;i++){int n=i;view.insertButtons[i].onClick.AddListener(()=>view.cards.Insert(n));}
  for(int i=0;i<view.actions.Length;i++){int n=i;view.leftButtons[i].onClick.AddListener(()=>view.cards.Move(n,-1));view.rightButtons[i].onClick.AddListener(()=>view.cards.Move(n,1));view.removeButtons[i].onClick.AddListener(()=>view.cards.Remove(n));}
 }
 public void EnsureCapacity(ContainmentGame g){
  if(g.Hand.Count>view.hand.Length){var list=view.hand.ToList();while(list.Count<Mathf.Max(g.Hand.Count,g.HandLimit)){var card=Instantiate(view.hand[0],view.hand[0].transform.parent);card.name="HandCard_"+list.Count;list.Add(card);}view.hand=list.ToArray();for(int i=0;i<view.hand.Length;i++)view.hand[i].SetLayout(new Vector2(25+i*115,-344),.8f);}
  if(g.Timeline.Count>view.actions.Length){int count=g.Timeline.Count;var cs=view.actions.ToList();var labels=view.actionLabels.ToList();var left=view.leftButtons.ToList();var right=view.rightButtons.ToList();var remove=view.removeButtons.ToList();var inserts=view.insertButtons.ToList();
   while(cs.Count<count){int n=cs.Count;var c=Instantiate(view.actions[0],view.actions[0].transform.parent);c.name="ActionCard_"+n;cs.Add(c);labels.Add(Instantiate(view.actionLabels[0],view.actionLabels[0].transform.parent));var l=Instantiate(view.leftButtons[0],view.leftButtons[0].transform.parent);l.onClick=new Button.ButtonClickedEvent();l.onClick.AddListener(()=>view.cards.Move(n,-1));left.Add(l);var rr=Instantiate(view.rightButtons[0],view.rightButtons[0].transform.parent);rr.onClick=new Button.ButtonClickedEvent();rr.onClick.AddListener(()=>view.cards.Move(n,1));right.Add(rr);var x=Instantiate(view.removeButtons[0],view.removeButtons[0].transform.parent);x.onClick=new Button.ButtonClickedEvent();x.onClick.AddListener(()=>view.cards.Remove(n));remove.Add(x);}
   while(inserts.Count<=count){int n=inserts.Count;var b=Instantiate(view.insertButtons[0],view.insertButtons[0].transform.parent);b.onClick=new Button.ButtonClickedEvent();b.onClick.AddListener(()=>view.cards.Insert(n));inserts.Add(b);}view.actions=cs.ToArray();view.actionLabels=labels.ToArray();view.leftButtons=left.ToArray();view.rightButtons=right.ToArray();view.removeButtons=remove.ToArray();view.insertButtons=inserts.ToArray();
   float spacing=1010f/count;for(int i=0;i<count;i++){float x=36+i*spacing;view.actions[i].SetLayout(new Vector2(x,-85),Mathf.Min(.82f,spacing/140));((RectTransform)view.actionLabels[i].transform).anchoredPosition=new Vector2(x,-50);((RectTransform)view.leftButtons[i].transform).anchoredPosition=new Vector2(x,-288);((RectTransform)view.rightButtons[i].transform).anchoredPosition=new Vector2(x+32,-288);((RectTransform)view.removeButtons[i].transform).anchoredPosition=new Vector2(x+64,-288);}for(int i=0;i<inserts.Count;i++)((RectTransform)inserts[i].transform).anchoredPosition=new Vector2(8+i*spacing,-145);
  }
 }
 public void Refresh(ContainmentGame g){bool planning=g.Phase==GamePhase.Planning,fighting=planning||g.Phase==GamePhase.Resolving;
  view.playerHealth.maxValue=g.HealthLimit;view.playerHealth.value=g.Health;view.enemyHealth.maxValue=Mathf.Max(1,g.EnemyMaxHealth);view.enemyHealth.value=g.EnemyHealth;
  view.playerStats.text="玩家 / 攻击 "+g.BaseAttack+"\n生命 "+g.Health+" / "+g.HealthLimit+"   护盾 "+g.Shield+"   增伤 "+g.Power;
  view.enemyStats.text=fighting?g.EnemyName+"\n生命 "+g.EnemyHealth+" / "+g.EnemyMaxHealth+"   护盾 "+g.EnemyShield+"   增伤 "+g.EnemyPower:"完成收容 "+g.Victories+" 次";
  view.roundStats.text="回合 "+g.Round+"   行动 "+g.PlayerActionCount+" / "+g.PlayerActionLimit+"\n金币 "+g.Coins+"   牌组 "+g.Deck.Count;
  view.feedback.text=g.LastAction+"\n"+g.Message.Replace("点击手牌追加行动，再用箭头调整敌我顺序。","拖牌上桌；桌上玩家牌可拖动排序，拖回手牌区撤回。");
  if(view.statusSummary!=null)view.statusSummary.text="你："+(g.Player.Statuses.Any?g.Player.Statuses.Brief:"无状态")+"\n敌方："+(g.Enemy.Statuses.Any?g.Enemy.Statuses.Brief:"无状态");
  var predicted=planning?g.Forecast():null;
  view.forecast.text=predicted==null?"数字流："+string.Join(" → ",g.NumberStream.Skip(Mathf.Max(0,g.NumberStream.Count-12))):"预计：你 "+predicted.Health+" / 盾 "+predicted.Shield+"\n敌人 "+predicted.EnemyHealth+" / 盾 "+predicted.EnemyShield+"\n数字："+string.Join("→",predicted.Numbers.Take(6));
  view.normalTask.interactable=true;view.bossTask.interactable=g.Route?.BossUnlockByBattles==true?g.Route.BossUnlocked:g.Victories>=6;view.restTask.interactable=g.RestAvailable;
  view.confirm.gameObject.SetActive(planning);view.confirm.interactable=g.CanConfirm;view.pause.gameObject.SetActive(!planning);view.step.gameObject.SetActive(!planning);view.step.interactable=!view.battle.autoPlay&&!view.battle.IsStepping&&!view.battle.IsSkipping&&g.Phase==GamePhase.Resolving;view.pauseLabel.text=view.battle.autoPlay?"暂停":"继续";
  for(int i=0;i<view.ruleViews.Length;i++){view.ruleViews[i].gameObject.SetActive(i<g.PlayerRules.Count);if(i<g.PlayerRules.Count)view.ruleViews[i].Bind(g.PlayerRules[i],false);}
  if(fighting){var r=g.ArenaRules[g.ArenaIndex];view.arena.text="场地牌序："+r.Pattern+"\n"+r.Description+" · 最后一个数字所属方触发";view.enemyIntent.text="敌方牌序："+string.Join(" / ",g.Enemy.ActiveRules.Select(rule=>rule.Pattern+" "+rule.Description));}
  for(int i=0;i<view.hand.Length;i++){
   view.hand[i].gameObject.SetActive(i<g.Hand.Count);if(i>=g.Hand.Count)continue;var c=g.Hand[i];bool placed=g.Timeline.Any(a=>a.Card==c);
   view.hand[i].Bind(c.Number,c.Effect,c.Description,false,c.Engraved,false,planning&&!placed&&g.PlayerActionCount<g.PlayerActionLimit,null);
   var drag=view.hand[i].GetComponent<HandCardDrag>();if(drag==null)drag=view.hand[i].gameObject.AddComponent<HandCardDrag>();drag.Bind(this,c);
   view.hand[i].SetDetails(view.tooltip,"玩家 · "+c.Name+" · 数字 "+c.Number,c.RulesText);
  }
  for(int i=0;i<view.actions.Length;i++){
   bool exists=i<g.Timeline.Count;view.actions[i].gameObject.SetActive(exists);view.actionLabels[i].gameObject.SetActive(exists);
   view.leftButtons[i].gameObject.SetActive(false);view.rightButtons[i].gameObject.SetActive(false);view.removeButtons[i].gameObject.SetActive(false);
   if(!exists)continue;var a=g.Timeline[i];int growth=g.CardEffectGrowthFor(a);int shownValue=CombatResolver.Clamp((long)a.Value+growth);view.actions[i].Bind(a.Number,a.Effect,a.Card==null?(a.Special?"特殊 / ":"")+CombatStatuses.Name(a.Effect)+" "+shownValue:a.Card.Description+(growth>0?" · 全局卡效 +"+growth:""),a.Enemy,a.Card!=null&&a.Card.Engraved,false,planning&&!a.Enemy&&a.Card!=null,null);
   var drag=view.actions[i].GetComponent<HandCardDrag>();if(!a.Enemy&&a.Card!=null){if(drag==null)drag=view.actions[i].gameObject.AddComponent<HandCardDrag>();drag.enabled=true;drag.Bind(this,a.Card,a);}else if(drag!=null)drag.enabled=false;
   view.actions[i].SetDetails(view.tooltip,(a.Enemy?"敌人":"玩家")+" · "+CombatStatuses.Name(a.Effect),(a.Card==null?(a.EffectSpec==null?a.Label+(growth>0?"\n当前效果数值："+shownValue:""):a.EffectSpec.Text.Replace("X",shownValue.ToString())):a.Card.RulesText)+(growth>0?"\n全局敌人卡牌效果 +"+growth+"（已计入当前效果）":""));
   view.actionLabels[i].text=(i+1).ToString("00")+" · "+(a.Enemy?"敌方":"玩家");view.actionLabels[i].color=a.Enemy?new Color(.98f,.57f,.46f):new Color(.55f,.88f,.78f);
  }
  for(int i=0;i<view.insertButtons.Length;i++)view.insertButtons[i].gameObject.SetActive(false);
  foreach(var text in view.battlePanel.GetComponentsInChildren<Text>(true)){if(text.name=="Heading"&&text.text.StartsWith("当前手牌"))text.text="当前手牌 / 拖牌上桌 · 拖回撤回 / 滚轮查看更多";if(text.name=="ActionHeading")text.text="行动牌桌 / 从左到右执行 · 拖动玩家牌排序";}
 }
}
}
