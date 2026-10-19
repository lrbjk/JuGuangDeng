using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;

namespace RainbowLi.EditorTools {
 public static class UnifiedUiArtVerification {
  const string ScenePath="Assets/RainbowLi/Scenes/RainbowLiGame.unity",Output="Assets/RainbowLi/Verification~/UnifiedUiPreview";
  static int checks,stage;
  static bool oldOptionsEnabled;
  static EnterPlayModeOptions oldOptions;
  static string prefsPrefix;
  static double began;
  static void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);checks++;}
  public static void Run(){
   try{
    EditorSceneManager.OpenScene(ScenePath);var view=UnityEngine.Object.FindFirstObjectByType<GameView>();
    var buttons=view.GetComponentsInChildren<Button>(true);var events=buttons.Select(b=>b.onClick).ToArray();
    var art=buttons.Select(b=>(b.targetGraphic as Image)?.sprite).ToArray();
    UnifiedUiArtMigration.Apply(view);
    Check(view.GetComponentsInChildren<Button>(true).Length==buttons.Length,"no button added or removed by migration");
    for(int i=0;i<buttons.Length;i++){
     var button=buttons[i];Check(ReferenceEquals(button.onClick,events[i]),"button events preserved: "+button.name);
     if(UnifiedUiArtMigration.IsContent(button)){Check((button.targetGraphic as Image)?.sprite==art[i],"content artwork preserved: "+button.name);continue;}
     if(button.GetComponent<MainMenuButtonArt>()!=null)continue;
     Check(button.targetGraphic is Image&&((Image)button.targetGraphic).sprite!=null,"button has artwork: "+button.name);
     Check(button.transition==Selectable.Transition.ColorTint,"button feedback enabled: "+button.name);
     Check(button.colors.normalColor!=button.colors.disabledColor&&button.colors.normalColor!=button.colors.pressedColor,"distinct button states: "+button.name);
    }
    var ui=view.battleHud.settingsPanel.GetComponent<SettingsPanelView>();
    Check(ui.transform.parent==view.transform,"shared settings is outside menu");
    Check(ui.volume.targetGraphic==ui.volume.handleRect.GetComponent<Image>(),"slider handle remains the target graphic");
    Check(ui.volume.fillRect!=null&&ui.volume.handleRect.sizeDelta.x>0,"slider references retained");
    Directory.CreateDirectory(Output);Render(view,1920,1080);Render(view,1280,800);
    EditorSceneManager.OpenScene(ScenePath);view=UnityEngine.Object.FindFirstObjectByType<GameView>();
    prefsPrefix="RainbowLi.UiVerification."+Guid.NewGuid().ToString("N")+".";
    view.GetComponent<GameSettings>().keyPrefix=prefsPrefix;
    oldOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;oldOptions=EditorSettings.enterPlayModeOptions;
    EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
    began=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;Application.logMessageReceived+=Log;EditorApplication.EnterPlaymode();
   }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
  static void Render(GameView view,int width,int height){
   var menu=view.GetComponent<StartMenuView>();var canvas=view.GetComponent<Canvas>();
   foreach(Transform child in view.transform)child.gameObject.SetActive(child==menu.panel.transform||child==menu.settingsPanel.transform);
   menu.panel.SetActive(true);menu.tutorialPanel.SetActive(false);menu.replacePanel.SetActive(false);menu.settingsPanel.SetActive(true);menu.settingsPanel.transform.SetAsLastSibling();
   foreach(var t in view.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
   var go=new GameObject("UiVerificationCamera");var camera=go.AddComponent<Camera>();camera.cullingMask=1<<30;
   camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;
   var target=new RenderTexture(width,height,24);target.Create();camera.targetTexture=target;
   canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   Canvas.ForceUpdateCanvases();typeof(CanvasScaler).GetMethod("Handle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(canvas.GetComponent<CanvasScaler>(),null);
   foreach(var text in canvas.GetComponentsInChildren<Text>())text.SetAllDirty();Canvas.ForceUpdateCanvases();
   var ui=menu.settingsPanel.GetComponent<SettingsPanelView>();
   foreach(var button in new[]{ui.close,ui.returnMenu,ui.speed,ui.shake}){
    var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
    foreach(var p in corners){var screen=camera.WorldToScreenPoint(p);Check(screen.x>=0&&screen.y>=0&&screen.x<=width&&screen.y<=height,"settings control remains in view: "+button.name);}
   }
   camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
   var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
   File.WriteAllBytes(Output+"/settings-"+width+"x"+height+".png",image.EncodeToPNG());
   RenderTexture.active=previous;camera.targetTexture=null;target.Release();
   UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);
  }
  static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+stack);}
  static void Tick(){
   try{
    if(EditorApplication.timeSinceStartup-began>90)throw new TimeoutException("Settings verification timed out.");
    if(!EditorApplication.isPlaying)return;
    var view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null||!view.IsReady||stage++==0)return;
    var menu=view.GetComponent<StartMenuView>();var ui=view.battleHud.settingsPanel.GetComponent<SettingsPanelView>();var prefs=view.GetComponent<GameSettings>();
    menu.settings.onClick.Invoke();Check(ui.gameObject.activeInHierarchy,"menu opens settings");
    ui.volume.value=.61f;Check(Mathf.Abs(prefs.Volume-.61f)<.001f,"volume slider changes audio setting");
    ui.speed.onClick.Invoke();Check(prefs.Speed==2&&view.battle.PlaybackSpeed==2,"speed button changes playback");
    ui.shake.onClick.Invoke();Check(!prefs.ScreenShake,"shake button changes shake preference");
    ui.close.onClick.Invoke();Check(!ui.gameObject.activeSelf&&menu.IsOpen,"close returns to menu");
    menu.start.onClick.Invoke();view.battleHud.ToggleSettings();Check(ui.gameObject.activeInHierarchy&&!menu.IsOpen,"shared settings opens during gameplay");
    Check(ui.returnMenu.gameObject.activeSelf,"return menu available in gameplay");
    ui.returnMenu.onClick.Invoke();Check(menu.IsOpen&&!ui.gameObject.activeSelf,"return menu button works");
    var template=view.GetComponent<DeckOperationView>().generate;
    var clone=UnityEngine.Object.Instantiate(template,template.transform.parent);
    Check(clone.targetGraphic is Image&&((Image)clone.targetGraphic).sprite!=null&&clone.colors.disabledColor==template.colors.disabledColor,"dynamic button clones inherit themed artwork and states");UnityEngine.Object.Destroy(clone.gameObject);
    Check(PlayerPrefs.HasKey(prefsPrefix+"Volume")&&PlayerPrefs.HasKey(prefsPrefix+"Speed"),"settings save using isolated test preference keys");
    Finish(true,"UNIFIED UI VERIFICATION PASS: "+checks+" checks; settings previews at 1920x1080 and 1280x800.");
   }catch(Exception e){Finish(false,e.ToString());}
  }
  static void Finish(bool success,string message){
   EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
   EditorSettings.enterPlayModeOptionsEnabled=oldOptionsEnabled;EditorSettings.enterPlayModeOptions=oldOptions;
   foreach(string suffix in new[]{"Volume","Speed","Shake"})PlayerPrefs.DeleteKey(prefsPrefix+suffix);PlayerPrefs.Save();
   Debug.Log(message);EditorApplication.Exit(success?0:1);
  }
 }
}
