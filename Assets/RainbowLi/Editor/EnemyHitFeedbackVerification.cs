using System;
using System.Collections;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class EnemyHitFeedbackVerification {
  private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
  private static void Call(BattleEffects fx,string method,params object[] args){typeof(BattleEffects).GetMethod(method,Private).Invoke(fx,args);}
  private static void Check(bool valid,string message){if(!valid)throw new Exception(message);}
  public static void Run(){
   if(Application.isPlaying)throw new InvalidOperationException("Verify outside play mode.");
   EditorSceneManager.OpenScene("Assets/RainbowLi/Scenes/RainbowLiGame.unity");
   var fx=UnityEngine.Object.FindFirstObjectByType<BattleEffects>(FindObjectsInactive.Include);
   Check(fx!=null&&fx.gameView!=null&&fx.gameView.battleHud.enemyPortrait!=null,"Enemy portrait wiring is missing.");
   var profile=fx.profile==null?ScriptableObject.CreateInstance<BattlePresentationProfile>():UnityEngine.Object.Instantiate(fx.profile);
   fx.profile=profile;profile.hitFlash=profile.screenShake=true;fx.playerScreenShake=true;fx.audioSource=null;
   var portrait=fx.gameView.battleHud.enemyPortrait;var startColor=portrait.color;var startPosition=portrait.rectTransform.localPosition;
   Call(fx,"Awake");var rootPosition=fx.shakeRoot.localPosition;
   try{
    Fire(fx,20,20,false,true,.06f);
    Check(portrait.color.g<startColor.g&&portrait.color.b<startColor.b,"Enemy damage must flash portrait red.");
    Check(Vector3.Distance(startPosition,portrait.rectTransform.localPosition)>.01f,"Enemy damage must shake portrait.");
    Check(Vector3.Distance(rootPosition,fx.shakeRoot.localPosition)<.001f,"Enemy hit must use local shake without duplicate screen shake.");
    var first=portrait.rectTransform.localPosition;Call(fx,"LateUpdate");
    Check(Vector3.Distance(first,portrait.rectTransform.localPosition)<.001f,"Shake must not accumulate position drift.");
    SetAge(fx,.85f);Call(fx,"LateUpdate");
    Check(portrait.color==startColor&&Vector3.Distance(startPosition,portrait.rectTransform.localPosition)<.001f,"Feedback must recover after hit window.");
    Call(fx,"Clear");Fire(fx,20,0,false,true,.06f);
    Check(portrait.color==startColor,"Blocked hit must not show health-damage red flash.");
    Call(fx,"Clear");fx.playerScreenShake=false;Fire(fx,20,20,false,true,.06f);
    Check(Vector3.Distance(startPosition,portrait.rectTransform.localPosition)<.001f,"Shake setting must disable local shake.");
    Check(portrait.color.g<startColor.g,"Disabling shake must retain damage flash.");
    Call(fx,"Clear");fx.playerScreenShake=true;Fire(fx,20,20,false,false,.06f);
    Check(portrait.color==startColor&&portrait.rectTransform.localPosition==startPosition,"Player damage must not animate enemy portrait.");
    Call(fx,"Clear");Fire(fx,20,20,false,true,-.05f);
    Check(portrait.color==startColor&&portrait.rectTransform.localPosition==startPosition,"Portrait feedback must wait for impact.");
    Call(fx,"Clear");Fire(fx,20,20,false,true,.06f);Call(fx,"Clear");
    Check(portrait.color==startColor&&portrait.rectTransform.localPosition==startPosition,"Skip/reset must restore portrait immediately.");
    Debug.Log("ENEMY HIT FEEDBACK PASS: impact timing, red flash, local shake, no drift, recovery, blocked hits, player hits, shake setting and reset.");
   }finally{Call(fx,"Clear");UnityEngine.Object.DestroyImmediate(profile);}
  }
  private static void Fire(BattleEffects fx,int damage,int health,bool aura,bool enemy,float hitAge){
   Call(fx,"SpawnBurst",Vector2.zero,Vector2.zero,damage,health,aura,enemy,false,null);
   SetAge(fx,fx.profile.fallbackTravelDuration+hitAge);Call(fx,"LateUpdate");
  }
  private static void SetAge(BattleEffects fx,float age){
   var bursts=(IList)typeof(BattleEffects).GetField("bursts",Private).GetValue(fx);
   float now=(float)typeof(BattleEffects).GetProperty("Now",Private).GetValue(fx);
   foreach(var burst in bursts)burst.GetType().GetField("started").SetValue(burst,now-age);
  }
 }
}
