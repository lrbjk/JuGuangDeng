using System.Collections.Generic;
using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class ServicePanelView:MonoBehaviour {
 public GameView view;readonly List<DeckRowView> rows=new List<DeckRowView>();
 public void Initialize(){foreach(Transform child in view.deckContent)child.gameObject.SetActive(false);for(int i=0;i<view.buyNumberButtons.Length;i++){int number=i+1;view.buyNumberButtons[i].onClick.AddListener(()=>view.progression.BuyCard(number));}for(int i=0;i<view.rebuildButtons.Length;i++){int index=i;view.rebuildButtons[i].onClick.AddListener(()=>{if(view.session.Game.Phase==GamePhase.Shop)view.sequences.BuyEffect(index);else view.sequences.Rebuild(index);});}}
 public void Refresh(ContainmentGame g){if(!view.servicePanel.activeSelf)return;bool shop=g.Phase==GamePhase.Shop;view.serviceTitle.text=shop?"档案交易 / 升级与铭刻":"维护装置 / 每场之间执行一次";view.heal.gameObject.SetActive(!shop);view.generate.gameObject.SetActive(!shop);view.heal.interactable=view.generate.interactable=!g.ServiceUsed;foreach(var b in view.buyNumberButtons){b.gameObject.SetActive(shop);b.interactable=g.Coins>=15;}for(int i=0;i<view.rebuildButtons.Length;i++){view.rebuildButtons[i].interactable=shop?g.Coins>=18:!g.ServiceUsed;view.rebuildButtons[i].GetComponentInChildren<UnityEngine.UI.Text>().text=shop?"牌序 "+(i+1)+" 换效果 / 18 金币":"重构牌序 "+(i+1);}while(rows.Count<g.Deck.Count){var row=Instantiate(view.deckRowPrefab,view.deckContent);row.name="DeckRow_"+rows.Count;rows.Add(row);}for(int i=0;i<rows.Count;i++){rows[i].gameObject.SetActive(i<g.Deck.Count);if(i<g.Deck.Count)rows[i].Bind(g.Deck[i],i,view.session,view.engravings,view.progression);}}
}
}
