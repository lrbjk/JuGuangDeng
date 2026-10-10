using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using RainbowLi;
using RainbowLi.UnityGame;
public static class CardMotionChecks {
 static double started;static float stageAt;static int stage,checks;static GameView view;static Card card;static int enemyBefore,castsBefore;static bool captured;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
 public static void Run(){
  RainbowLi.EditorTools.CardMotionSceneMigration.Run();view=UnityEngine.Object.FindFirstObjectByType<GameView>();
  foreach(var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"missing script: "+t.name);
  Check(view.cardMotion!=null&&view.cardMotion.motionLayer!=null,"serialized controller/layer");
  Check(view.battle.presentation==view.cardMotion,"playback gate bound");Check(view.hand.All(c=>PrefabUtility.IsPartOfPrefabInstance(c)),"prefab links preserved");
  Check(!view.cardMotion.motionLayer.GetComponent<CanvasGroup>().blocksRaycasts,"overlay does not block clicks");
  Check(CardMotionController.TargetsEnemy(new BattleAction{Effect=CardEffect.Poison}),"player poison targets enemy");
  Check(!CardMotionController.TargetsEnemy(new BattleAction{Effect=CardEffect.Guard}),"player guard targets self");
  Check(CardMotionController.TargetsEnemy(new BattleAction{Effect=CardEffect.Guard,Enemy=true}),"enemy guard targets enemy");
  Check(!CardMotionController.TargetsEnemy(new BattleAction{Effect=CardEffect.Strike,Enemy=true}),"enemy attack targets player");
  Check(!CardMotionController.TargetsEnemy(new BattleAction{Effect=CardEffect.Mend}),"player heal targets self");
  Check(!CardMotionController.TargetsEnemy(new BattleAction{Effect=CardEffect.Guard,EffectSpec=new CardEffectSpec{Polarity=EffectPolarity.Negative}}),"direct guard ignores erroneous polarity like domain");
  Check(CardMotionController.TargetsEnemy(new BattleAction{Enemy=true,Rule=new SequenceRule{Effect=RuleEffect.Shield}}),"enemy sequence shield self");
  started=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
  EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;EditorApplication.EnterPlaymode();
 }
 static void Log(string s,string stack,LogType t){if(stack.Contains("UnityEditor.Search.SearchDatabase"))return;if(t==LogType.Error||t==LogType.Exception){Console.WriteLine("MOTION FAILED: "+s+stack);EditorApplication.Exit(1);}}
 static void Next(int n){stage=n;stageAt=Time.unscaledTime;captured=false;}
 static void Tick(){try{
  if(EditorApplication.timeSinceStartup-started>120)throw new Exception("timeout stage "+stage);if(!EditorApplication.isPlaying)return;
  view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null||!view.IsReady)return;var g=view.session.Game;var motion=view.cardMotion;
  if(stage==0){view.battle.autoPlay=false;view.session.seed=314159;view.session.NewGame();g=view.session.Game;g.StartBattle();g.EnemyMaxHealth=g.EnemyHealth=500;view.session.Notify();Check(g.Phase==GamePhase.Planning,"entered battle");Check(motion.TotalDraws==g.Hand.Count,"initial deal observed");Check(motion.BlocksResolution&&motion.ActiveTransfers>0,"deal occupies presentation");Next(1);return;}
  if(stage==1){
   if(!captured&&Time.unscaledTime-stageAt>.22f){Capture("01-deal");captured=true;}
   if(motion.BlocksResolution)return;
   Check(view.hand.Take(g.Hand.Count).All(c=>c.GetComponent<CanvasGroup>().alpha==1),"all hand cards revealed");
   card=g.Hand[0];card.Effect=CardEffect.Strike;view.cards.Select(card);view.cards.Insert(0);
   Check(motion.TotalPlacements==1&&motion.ActiveTransfers>0,"hand flies to battle");
   Check(view.hand[0].GetComponent<CanvasGroup>().alpha==0,"placed hand copy hidden");
   Check(motion.motionLayer.GetComponentsInChildren<Graphic>(true).All(c=>!c.raycastTarget),"every flying graphic ignores raycasts");
   Check(g.Hand.Contains(card),"animation does not remove domain hand");
   view.battle.Confirm();view.battle.Step();Check(g.LastResolvedAction==null,"step waits for placement");Next(2);return;
  }
  if(stage==2){if(motion.BlocksResolution)return;enemyBefore=g.EnemyHealth;castsBefore=motion.TotalCasts;view.battle.Step();Check(g.EnemyHealth<enemyBefore,"real strike resolved");Check(view.enemyHealth.value==enemyBefore,"health display waits for projectile hit");Check(motion.ActiveCasts==1&&motion.TotalCasts==castsBefore+1,"one cast created");var last=g.LastResolvedAction;view.battle.Step();Check(g.LastResolvedAction==last,"second step waits for impact");Next(3);return;}
  if(stage==3){
   if(!captured&&Time.unscaledTime-stageAt>motion.chargeDuration+motion.projectileDuration*.45f){Capture("02-attack-flight");captured=true;}
   if(motion.BlocksResolution)return;Check(view.enemyHealth.value==g.EnemyHealth,"health display applied at impact");Check(motion.motionLayer.childCount==0,"cast objects released");
   view.battle.Step();Next(4);return;
  }
  if(stage==4){
   if(motion.BlocksResolution)return;
   if(g.Round==1&&g.Phase==GamePhase.Resolving){view.battle.Step();return;}
   Check(g.Round==2&&g.Phase==GamePhase.Planning,"advanced round");Check(motion.TotalDiscards>=6,"old hand flies to discard");Check(motion.TotalDraws>=12,"next hand flies from draw pile");Next(5);return;
  }
  if(stage==5){
   if(!captured&&Time.unscaledTime-stageAt>.1f){Capture("03-round-deal");captured=true;}
   if(motion.BlocksResolution)return;
   card=g.Hand[0];view.cards.Select(card);view.cards.Insert(0);view.cards.Move(0,1);view.cards.Remove(1);
   Check(g.PlayerActionCount==0,"rapid place/reorder/remove preserves rules");Check(motion.ActiveTransfers>0,"removed card flies back");Next(6);return;
  }
  if(stage==6){if(motion.BlocksResolution)return;Check(view.hand[0].GetComponent<CanvasGroup>().alpha==1,"returned hand revealed");Check(motion.motionLayer.childCount==0,"no stale ghost after rapid edits");
   view.cards.Select(g.Hand[0]);view.cards.Insert(0);view.session.NewGame();Check(!motion.BlocksResolution&&motion.ActiveTransfers==0&&motion.ActiveCasts==0,"new game cancels old animations");Next(7);return;}
  if(stage==7){
   g.StartBattle();view.session.Notify();motion.enableMotion=false;view.Refresh();view.cards.Select(g.Hand[0]);view.cards.Insert(0);view.battle.Confirm();view.battle.Step();Check(g.LastResolvedAction!=null,"motion off uses immediate fallback");Check(!motion.BlocksResolution,"disabled motion never blocks");motion.enableMotion=true;view.session.NewGame();g=view.session.Game;g.StartBattle();g.EnemyHealth=1;g.EnemyShield=0;view.session.Notify();Next(8);return;
  }
  if(stage==8){if(motion.BlocksResolution)return;card=g.Hand[0];card.Effect=CardEffect.Strike;view.cards.Select(card);view.cards.Insert(0);view.battle.Confirm();Next(9);return;}
  if(stage==9){if(motion.BlocksResolution)return;view.battle.Step();Check(g.Phase==GamePhase.Reward,"lethal strike domain reward");Check(motion.KeepBattleVisible&&view.battlePanel.activeSelf&&!view.rewardPanel.activeSelf,"finish impact before reward");Next(10);return;}
  if(stage==10){if(!captured&&Time.unscaledTime-stageAt>.45f){Capture("04-lethal-impact");captured=true;}if(motion.BlocksResolution)return;Check(view.rewardPanel.activeSelf&&!view.battlePanel.activeSelf,"reward revealed after presentation");Check(motion.motionLayer.childCount==0,"terminal presentation released");Console.WriteLine("PASS: "+checks+" card motion/scene/lifecycle/interaction checks.");EditorApplication.update-=Tick;EditorApplication.Exit(0);}
 }catch(Exception e){Console.WriteLine("MOTION FAILED: "+e);EditorApplication.Exit(1);}}
 static void Capture(string name){
  if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
  var canvas=view.GetComponent<Canvas>();var mode=canvas.renderMode;var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
  var rt=new RenderTexture(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
  try{camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();Directory.CreateDirectory("Assets/RainbowLi/Verification~/CardMotionPreview");File.WriteAllBytes("Assets/RainbowLi/Verification~/CardMotionPreview/"+name+".png",image.EncodeToPNG());}
  finally{RenderTexture.active=oldActive;camera.targetTexture=oldTarget;canvas.renderMode=mode;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
}
