using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
public sealed class DeckRowView : MonoBehaviour {
 public Text label;
 public Button upgrade,engrave,remove;
 public void Bind(Card card,int index,GameSession session,EngravingModule engravings,ProgressionModule progression){
  var g=session.Game;bool shop=g.Phase==GamePhase.Shop;
  label.text=(index+1).ToString("00")+" / 数字 "+card.Number+" · "+card.Description;
  upgrade.gameObject.SetActive(shop);engrave.gameObject.SetActive(shop);remove.gameObject.SetActive(!shop);
  upgrade.interactable=g.Coins>=12&&card.CanUpgrade;engrave.interactable=g.Coins>=20&&!card.Engraved;remove.interactable=!g.ServiceUsed&&g.Deck.Count>6;
  upgrade.onClick.RemoveAllListeners();engrave.onClick.RemoveAllListeners();remove.onClick.RemoveAllListeners();
  upgrade.onClick.AddListener(()=>engravings.Upgrade(card));engrave.onClick.AddListener(()=>engravings.Engrave(card));remove.onClick.AddListener(()=>progression.Remove(card));
 }
}}
