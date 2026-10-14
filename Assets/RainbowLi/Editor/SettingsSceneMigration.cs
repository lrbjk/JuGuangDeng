using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class SettingsSceneMigration {
  const string Root="Assets/RainbowLi";
  static readonly Color Gold=new Color(.92f,.76f,.42f),Cream=new Color(.95f,.92f,.8f),Surface=new Color(.04f,.12f,.095f),Tile=new Color(.08f,.22f,.18f);
  static Font font;
  [MenuItem("RainbowLi/UI/修复并统一设置界面")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
  public static void Apply(){
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();var menu=view.GetComponent<StartMenuView>();var existing=view.transform.Find("SettingsOverlay");SettingsPanelView ui;
   if(existing!=null)ui=existing.GetComponent<SettingsPanelView>();
   else{
    font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/SourceHanSansCN-Bold.otf");
    var overlay=Panel("SettingsOverlay",view.transform,0,0,1920,1080,new Color(0,0,0,.75f));overlay.GetComponent<Outline>().enabled=false;ui=overlay.gameObject.AddComponent<SettingsPanelView>();ui.view=view;
    var dialog=Panel("Dialog",overlay,480,260,960,560,Surface);Label("Caption",dialog,40,24,880,60,"设置",32,TextAnchor.MiddleCenter);
    Label("VolumeLabel",dialog,60,120,600,48,"战斗音量",26,TextAnchor.MiddleLeft);ui.volumeValue=Label("VolumeValue",dialog,720,120,180,48,"30%",26,TextAnchor.MiddleRight);
    var slider=Panel("Volume",dialog,60,200,840,48,Surface);slider.GetComponent<Outline>().enabled=false;
    var track=Panel("Track",slider,0,18,840,12,Tile);track.GetComponent<Image>().raycastTarget=false;
    var fillArea=Rect("FillArea",slider,18,18,804,12);var fill=Panel("Fill",fillArea,0,0,0,0,Gold);Stretch(fill);fill.GetComponent<Outline>().enabled=false;fill.GetComponent<Image>().raycastTarget=false;
    var handleArea=Rect("HandleArea",slider,18,0,804,48);var handle=Panel("Handle",handleArea,0,0,28,40,Cream);handle.anchorMin=handle.anchorMax=handle.pivot=new Vector2(.5f,.5f);handle.anchoredPosition=Vector2.zero;handle.GetComponent<Image>().raycastTarget=false;
    ui.volume=slider.gameObject.AddComponent<Slider>();ui.volume.fillRect=fill;ui.volume.handleRect=handle;ui.volume.targetGraphic=handle.GetComponent<Image>();ui.volume.minValue=0;ui.volume.maxValue=1;ui.volume.value=.3f;
    ui.speedValue=Label("SpeedValue",dialog,60,306,490,64,"播放速度  1×",26,TextAnchor.MiddleLeft);ui.speed=Click("Speed",dialog,600,306,300,64,"切换速度");
    Label("Hint",dialog,60,390,840,40,"设置即时生效。返回后继续游戏。",22,TextAnchor.MiddleCenter);ui.close=Click("Close",dialog,320,462,320,64,"返回");overlay.gameObject.SetActive(false);
   }
   // Slider stretches its handle vertically; subtract the desired inset from the parent height.
   ui.volume.handleRect.sizeDelta=new Vector2(28,-8);
   var previous=view.battleHud.settingsPanel;var previousMenu=menu==null?null:menu.settingsPanel;
   view.battleHud.settingsPanel=ui.gameObject;view.battleHud.volume=ui.volume;view.battleHud.speedLabel=ui.speedValue;
   if(menu!=null){menu.settingsPanel=ui.gameObject;menu.volume=ui.volume;menu.speed=ui.speed;menu.closeSettings=ui.close;EditorUtility.SetDirty(menu);}
   if(previous!=null&&previous!=ui.gameObject)UnityEngine.Object.DestroyImmediate(previous);
   if(previousMenu!=null&&previousMenu!=previous&&previousMenu!=ui.gameObject)UnityEngine.Object.DestroyImmediate(previousMenu);
   EditorUtility.SetDirty(view.battleHud);EditorUtility.SetDirty(ui);
   foreach(var component in ui.GetComponentsInChildren<Component>(true)){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
  }
  static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  static RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color color){var r=Rect(name,parent,x,y,w,h);r.gameObject.AddComponent<Image>().color=color;var o=r.gameObject.AddComponent<Outline>();o.effectColor=new Color(Gold.r,Gold.g,Gold.b,.42f);o.effectDistance=new Vector2(1,-1);return r;}
  static Text Label(string name,Transform parent,float x,float y,float w,float h,string text,int size,TextAnchor alignment){var r=Rect(name,parent,x,y,w,h);var label=r.gameObject.AddComponent<Text>();label.font=font;label.text=text;label.fontSize=size;label.color=Cream;label.alignment=alignment;label.raycastTarget=false;return label;}
  static Button Click(string name,Transform parent,float x,float y,float w,float h,string text){var r=Panel(name,parent,x,y,w,h,Tile);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();Label("Text",r,10,6,w-20,h-12,text,26,TextAnchor.MiddleCenter);return button;}
  static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=Vector2.zero;}
 }
}
