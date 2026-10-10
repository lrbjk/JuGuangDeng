using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
public sealed class BattlePanelView:MonoBehaviour {
 public GameView view;
 public void Initialize(){
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
  view.feedback.text=g.LastAction+"\n"+g.Message;
  if(view.statusSummary!=null)view.statusSummary.text="你："+(g.Player.Statuses.Any?g.Player.Statuses.Brief:"无状态")+"\n敌方："+(g.Enemy.Statuses.Any?g.Enemy.Statuses.Brief:"无状态");
  var predicted=planning?g.Forecast():null;
  view.forecast.text=predicted==null?"数字流："+string.Join(" → ",g.NumberStream.Skip(Mathf.Max(0,g.NumberStream.Count-12))):"预计：你 "+predicted.Health+" / 盾 "+predicted.Shield+"\n敌人 "+predicted.EnemyHealth+" / 盾 "+predicted.EnemyShield+"\n数字："+string.Join("→",predicted.Numbers.Take(6));
  view.normalTask.interactable=true;view.bossTask.interactable=g.Victories>=2;view.restTask.interactable=g.RestAvailable;
  view.confirm.gameObject.SetActive(planning);view.confirm.interactable=g.CanConfirm;view.pause.gameObject.SetActive(!planning);view.step.gameObject.SetActive(!planning);view.step.interactable=!view.battle.autoPlay&&!view.battle.IsStepping&&!view.battle.IsSkipping&&g.Phase==GamePhase.Resolving;view.pauseLabel.text=view.battle.autoPlay?"暂停":"继续";
  for(int i=0;i<view.ruleViews.Length;i++){view.ruleViews[i].gameObject.SetActive(i<g.PlayerRules.Count);if(i<g.PlayerRules.Count)view.ruleViews[i].Bind(g.PlayerRules[i],g.Phase==GamePhase.Map);}
  if(fighting){var r=g.ArenaRules[g.ArenaIndex];view.arena.text="场地牌序："+r.Pattern+"\n"+r.Description+" · 最后一个数字所属方触发";view.enemyIntent.text="敌方牌序："+string.Join(" / ",g.Enemy.ActiveRules.Select(rule=>rule.Pattern+" "+rule.Description));}
  for(int i=0;i<view.hand.Length;i++){
   view.hand[i].gameObject.SetActive(i<g.Hand.Count);if(i>=g.Hand.Count)continue;var c=g.Hand[i];bool placed=g.Timeline.Any(a=>a.Card==c);
   view.hand[i].Bind(c.Number,c.Effect,c.Description,false,c.Engraved,view.cards.Selected==c,planning&&!placed&&g.PlayerActionCount<g.PlayerActionLimit,()=>view.cards.Select(c));
   view.hand[i].SetDetails(view.tooltip,"玩家 · "+c.Name+" · 数字 "+c.Number,c.RulesText);
  }
  for(int i=0;i<view.actions.Length;i++){
   bool exists=i<g.Timeline.Count;view.actions[i].gameObject.SetActive(exists);view.actionLabels[i].gameObject.SetActive(exists);
   view.leftButtons[i].gameObject.SetActive(exists&&planning);view.rightButtons[i].gameObject.SetActive(exists&&planning);view.removeButtons[i].gameObject.SetActive(exists&&planning);
   if(!exists)continue;var a=g.Timeline[i];view.actions[i].Bind(a.Number,a.Effect,a.Card==null?(a.Special?"特殊 / ":"")+CombatStatuses.Name(a.Effect)+" "+a.Value:a.Card.Description,a.Enemy,a.Card!=null&&a.Card.Engraved,false,false,null);
   view.actions[i].SetDetails(view.tooltip,(a.Enemy?"敌人":"玩家")+" · "+CombatStatuses.Name(a.Effect),a.Card==null?(a.EffectSpec==null?a.Label:a.EffectSpec.Text.Replace("X",a.Value.ToString())):a.Card.RulesText);
   view.actionLabels[i].text=(i+1)+" / "+(a.Enemy?"敌人":"你");view.leftButtons[i].interactable=i>0;view.rightButtons[i].interactable=i<g.Timeline.Count-1;view.removeButtons[i].interactable=!a.Enemy;
  }
  for(int i=0;i<view.insertButtons.Length;i++){view.insertButtons[i].gameObject.SetActive(planning&&i<=g.Timeline.Count);view.insertButtons[i].interactable=view.cards.Selected!=null&&g.PlayerActionCount<g.PlayerActionLimit;}
 }
}
}
