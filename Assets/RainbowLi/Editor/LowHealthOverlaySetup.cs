using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class LowHealthOverlaySetup {
  [MenuItem("RainbowLi/UI/添加低血量红色滤镜")]
  public static void ApplyCurrent(){
   if(Application.isPlaying)return;
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();
   if(view==null)throw new InvalidOperationException("Current scene has no GameView.");
   LowHealthOverlay.Ensure(view);EditorUtility.SetDirty(view.gameObject);
   EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
  }
  [MenuItem("RainbowLi/UI/验证低血量红色滤镜")]
  public static void Verify(){
   var root=new GameObject("LowHealthCheck",typeof(RectTransform),typeof(Canvas));
   root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
   var effect=root.AddComponent<LowHealthOverlay>();effect.EnsureImage();
   try{
    Check(!effect.ShouldShow(40,100,true),"40% must stay clear");
    Check(effect.ShouldShow(39,100,true),"39% must show");
    Check(!effect.ShouldShow(41,100,true),"41% must stay clear");
    Check(effect.ShouldShow(1,100,true),"critical health must show");
    Check(effect.ShouldShow(0,100,true),"zero health during battle presentation must show");
    Check(!effect.ShouldShow(1,100,false),"noncombat must stay clear");
    Check(!effect.ShouldShow(1,0,true),"invalid maximum must stay clear");
    Check(!effect.ShouldShow(400,1000,true)&&effect.ShouldShow(399,1000,true),"max health scaling");
    Check(!effect.overlay.raycastTarget,"must not intercept clicks or drags");
    Check(effect.overlay.rectTransform.anchorMin==Vector2.zero&&effect.overlay.rectTransform.anchorMax==Vector2.one,"full-screen anchors");
    var tick=typeof(LowHealthOverlay).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic);
    effect.Refresh(39,100,true);
    for(int i=0;i<30;i++)tick.Invoke(effect,new object[]{1f/60});
    Check(effect.overlay.enabled&&Mathf.Abs(effect.overlay.color.a-effect.opacity)<.001f,"fade in");
    effect.Refresh(40,100,true);for(int i=0;i<30;i++)tick.Invoke(effect,new object[]{1f/60});
    Check(!effect.overlay.enabled&&effect.overlay.color.a==0,"healing must clear tint");
    effect.Refresh(1,100,true);for(int i=0;i<30;i++)tick.Invoke(effect,new object[]{1f/60});
    effect.Refresh(1,100,false);for(int i=0;i<30;i++)tick.Invoke(effect,new object[]{1f/60});
    Check(!effect.overlay.enabled,"leaving battle must clear tint");
    Debug.Log("LOW HEALTH FILTER PASS: threshold, recovery, phase changes, fade, full-screen layout and input passthrough.");
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  private static void Check(bool valid,string message){if(!valid)throw new Exception(message);}
  public static void Run(){
   Verify();
   EditorSceneManager.OpenScene("Assets/RainbowLi/Scenes/RainbowLiGame.unity");
   ApplyCurrent();EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
   Debug.Log("LOW HEALTH FILTER: applied and saved RainbowLiGame scene.");
  }
 }
}
