using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RainbowLi;
using RainbowLi.UnityGame;
public static class RepeatSpeedChecks {
 static GameView view;static int stage,checks,maximum;static double start;static float lastSpeed=1;
 static void Check(bool v,string name){if(!v)throw new Exception(name);checks++;}
 public static void Run(){EditorSceneManager.OpenScene("Assets/RainbowLi/Scenes/RainbowLiGame.unity");start=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;Application.logMessageReceived+=Log;EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;EditorApplication.EnterPlaymode();}
 static void Log(string s,string stack,LogType t){if(stack.Contains("UnityEditor.Search.SearchDatabase"))return;if(t==LogType.Exception||t==LogType.Error){Console.WriteLine("SPEED FAILED: "+s+stack);EditorApplication.Exit(1);}}
 static void Tick(){try{
  if(EditorApplication.timeSinceStartup-start>90)throw new Exception("timeout");if(!EditorApplication.isPlaying)return;view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null||!view.IsReady)return;var b=view.battle;var g=view.session.Game;var m=view.cardMotion;
  if(stage==0){b.autoPlay=false;m.enableMotion=false;view.session.seed=314159;view.session.NewGame();g=view.session.Game;g.StartBattle();g.PlayerRules.Clear();g.EnemyRule.Numbers=new[]{0,0,0};foreach(var r in g.ArenaRules)r.Numbers=new[]{0,0,0};
   var rule=g.ArenaRules[0];rule.Numbers=new[]{2,2,2};rule.Effect=RuleEffect.Power;rule.Value=0;rule.InsertsTwo=false;
   foreach(var a in g.Timeline){a.Effect=CardEffect.Guard;a.Value=0;a.Number=5;}
   for(int i=0;i<3;i++){g.Hand[i].Spec=null;g.Hand[i].Effect=CardEffect.Guard;g.Hand[i].Number=2;g.InsertCard(g.Hand[i],i);}g.Player.Statuses.Add(CardEffect.Chant,20);view.session.Notify();b.Confirm();Check(g.Phase==GamePhase.Resolving,"confirmed");
   var sample=new BattleAction{Rule=rule};b.TrackResolution(new ResolutionEvent{Action=sample,Round=g.Round});b.TrackResolution(new ResolutionEvent{Action=sample,Round=g.Round});Check(b.RepeatedEffects==1,"event counted once");
   for(int i=0;i<40;i++)b.TrackResolution(new ResolutionEvent{Action=new BattleAction{Rule=rule,Enemy=i%2==1},Round=g.Round});Check(b.RepeatSpeed==6,"alternating sides cap");b.accelerateRepeats=false;Check(b.RepeatSpeed==1,"toggle disables acceleration");b.accelerateRepeats=true;
   b.TrackResolution(new ResolutionEvent{Action=g.Timeline[0],Round=g.Round});Check(b.RepeatSpeed==1&&b.RepeatedEffects==0,"normal action resets");m.enableMotion=true;view.Refresh();stage=1;return;
  }
  if(stage==1){if(m.BlocksResolution)return;
   if(g.Round>1){Check(maximum==21,"all 21 chant effects execute");Check(b.RepeatSpeed==1&&b.RepeatedEffects==0,"next round resets");view.session.NewGame();Check(b.RepeatSpeed==1&&b.RepeatedEffects==0,"new run resets");Console.WriteLine("PASS: "+checks+" repeat speed/real chant/timing checks.");EditorApplication.update-=Tick;EditorApplication.Exit(0);return;}
   b.Step();var a=g.LastResolvedAction;if(a==null)return;
   if(a.Rule!=null){maximum=Math.Max(maximum,b.RepeatedEffects);float expected=Mathf.Min(6,1+Mathf.Max(0,b.RepeatedEffects-6)*.25f);Check(Mathf.Abs(b.RepeatSpeed-expected)<.001f,"gradual speed");Check(b.RepeatSpeed>=lastSpeed,"monotone speed");lastSpeed=b.RepeatSpeed;
    var list=(IList)typeof(CardMotionController).GetField("casts",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(m);Check(list.Count==1,"one actual effect cast");var cast=list[0];float charge=(float)cast.GetType().GetField("charge").GetValue(cast);Check(Mathf.Abs(charge-m.chargeDuration/b.RepeatSpeed)<.001f,"animation uses repeat multiplier");
    float next=(float)typeof(BattleModule).GetField("nextStep",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(b);Check(Mathf.Abs(next-Time.unscaledTime-b.ruleInterval/b.RepeatSpeed)<.001f,"resolution interval uses repeat multiplier");
    b.ToggleAutoPlay();b.ToggleAutoPlay();Check(b.RepeatSpeed==expected,"pause preserves chain");
   }
  }
 }catch(Exception e){Console.WriteLine("SPEED FAILED: "+e);EditorApplication.Exit(1);}}
}
