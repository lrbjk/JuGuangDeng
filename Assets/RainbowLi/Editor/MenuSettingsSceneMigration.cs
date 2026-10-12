using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class MenuSettingsSceneMigration {
  const string Root="Assets/RainbowLi";
  static Font font;
  static readonly Color Gold=new Color(.92f,.76f,.42f),Cream=new Color(.95f,.92f,.8f),Surface=new Color(.04f,.12f,.095f),Tile=new Color(.08f,.22f,.18f);
  [MenuItem("RainbowLi/UI/接入菜单流程与设置保存")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
  public static void Apply(){
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();var menu=view.GetComponent<StartMenuView>();var ui=view.battleHud.settingsPanel.GetComponent<SettingsPanelView>();font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Resources/RainbowLiChinese.ttf");
   var prefs=view.GetComponent<GameSettings>();if(prefs==null)prefs=view.gameObject.AddComponent<GameSettings>();prefs.view=view;ui.preferences=prefs;
   if(menu.continueGame==null){
    var actions=menu.panel.transform.Find("Actions");Place(menu.settings.transform,44,28,260,90);Place(menu.start.transform,390,28,260,90);Place(menu.quit.transform,1082,28,260,90);
    menu.continueGame=Click("Continue",actions,736,28,260,90,"继续游戏\n（读取存档）");menu.resume=Click("Resume",actions,1120,144,290,70,"继续本局");menu.resume.gameObject.SetActive(false);menu.message=Label("Message",actions,44,140,1000,80,"",24,TextAnchor.MiddleLeft);
    var overlay=Box("ReplaceRunConfirmation",menu.panel.transform,0,0,1920,1080,new Color(0,0,0,.75f));overlay.GetComponent<Outline>().enabled=false;menu.replacePanel=overlay.gameObject;var dialog=Box("Dialog",overlay,680,340,560,400,Surface);Label("Title",dialog,30,20,500,50,"确认替换当前进度",28,TextAnchor.MiddleCenter);menu.replaceMessage=Label("Message",dialog,40,90,480,170,"",24,TextAnchor.MiddleLeft);menu.cancelReplace=Click("Cancel",dialog,40,290,220,64,"取消");menu.confirmReplace=Click("Confirm",dialog,300,290,220,64,"确认");overlay.gameObject.SetActive(false);
   }
   if(ui.shake==null){
    var dialog=ui.transform.Find("Dialog");Place(dialog,480,210,960,660);Place(dialog.Find("Hint"),60,474,840,40);Place(ui.close.transform,510,556,390,64);
    ui.shakeValue=Label("ShakeValue",dialog,60,400,490,64,"震屏  开启",26,TextAnchor.MiddleLeft);ui.shake=Click("Shake",dialog,600,400,300,64,"切换震屏");ui.returnMenu=Click("ReturnMenu",dialog,60,556,390,64,"返回菜单");
   }
   foreach(var component in new Component[]{menu,ui,prefs})EditorUtility.SetDirty(component);
   foreach(var component in view.GetComponentsInChildren<Component>(true)){if(component==null)continue;EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
  }
  static RectTransform Box(string name,Transform parent,float x,float y,float w,float h,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Outline));go.transform.SetParent(parent,false);Place(go.transform,x,y,w,h);go.GetComponent<Image>().color=color;go.GetComponent<Outline>().effectColor=new Color(Gold.r,Gold.g,Gold.b,.42f);go.GetComponent<Outline>().effectDistance=new Vector2(1,-1);return (RectTransform)go.transform;}
  static Text Label(string name,Transform parent,float x,float y,float w,float h,string text,int size,TextAnchor align){var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Place(go.transform,x,y,w,h);var label=go.GetComponent<Text>();label.font=font;label.text=text;label.fontSize=size;label.color=Cream;label.alignment=align;label.raycastTarget=false;return label;}
  static Button Click(string name,Transform parent,float x,float y,float w,float h,string text){var r=Box(name,parent,x,y,w,h,Tile);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();Label("Text",r,10,6,w-20,h-12,text,26,TextAnchor.MiddleCenter);return button;}
  static void Place(Transform target,float x,float y,float w,float h){var r=(RectTransform)target;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
 }
}
