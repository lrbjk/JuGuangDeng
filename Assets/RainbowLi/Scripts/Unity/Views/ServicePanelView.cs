using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class ServicePanelView:MonoBehaviour {
 public GameView view;
 public bool illustrated;
 public DeckOperationView deck;
 public UnityEngine.UI.Button upgradeDeck,engraveDeck,removeDeck;
 public void Initialize(){foreach(Transform child in view.deckContent)child.gameObject.SetActive(false);for(int i=0;i<view.buyNumberButtons.Length;i++){int number=i+1;view.buyNumberButtons[i].onClick.AddListener(()=>view.progression.BuyCard(number));}for(int i=0;i<view.rebuildButtons.Length;i++){int index=i;view.rebuildButtons[i].onClick.AddListener(()=>view.sequences.OpenAdjustment(index));}upgradeDeck.onClick.AddListener(()=>deck.Open(DeckOperation.Upgrade));engraveDeck.onClick.AddListener(()=>deck.Open(DeckOperation.Engrave));removeDeck.onClick.AddListener(()=>deck.Open(DeckOperation.Remove));}
 public void Refresh(ContainmentGame g){
  if(!view.servicePanel.activeSelf)return;bool shop=g.Phase==GamePhase.Shop;
  view.serviceTitle.text=shop?"档案交易 / 升级与铭刻":"维护装置 / 每场之间执行一次";
  view.heal.gameObject.SetActive(!shop);view.generate.gameObject.SetActive(!shop);view.heal.interactable=view.generate.interactable=!g.ServiceUsed;
  for(int i=0;i<view.buyNumberButtons.Length;i++){var button=view.buyNumberButtons[i];button.gameObject.SetActive(shop&&!illustrated);button.interactable=g.Coins>=g.CardPurchasePrice;button.GetComponentInChildren<UnityEngine.UI.Text>().text="数字 "+(i+1)+" / "+g.CardPurchasePrice+" 金币";}
  for(int i=0;i<view.rebuildButtons.Length;i++){view.rebuildButtons[i].interactable=true;view.rebuildButtons[i].GetComponentInChildren<UnityEngine.UI.Text>().text="进入牌序 "+(i+1)+" 调整界面";}
  view.deckContent.gameObject.SetActive(false);upgradeDeck.gameObject.SetActive(shop);engraveDeck.gameObject.SetActive(shop);removeDeck.gameObject.SetActive(!shop);
 }
}
}
