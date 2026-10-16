using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
[DisallowMultipleComponent]
public sealed class DeckBrowserView:MonoBehaviour {
 public RectTransform content;
 public ScrollRect scroll;
 public CardView cardPrefab;
 public TooltipView tooltip;
 public GameObject textPileScroll;
 public Text emptyLabel;
 [Min(1)] public int columns=5;
 [Min(0)] public float gap=12,padding=8;
 readonly List<CardView> cards=new List<CardView>();
 public void SetVisible(bool visible){gameObject.SetActive(visible);if(textPileScroll!=null)textPileScroll.SetActive(!visible);}
 public void ResetScroll(){scroll.StopMovement();scroll.verticalNormalizedPosition=1;}
 public void Refresh(ContainmentGame game){
  var ordered=game.Deck.OrderBy(c=>c.Number).ToArray();
  emptyLabel.gameObject.SetActive(ordered.Length==0);
  while(cards.Count<ordered.Length){var card=Instantiate(cardPrefab,content);card.name="DeckCard_"+cards.Count;cards.Add(card);}
  int count=Mathf.Max(1,columns);float scale=Mathf.Min(1,(scroll.viewport.rect.width-padding*2-gap*(count-1))/(count*130f));scale=Mathf.Max(.1f,scale);
  float width=130*scale,height=225*scale;
  int rows=(ordered.Length+count-1)/count;
  content.sizeDelta=new Vector2(scroll.viewport.rect.width,Mathf.Max(scroll.viewport.rect.height,padding*2+rows*height+Mathf.Max(0,rows-1)*gap));
  for(int i=0;i<cards.Count;i++){
   cards[i].gameObject.SetActive(i<ordered.Length);if(i>=ordered.Length)continue;
   var card=ordered[i];string caption=card.Description+(card.Upgrade>0?" · 升级 "+card.Upgrade:"");
   cards[i].Bind(card.Number,card.Effect,caption,false,card.Engraved,false,false,null,card.Engraving);
   cards[i].SetLayout(new Vector2(padding+i%count*(width+gap),-padding-i/count*(height+gap)),scale);
   cards[i].SetDetails(tooltip,card.Name+" / 数字 "+card.Number,"升级次数："+card.Upgrade+"\n"+card.RulesText+(card.Engraved?"\n铭刻："+(card.Engraving==null?"牌序收尾时追加伤害":card.Engraving.Description):""));
  }
 }
}
}
