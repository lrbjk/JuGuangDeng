using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class ProgressionModule : MonoBehaviour {
 public GameSession session;
 public LevelModule levels;
 public void StartNormal(){if(levels!=null)return;session.Game.StartBattle();session.Notify();}
 public void StartBoss(){if(levels!=null)return;session.Game.StartBattle(true);session.Notify();}
 public void OpenShop(){if(levels!=null)return;session.Game.OpenService(true);session.Notify();}
 public void OpenRest(){if(levels!=null)return;session.Game.OpenService(false);session.Notify();}
 public void Leave(){session.Game.LeaveService();if(levels!=null)levels.CompleteNode();session.Notify();}
 public void Reward(int index){session.Game.TakeReward(index);if(levels!=null)levels.CompleteNode();session.Notify();}
 public void Heal(){session.Game.RestHeal();session.Notify();}
 public void Generate(){session.Game.RestGenerate();session.Notify();}
 public void Remove(Card card){session.Game.RestRemove(card);session.Notify();}
 public void BuyCard(int number){session.Game.BuyCard(number);session.Notify();}
}}
