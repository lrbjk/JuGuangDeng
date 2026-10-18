using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class AdventureUiArtView:MonoBehaviour {
  public GameView view;
  public Button shopCards,shopRules,restRebuild,rewardEngrave,rewardSequence,rewardExit;
  public Button[] settings;
  public Image serviceHeading;
  public Sprite shopHeading,restHeading;
  public Text[] prices;
  public Text roomHint,deckHeading;
  void Awake(){
   var deck=view.GetComponent<DeckOperationView>();shopCards.onClick.AddListener(()=>deck.Open(DeckOperation.Buy));shopRules.onClick.AddListener(()=>view.sequences.OpenAdjustment(0));restRebuild.onClick.AddListener(()=>view.sequences.OpenAdjustment(0));
   rewardEngrave.onClick.AddListener(()=>deck.Open(DeckOperation.RewardEngrave));rewardSequence.onClick.AddListener(deck.OpenSequenceReward);
   rewardExit.onClick.AddListener(()=>{if(view.session.Game.SkipReward()){view.progression.levels?.CompleteNode();view.session.Notify();}});
   foreach(var button in settings)button.onClick.AddListener(view.battleHud.ToggleSettings);
  }
  void LateUpdate(){Refresh();}
  public void Refresh(){
   var g=view.session.Game;if(g==null)return;bool shop=g.Phase==GamePhase.Shop;
   if(view.servicePanel.activeSelf){serviceHeading.sprite=shop?shopHeading:restHeading;shopCards.gameObject.SetActive(shop);shopRules.gameObject.SetActive(shop);restRebuild.gameObject.SetActive(!shop);foreach(var b in view.buyNumberButtons)b.gameObject.SetActive(false);foreach(var b in view.rebuildButtons)b.gameObject.SetActive(false);roomHint.text=shop?"选择服务后，在卡组或牌序界面操作；费用在确认后扣除。":"生命 "+g.Health+" / "+g.HealthLimit+" · "+(g.ServiceUsed?"本次休息服务已使用":"恢复、删除、激发、重构，任选一项服务");
    shopCards.interactable=g.Coins>=g.CardPurchasePrice;view.serviceView.upgradeDeck.interactable=g.Coins>=g.CardUpgradePrice;view.serviceView.engraveDeck.interactable=g.Coins>=g.CardEngravingPrice;view.serviceView.removeDeck.interactable=!g.ServiceUsed;restRebuild.interactable=!g.ServiceUsed;prices[0].text="花费 "+g.CardPurchasePrice;prices[1].text="花费 "+g.CardUpgradePrice;prices[2].text="花费 "+g.CardEngravingPrice;prices[3].text="花费 "+(view.session.definition==null?18:view.session.definition.rulePrice);
   }
   rewardEngrave.interactable=g.CanClaimReward&&g.Deck.Any(c=>!c.Engraved);rewardSequence.interactable=g.CanClaimReward;rewardExit.interactable=g.CanSkipReward;
   var deck=view.GetComponent<DeckOperationView>();if(deck.IsOpen){deckHeading.text=deck.Operation==DeckOperation.Browse?"已使用卡组":DeckOperationView.Name(deck.Operation);}
  }
 }
}
