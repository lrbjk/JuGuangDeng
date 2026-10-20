using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class AmbientScreenFlickerVerification {
  private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  private static int checks;
  private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
  private static void Call(AmbientScreenFlicker effect,string method,params object[] args)=>typeof(AmbientScreenFlicker).GetMethod(method,Private).Invoke(effect,args);
  private static T Field<T>(AmbientScreenFlicker effect,string field)=>(T)typeof(AmbientScreenFlicker).GetField(field,Private).GetValue(effect);
  private static void Tick(AmbientScreenFlicker effect,float seconds,bool focused=true)=>Call(effect,"Advance",seconds,focused);
  private static bool Visible(AmbientScreenFlicker effect)=>effect.overlay.GetComponentsInChildren<Graphic>().Any(g=>g.enabled);
  private static Texture2D Frame(){var image=new Texture2D(64,64,TextureFormat.RGB24,false);var colors=new Color[64*64];for(int i=0;i<colors.Length;i++)colors[i]=i%64<32?Color.gray:Color.white;image.SetPixels(colors);image.Apply();return image;}
  public static void Run(){
   checks=0;var savedProfile=AmbientGlitchProfileEditor.GetOrCreate();
   var root=new GameObject("AmbientGlitchCheck",typeof(RectTransform),typeof(Canvas));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
   var effect=root.AddComponent<AmbientScreenFlicker>();var tuning=UnityEngine.Object.Instantiate(savedProfile);effect.profile=tuning;effect.EnsureOverlay();Call(effect,"OnEnable");
   try{
    Check(!Visible(effect)&&Field<float>(effect,"remaining")>=120&&Field<float>(effect,"remaining")<=240,"Startup must wait 2–4 minutes without a flash.");
    Tick(effect,119);Check(!Field<bool>(effect,"captureRequested"),"Glitch must not occur early.");
    Tick(effect,Field<float>(effect,"remaining")+.01f);Check(Field<bool>(effect,"captureRequested"),"Timer must request one screenshot at interval.");
    Call(effect,"BeginGlitch",Frame());var tears=Field<RawImage[]>(effect,"tears");var noise=Field<RawImage[]>(effect,"noiseBands");
    Check(tears.Any(t=>t.enabled&&Mathf.Abs(t.uvRect.x)>.001f),"Actual captured image strips must shift horizontally.");
    Check(tears.Length==14&&tears.All(t=>t.uvRect.height<.12f&&t.color==Color.white),"Stronger tearing must use more bands while retaining original colors.");
    Check(noise.All(n=>n.enabled&&n.texture!=null),"Signal noise must be visible in bands.");
    Check(!effect.overlay.enabled&&effect.overlay.color.a==0,"No full-screen tint may be drawn.");
    Check(effect.overlay.GetComponentsInChildren<Graphic>().All(g=>!g.raycastTarget&&!((MaskableGraphic)g).maskable),"All bands must pass input and avoid UI clipping.");
    var rect=effect.overlay.rectTransform;Check(rect.anchorMin==Vector2.zero&&rect.anchorMax==Vector2.one,"Glitch layer must fill screen.");
    Tick(effect,effect.duration);Check(!Visible(effect)&&Field<Texture2D>(effect,"snapshot")==null&&Field<float>(effect,"remaining")>=120,"End must clear all bands, free snapshot and reschedule.");
    Call(effect,"BeginGlitch",Frame());Call(effect,"OnApplicationFocus",false);Check(!Visible(effect)&&Field<Texture2D>(effect,"snapshot")==null,"Focus loss must restore picture and free capture.");
    float wait=Field<float>(effect,"remaining");Tick(effect,1000,false);Check(Field<float>(effect,"remaining")==wait,"Background must not accumulate triggers.");
    Call(effect,"BeginGlitch",Frame());tuning.effectEnabled=false;Tick(effect,.01f);Check(!Visible(effect),"Disabling profile must restore picture immediately.");tuning.effectEnabled=true;
    Call(effect,"BeginGlitch",Frame());Call(effect,"OnDisable");Check(!Visible(effect)&&Field<Texture2D>(effect,"snapshot")==null,"Disabling component must release screenshot.");
    tuning.minIntervalSeconds=tuning.maxIntervalSeconds=7;tuning.duration=1.6f;tuning.intensity=.82f;Call(effect,"ApplyProfile");
    Check(Field<float>(effect,"remaining")==7&&effect.duration==1.6f&&effect.intensity==.82f,"Designer profile must apply fixed interval, duration and strength live.");
    Tick(effect,2);Check(Mathf.Abs(Field<float>(effect,"remaining")-5)<.001f,"Configured wait must count down in seconds.");
    Call(effect,"BeginGlitch",Frame());Tick(effect,1.2f);Check(Visible(effect),"Custom duration must keep glitch visible beyond old duration.");Tick(effect,.5f);Check(!Visible(effect)&&Field<float>(effect,"remaining")==7,"Custom duration must end and reschedule fixed interval.");
    tuning.minIntervalSeconds=5;tuning.maxIntervalSeconds=8;Call(effect,"ApplyProfile");Check(Field<float>(effect,"remaining")>=5&&Field<float>(effect,"remaining")<=8,"Editing interval bounds must immediately restart countdown.");
    tuning.minIntervalSeconds=-1;tuning.maxIntervalSeconds=-2;tuning.duration=float.NaN;tuning.Validate();Check(tuning.minIntervalSeconds>=1&&tuning.maxIntervalSeconds>=tuning.minIntervalSeconds&&tuning.duration>0,"Invalid designer inputs must be repaired.");
    Check(File.Exists(AmbientGlitchProfileEditor.AssetPath)&&Resources.Load<AmbientGlitchProfile>("AmbientGlitchProfile")==savedProfile,"Saved profile must be available to builds through Resources.");
   }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(tuning);}
   RenderPreview();Debug.Log($"AMBIENT GLITCH PASS: {checks} checks; screen tearing, noise, no tint, timer, input passthrough, cleanup and actual UI render.");
  }
  private static Texture2D Read(RenderTexture target){var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();RenderTexture.active=previous;return image;}
  private static void RenderPreview(){
   EditorSceneManager.OpenScene("Assets/RainbowLi/Scenes/RainbowLiGame.unity");var view=UnityEngine.Object.FindFirstObjectByType<GameView>();var canvas=view.GetComponent<Canvas>();var menu=view.GetComponent<StartMenuView>();
   foreach(Transform child in canvas.transform)child.gameObject.SetActive(child==menu.panel.transform);menu.panel.SetActive(true);
   var effect=AmbientScreenFlicker.Ensure(view);var cameraObject=new GameObject("GlitchPreviewCamera");var camera=cameraObject.AddComponent<Camera>();camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;
   var target=new RenderTexture(1920,1080,24);target.Create();camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   foreach(var transform in canvas.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=30;
   Canvas.ForceUpdateCanvases();camera.Render();var before=Read(target);File.WriteAllBytes("Logs/ambient-glitch-before.png",before.EncodeToPNG());
   Call(effect,"BeginGlitch",before);Canvas.ForceUpdateCanvases();camera.Render();var after=Read(target);File.WriteAllBytes("Logs/ambient-glitch-preview.png",after.EncodeToPNG());
   var a=before.GetPixels32();var b=after.GetPixels32();int changed=0;for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>15)changed++;
   Check(changed>a.Length*.15f&&changed<a.Length*.95f,"Strong glitch must visibly distort many bands while retaining untouched screen regions.");
   UnityEngine.Object.DestroyImmediate(effect);UnityEngine.Object.DestroyImmediate(after);camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);
  }
 }
}
