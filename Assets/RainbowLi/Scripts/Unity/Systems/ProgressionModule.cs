using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class ProgressionModule : MonoBehaviour {
 public GameSession session;
 public LevelModule levels;
 public DeckOperationView deck;
 public void StartNormal(){if(levels!=null)return;session.Game.StartBattle();session.Notify();}
 public void StartBoss(){if(levels!=null){levels.SelectBoss();return;}session.Game.StartBattle(true);session.Notify();}
 public void OpenShop(){if(levels!=null)return;session.Game.OpenService(true);session.Notify();}
 public void OpenRest(){if(levels!=null)return;session.Game.OpenService(false);session.Notify();}
 public void Leave(){session.Game.LeaveService();if(levels!=null)levels.CompleteNode();session.Notify();}
 public void Reward(int index){if(deck!=null)deck.OpenReward(index);}
 public void Heal(){session.Game.RestHeal();session.Notify();}
 public void Generate(){if(deck!=null)deck.OpenGenerate();}
 public void Remove(Card card){if(deck!=null)deck.OpenCard(DeckOperation.Remove,card);}
 public void BuyCard(int number){if(deck!=null)deck.OpenBuy(number);}
}}
