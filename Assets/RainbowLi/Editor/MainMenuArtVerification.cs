using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RainbowLi.UnityGame;

namespace RainbowLi.EditorTools {
 // Batch entry: -executeMethod RainbowLi.EditorTools.MainMenuArtVerification.Run (without -quit).
 public static class MainMenuArtVerification {
  const string ScenePath="Assets/RainbowLi/Scenes/RainbowLiGame.unity";
  const string Output="Assets/RainbowLi/Verification~/MainMenuPreview";
  static int checks,stage;
  static double began;
  static bool previousOptionsEnabled;
  static EnterPlayModeOptions previousOptions;
  static ContainmentGame previousGame;
  static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks++;}
  public static void Run(){
   try{
    EditorSceneManager.OpenScene(ScenePath);
    var menu=UnityEngine.Object.FindFirstObjectByType<StartMenuView>();
    var buttons=new[]{menu.start,menu.continueGame,menu.quit,menu.settings,menu.manual,menu.resume};
    var ids=buttons.Select(b=>b.GetInstanceID()).ToArray();
    string introduction=menu.introduction.text,flavor=menu.flavor.text,credits=menu.credits.text;
    MainMenuArtSceneMigration.Apply();
    Check(buttons.Select(b=>b.GetInstanceID()).SequenceEqual(ids),"migration preserves button references");
    Check(menu.introduction.text==introduction&&menu.flavor.text==flavor&&menu.credits.text==credits,"migration preserves authored copy");
    Check(menu.settingsPanel.transform.parent==menu.view.transform,"shared battle settings stays outside menu");
    Check(menu.panel.GetComponentsInChildren<MainMenuButtonArt>(true).Length==5,"five styled buttons, no duplicate components");
    Check(menu.panel.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="ArtMainSurface")==1,"idempotent art hierarchy");
    Directory.CreateDirectory(Output);
    Render(menu,1920,1080);Render(menu,1280,720);Render(menu,1920,1200);Render(menu,2560,1080);
    // Reopen saved scene so rendering cameras and preview visibility cannot affect gameplay.
    EditorSceneManager.OpenScene(ScenePath);
    previousOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;previousOptions=EditorSettings.enterPlayModeOptions;
    EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
    began=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;
    Application.logMessageReceived+=Log;EditorApplication.EnterPlaymode();
   }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
  static void Render(StartMenuView menu,int width,int height){
   var canvas=menu.view.GetComponent<Canvas>();
   foreach(Transform child in canvas.transform)child.gameObject.SetActive(child==menu.panel.transform);
   menu.panel.SetActive(true);menu.tutorialPanel.SetActive(false);menu.replacePanel.SetActive(false);
   foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
   var cameraObject=new GameObject("MenuVerificationCamera");var camera=cameraObject.AddComponent<Camera>();
   camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1<<30;camera.orthographic=true;
   var target=new RenderTexture(width,height,24);target.Create();camera.targetTexture=target;
   canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   Canvas.ForceUpdateCanvases();
   typeof(CanvasScaler).GetMethod("Handle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(canvas.GetComponent<CanvasScaler>(),null);
   foreach(var label in canvas.GetComponentsInChildren<Text>())label.SetAllDirty();Canvas.ForceUpdateCanvases();
   Check(((RectTransform)menu.panel.transform).rect.size==((RectTransform)canvas.transform).rect.size,"menu fills canvas "+width+"x"+height);
   foreach(var button in new[]{menu.start,menu.continueGame,menu.quit,menu.settings,menu.manual}){
    var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
    foreach(var corner in corners){var screen=camera.WorldToScreenPoint(corner);Check(screen.x>=-1&&screen.y>=-1&&screen.x<=width+1&&screen.y<=height+1,"button stays on screen "+button.name+" "+width+"x"+height);}
   }
   Check(menu.introductionScroll.viewport.rect.width>500,"readable introduction width");
   camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
   var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
   File.WriteAllBytes(Output+"/menu-"+width+"x"+height+".png",texture.EncodeToPNG());
   RenderTexture.active=old;camera.targetTexture=null;target.Release();
   UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);
  }
  static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+stack);}
  static void Tick(){
   try{
    if(EditorApplication.timeSinceStartup-began>90)throw new TimeoutException("Menu play mode verification timed out.");
    if(!EditorApplication.isPlaying)return;
    var menu=UnityEngine.Object.FindFirstObjectByType<StartMenuView>();if(menu==null||!menu.view.IsReady)return;
    if(stage++==0)return;
    Check(menu.IsOpen&&menu.view.session.PauseElapsedTime,"menu initially pauses game");
    Check(menu.continueGame.interactable==File.Exists(menu.view.session.SavePath),"continue availability follows save file");
    var art=menu.start.GetComponent<MainMenuButtonArt>();var pointer=new PointerEventData(EventSystem.current);
    art.OnPointerEnter(pointer);Check(art.glow.enabled,"pointer hover shows supplied glow");
    art.OnPointerExit(pointer);Check(!art.glow.enabled,"pointer exit removes glow");
    art.OnSelect(new BaseEventData(EventSystem.current));Check(art.glow.enabled,"keyboard selection shows glow");
    menu.start.interactable=false;art.OnSelect(new BaseEventData(EventSystem.current));Check(!art.glow.enabled,"disabled buttons cannot glow");
    menu.start.interactable=true;art.OnDeselect(new BaseEventData(EventSystem.current));
    menu.settings.onClick.Invoke();Check(menu.settingsPanel.activeInHierarchy,"settings opens from menu");
    menu.closeSettings.onClick.Invoke();Check(!menu.settingsPanel.activeSelf,"settings closes");
    menu.manual.onClick.Invoke();Check(menu.tutorialPanel.activeInHierarchy,"manual opens");
    menu.closeManual.onClick.Invoke();Check(!menu.tutorialPanel.activeSelf,"manual closes");
    menu.start.onClick.Invoke();Check(!menu.IsOpen&&!menu.view.session.PauseElapsedTime,"start enters gameplay");
    menu.settingsPanel.SetActive(true);Check(menu.settingsPanel.activeInHierarchy,"shared settings works with menu hidden");menu.settingsPanel.SetActive(false);
    previousGame=menu.view.session.Game;menu.ReturnToMenu();Check(menu.IsOpen&&menu.view.session.PauseElapsedTime,"return pauses gameplay");
    menu.start.onClick.Invoke();Check(menu.replacePanel.activeInHierarchy,"new run requests replacement confirmation");
    menu.cancelReplace.onClick.Invoke();Check(!menu.replacePanel.activeSelf&&menu.view.session.Game==previousGame,"cancel preserves run");
    menu.resume.onClick.Invoke();Check(!menu.IsOpen&&menu.view.session.Game==previousGame,"resume keeps active run");
    menu.ReturnToMenu();menu.start.onClick.Invoke();menu.confirmReplace.onClick.Invoke();
    Check(!menu.IsOpen&&menu.view.session.Game!=previousGame,"confirmed new run replaces active run");
    Finish(true,"MAIN MENU VERIFICATION PASS: "+checks+" checks and four resolution previews.");
   }catch(Exception e){Finish(false,e.ToString());}
  }
  static void Finish(bool success,string message){
   EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
   EditorSettings.enterPlayModeOptionsEnabled=previousOptionsEnabled;EditorSettings.enterPlayModeOptions=previousOptions;
   Debug.Log(message);EditorApplication.Exit(success?0:1);
  }
 }
}
