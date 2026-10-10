using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using RainbowLi;
using RainbowLi.UnityGame;
public static class EffectTargetPlayChecks {
 static int index,stage,checks;static double started;static float actionAt;static GameView view;static BattleAction action;static bool enemy;static CardEffect effect;static bool fatal,dot,bleed;static int ownerBefore,otherBefore;
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);checks++;}
 public static void Run(){
  EffectTargetChecks.Run();TableEffectChecks.Main();
  RainbowLi.EditorTools.CardMotionSceneMigration.Run();
  started=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
  EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;EditorApplication.EnterPlaymode();
 }
 static void Log(string s,string stack,LogType t){if(stack.Contains("UnityEditor.Search.SearchDatabase"))return;if(t==LogType.Error||t==LogType.Exception){Console.WriteLine("TARGET PLAY FAILED: "+s+stack);EditorApplication.Exit(1);}}
 static void Setup(){
  view.battle.autoPlay=false;var motion=view.cardMotion;motion.enableMotion=false;view.session.seed=2718;view.session.NewGame();var g=view.session.Game;g.StartBattle();g.PlayerRules.Clear();g.EnemyRule.Numbers=new[]{0,0,0};foreach(var r in g.ArenaRules)r.Numbers=new[]{0,0,0};g.Health=g.HealthLimit/2;g.EnemyHealth=g.EnemyMaxHealth/2;
  enemy=index%2==1;fatal=index==8;dot=index==9;bleed=index==10;
  effect=index<2?CardEffect.Poison:index<4?CardEffect.Strength:index<6?CardEffect.Guard:index<8?CardEffect.Mend:CardEffect.Strike;
  var spec=new CardEffectSpec{Effect=effect,Name=effect.ToString(),Text="X",Min=3,Max=3,Polarity=effect==CardEffect.Poison?EffectPolarity.Positive:EffectPolarity.Negative,Lifetime=EffectLifetime.Persistent};
  if(fatal||bleed){enemy=false;g.Health=fatal?1:20;g.Player.Statuses.Add(CardEffect.Bleed,3);}
  if(dot){enemy=false;g.Player.Statuses.Add(CardEffect.Poison,3);}
  var card=g.Hand[0];card.Effect=effect;card.Spec=spec;card.EffectAmount=3;card.UpgradeBonus=0;
  if(dot){action=new BattleAction{IsStatusTick=true,Enemy=false};g.Timeline.Insert(0,action);g.AddCard(card);}
  else if(enemy){action=g.Timeline[0];action.Effect=effect;action.EffectSpec=spec;action.Value=3;g.AddCard(card);}
  else {g.InsertCard(card,0);action=g.Timeline[0];}
  Check(g.Confirm(),"confirm live target case "+index);view.session.Notify();motion.enableMotion=true;view.Refresh();
  ownerBefore=enemy?g.EnemyHealth:g.Health;otherBefore=enemy?g.Health:g.EnemyHealth;
  view.session.ResolveOne();actionAt=Time.unscaledTime;
  bool opponent=index<2||index>=8&&!fatal&&!dot;var expected=opponent?!enemy:enemy;
  Check(motion.LastEffectTarget==(expected?motion.enemyTarget:motion.playerTarget),"actual projectile endpoint "+index);
  var owner=enemy?(CombatantState)g.Enemy:g.Player;var other=enemy?(CombatantState)g.Player:g.Enemy;
  if(index<2)Check(other.Statuses[CardEffect.Poison]==3&&owner.Statuses[CardEffect.Poison]==0,"misclassified poison still opponents "+index);
  else if(index<4)Check(owner.Statuses[CardEffect.Strength]==3&&other.Statuses[CardEffect.Strength]==0,"misclassified strength self "+index);
  else if(index<6)Check(owner.Shield==3&&other.Shield==0,"live shield self "+index);
  else if(index<8)Check(owner.Health==ownerBefore+3&&other.Health==otherBefore,"live heal self "+index);
  else if(fatal)Check(owner.Health==0&&other.Health==otherBefore,"fatal bleed cancels attack");
  else if(dot)Check(owner.Health==ownerBefore-3&&other.Health==otherBefore,"DOT damages affected actor");
  else Check(owner.Health==ownerBefore-3&&other.Health==otherBefore-3,"bleed before attack does not reverse attack");
  stage=1;
 }
 static void Tick(){try{
  if(EditorApplication.timeSinceStartup-started>120)throw new Exception("target play timeout "+index);if(!EditorApplication.isPlaying)return;
  view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null||!view.IsReady)return;
  if(stage==0){Setup();return;}var motion=view.cardMotion;
  if(stage==1){
   if(Time.unscaledTime-actionAt<motion.chargeDuration+motion.projectileDuration+.04f)return;
   var labels=motion.effects.overlay.GetComponentsInChildren<Text>().Select(t=>t.text).ToArray();
   if(fatal||bleed)Check(labels.Any(s=>s.StartsWith("流血 ")),"self bleed caption "+index);
   if(dot)Check(labels.Any(s=>s.StartsWith("中毒 ")),"self poison caption");
   if(fatal)Check(motion.motionLayer.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("行动中断")),"fatal bleed interruption label");
   var g=view.session.Game;Check(view.playerHealth.value==g.Health&&view.enemyHealth.value==g.EnemyHealth,"vitals correct on impact "+index);stage=2;return;
  }
  if(stage==2){if(motion.BlocksResolution)return;index++;if(index<=10){stage=0;return;}
   Console.WriteLine("PASS: "+checks+" live target/projectile/status-reaction checks across 11 cases.");EditorApplication.update-=Tick;EditorApplication.Exit(0);}
 }catch(Exception e){Console.WriteLine("TARGET PLAY FAILED: "+e);EditorApplication.Exit(1);}}
}
