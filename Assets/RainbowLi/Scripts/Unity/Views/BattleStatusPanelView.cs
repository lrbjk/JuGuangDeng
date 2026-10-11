using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
public sealed class BattleStatusPanelView:MonoBehaviour {
 public bool enemy;
 public RectTransform content;
 public Button chipTemplate;
 public Text emptyLabel;
 public GameObject detailPanel;
 public Text detailTitle,detailBody;
 public Button closeButton;
 public Color positiveColor=new Color(.13f,.31f,.25f),negativeColor=new Color(.39f,.15f,.16f),specialColor=new Color(.26f,.20f,.37f);
 [Tooltip("横排状态标签，状态较多时搭配水平 ScrollRect 查看。")]
 public bool singleRow;
 readonly Dictionary<CardEffect,Button> chips=new Dictionary<CardEffect,Button>();
 CombatStatuses statuses;CardEffect? selected;
 void Awake(){closeButton.onClick.AddListener(CloseDetails);}
 void OnDisable(){CloseDetails();}
 public void Refresh(CombatStatuses value){
  statuses=value;emptyLabel.gameObject.SetActive(!value.Any);
  foreach(var chip in chips.Values)chip.gameObject.SetActive(false);
  var effects=value.ActiveEffects.OrderBy(e=>Category(e)).ThenBy(e=>(int)e).ToArray();
  int columns=singleRow?Mathf.Max(1,effects.Length):3;
  content.sizeDelta=singleRow?new Vector2(Mathf.Max(((RectTransform)content.parent).rect.width,effects.Length*123),34):new Vector2(content.sizeDelta.x,Mathf.Max(70,((effects.Length+2)/3)*38));
  for(int i=0;i<effects.Length;i++){
   var effect=effects[i];if(!chips.TryGetValue(effect,out var button)){button=Instantiate(chipTemplate,content);button.name="Status_"+effect;button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(()=>ShowDetails(effect));chips.Add(effect,button);}
   button.gameObject.SetActive(true);var r=(RectTransform)button.transform;r.anchoredPosition=new Vector2(i%columns*123,-i/columns*38);button.GetComponent<Image>().color=Category(effect)==0?positiveColor:Category(effect)==1?negativeColor:specialColor;
   button.GetComponentInChildren<Text>().text=CombatStatuses.Name(effect)+"  "+value[effect]+"层\n"+CategoryName(effect);
  }
  if(selected.HasValue){if(value[selected.Value]<=0)CloseDetails();else UpdateDetails(selected.Value);}
 }
 public void ShowDetails(CardEffect effect){if(statuses==null||statuses[effect]<=0)return;selected=effect;detailPanel.SetActive(true);detailPanel.transform.SetAsLastSibling();UpdateDetails(effect);}
 public void CloseDetails(){selected=null;if(detailPanel!=null)detailPanel.SetActive(false);}
 void UpdateDetails(CardEffect effect){
  string owner=enemy?"敌人":"玩家",other=enemy?"玩家":"敌人";
  detailTitle.text=owner+" · "+CombatStatuses.Name(effect)+"  "+statuses[effect]+"层";
  detailBody.text="类型："+CategoryName(effect)+"   |   持有者："+owner+"\n\n"+Explain(effect,statuses[effect],owner,other)+"\n\n持续："+(statuses.LifetimeOf(effect)==EffectLifetime.Persistent?"本场战斗内保留，按下述效果消耗或衰减。":"本回合有效，回合结束清除。")+Decay(effect);
 }
 public static int Category(CardEffect e)=>CardEffectTargeting.TargetsOpponent(e)?1:e==CardEffect.RandomNumbers||e==CardEffect.Chant||e==CardEffect.Reciprocity||e==CardEffect.Mutation||e==CardEffect.AddNumber||e==CardEffect.SubtractNumber?2:0;
 public static string CategoryName(CardEffect e)=>Category(e)==0?"增益":Category(e)==1?"负面状态":"特殊效果";
 public static string Explain(CardEffect e,int x,string owner,string other){switch(e){
  case CardEffect.Ignite:return owner+"每次行动后，对"+other+"施加 "+x+" 层烧伤。\n这是自身增益，不会因这个增益烧伤自己。";
  case CardEffect.Envenom:return owner+"每次行动后，对"+other+"施加 "+x+" 层中毒。";
  case CardEffect.Burn:return owner+"回合开始时受到 "+x+" 点伤害（可被护盾抵挡）。";
  case CardEffect.Poison:return owner+"回合结束时受到 "+x+" 点伤害（可被护盾抵挡）。";
  case CardEffect.Bleed:return owner+"每次行动前失去 "+x+" 点生命，无视护盾。";
  case CardEffect.Strength:return owner+"造成的伤害增加 "+x+" 点。";
  case CardEffect.Vulnerable:return owner+"每次受到的伤害增加 "+x+" 点。";
  case CardEffect.Weaken:return owner+"造成的伤害减少 25%，向下取整；层数表示剩余衰减次数，不叠加比例。";
  case CardEffect.Fortify:return owner+"受到的伤害减少 25%，向下取整；不叠加比例。";
  case CardEffect.Regeneration:return owner+"回合开始恢复 "+x+" 点生命。";
  case CardEffect.IronWall:return owner+"获得的护盾增加 25%，向下取整；不叠加比例。";
  case CardEffect.Parry:return owner+"抵挡接下来 "+x+" 次非零伤害。";
  case CardEffect.Barrier:return owner+"回合开始获得 "+x+" 点护盾。";
  case CardEffect.DamageCap:return owner+"每次受到的伤害最多为 "+x+" 点；数值越小效果越强。";
  case CardEffect.RandomNumbers:return owner+"每次行动后追加 "+x+" 个随机数字。";
  case CardEffect.Chant:return owner+"触发的牌序效果额外触发 "+x+" 次。";
  case CardEffect.Reciprocity:return owner+"的行为额外执行一次；额外行为不会继续递归复制。";
  case CardEffect.Disorder:return owner+"填入的数字随机变为 1～6。";
  case CardEffect.Mutation:return owner+"接下来 "+x+" 次填入的数字随机变为 1～6。";
  case CardEffect.AddNumber:return owner+"填入的数字增加 "+x+"。";
  case CardEffect.SubtractNumber:return owner+"填入的数字减少 "+x+"。";
  default:return "作用于"+owner+"。";
 }}
 static string Decay(CardEffect e){switch(e){
  case CardEffect.Burn:case CardEffect.Regeneration:return "\n每次回合开始结算后减少 1 层。";
  case CardEffect.Poison:case CardEffect.Barrier:return "\n每次结算后层数减半，向下取整。";
  case CardEffect.Bleed:return "\n每次行动前结算后减少 1 层。";
  case CardEffect.Weaken:case CardEffect.Fortify:case CardEffect.IronWall:return "\n每次回合结束减少 1 层。";
  case CardEffect.Parry:return "\n每次成功抵挡伤害消耗 1 层。";
  case CardEffect.Mutation:return "\n每次填入数字消耗 1 层。";
  default:return "";
 }}
}
}
