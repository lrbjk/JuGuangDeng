using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class SequenceEditorSceneMigration {
  const string Root="Assets/RainbowLi";
  static readonly Color Gold=new Color(.92f,.76f,.42f),Cream=new Color(.95f,.92f,.8f),Surface=new Color(.04f,.12f,.095f),Tile=new Color(.08f,.22f,.18f);
  static Font font;
  [MenuItem("RainbowLi/UI/接入独立牌序调整界面")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
  public static void Apply(){
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();var map=view.GetComponent<RouteMapView>();var hud=view.GetComponent<AdventureMapHudView>();
   map.mapRoot.GetComponent<Image>().raycastTarget=true;
   font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Resources/RainbowLiChinese.ttf");
   var editor=view.GetComponent<SequenceEditorView>();if(editor==null)editor=view.gameObject.AddComponent<SequenceEditorView>();editor.session=view.session;editor.sequences=view.sequences;view.sequences.editor=editor;
   var old=map.mapRoot.transform.Find("SequenceAdjustmentPanel");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
   var panel=Panel("SequenceAdjustmentPanel",map.mapRoot.transform,450,20,1450,1032,Surface);panel.GetComponent<Image>().raycastTarget=true;editor.panel=panel.gameObject;editor.mapRoot=map.mapRoot;editor.mapPanel=view.mapPanel;editor.selectedColor=new Color(.28f,.27f,.105f);
   editor.instruction=Label("Instruction",panel,30,84,1390,92,"点击数字轮换 0–6（0 为未设置）\n先选择效果槽，再点击备用效果装入；备用效果列表可横向滚动。",25,Cream);
   for(int d=0;d<3;d++)Label("DigitHeading_"+d,panel,34+d*167,228,98,52,new[]{"前","中","后"}[d],32,Gold);
   Label("PrimaryHeading",panel,608,228,98,52,"效果 1",27,Gold);Label("SecondaryHeading",panel,775,228,98,52,"效果 2",27,Gold);Label("ReserveHeading",panel,940,228,498,52,"备用效果",28,Gold);
   editor.rows=new SequenceEditorRow[3];
   for(int i=0;i<3;i++){
    var row=new SequenceEditorRow();editor.rows[i]=row;row.root=Panel("SequenceRow_"+i,panel,14,278+i*174,1424,154,new Color(0,0,0,0));
    row.title=Label("Title",row.root,12,0,465,30,"牌序 "+(i+1),22,Gold);
    row.serviceAction=Click("ServiceAction",row.root,558,0,345,26,"休息 / 商店服务",16);row.serviceAction.gameObject.SetActive(false);
    var digitPanel=Panel("Numbers",row.root,0,27,489,125,new Color(.22f,.22f,.09f));row.digits=new Button[3];row.numbers=new Text[3];
    for(int d=0;d<3;d++){row.digits[d]=Click("Digit_"+d,digitPanel,20+d*167,14,98,98,"—",38);row.numbers[d]=row.digits[d].GetComponentInChildren<Text>();}
    var equipped=Panel("EquippedEffects",row.root,558,27,345,125,new Color(.045f,.20f,.18f));
    row.primarySlot=Click("PrimarySlot",equipped,36,14,98,98,"效果 1",17);row.secondarySlot=Click("SecondarySlot",equipped,203,14,98,98,"空槽\n点击选择",17);
    row.primaryText=row.primarySlot.GetComponentInChildren<Text>();row.secondaryText=row.secondarySlot.GetComponentInChildren<Text>();
    row.primaryFrame=row.primarySlot.GetComponent<Image>();row.secondaryFrame=row.secondarySlot.GetComponent<Image>();
    row.primaryText.color=row.secondaryText.color=Cream;
    row.removeSecondary=Click("RemoveSecondary",equipped,278,14,23,23,"×",19);row.removeSecondary.gameObject.SetActive(false);
    var reserve=Panel("ReserveEffects",row.root,926,27,498,125,Tile);
    var viewport=Panel("Viewport",reserve,8,6,482,102,new Color(0,0,0,0));viewport.gameObject.AddComponent<RectMask2D>();viewport.GetComponent<Image>().raycastTarget=true;
    var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(viewport,false);Place(content.transform,0,0,482,102);
    row.reserves=reserve.gameObject.AddComponent<ScrollRect>();row.reserves.viewport=viewport;row.reserves.content=(RectTransform)content.transform;row.reserves.horizontal=true;row.reserves.vertical=false;row.reserves.movementType=ScrollRect.MovementType.Clamped;
    var scrollbar=Panel("HorizontalScrollbar",reserve,8,113,482,8,new Color(.025f,.08f,.06f));var handle=Panel("Handle",scrollbar,0,0,482,8,Gold);
    handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.pivot=new Vector2(.5f,.5f);handle.sizeDelta=Vector2.zero;handle.anchoredPosition=Vector2.zero;
    var bar=scrollbar.gameObject.AddComponent<Scrollbar>();bar.targetGraphic=handle.GetComponent<Image>();bar.targetGraphic.raycastTarget=true;bar.handleRect=handle;bar.direction=Scrollbar.Direction.LeftToRight;
    row.reserves.horizontalScrollbar=bar;row.reserves.horizontalScrollbarVisibility=ScrollRect.ScrollbarVisibility.Permanent;
    row.effectTemplate=Click("EffectTemplate",content.transform,8,4,104,96,"备用效果",17);row.effectTemplate.gameObject.SetActive(false);
   }
   editor.exitButton=Click("Exit",panel,630,877,254,98,"退出调整",30);
   for(int i=0;i<hud.playerSequences.Length;i++){
    var summary=hud.playerSequences[i];var entry=summary.GetComponent<Button>();if(entry==null)entry=summary.gameObject.AddComponent<Button>();
    foreach(var graphic in summary.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
    entry.targetGraphic=summary.GetComponent<Image>();entry.targetGraphic.raycastTarget=true;entry.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddIntPersistentListener(entry.onClick,editor.Open,i);
    MakeReadOnly(summary);summary.title.text="牌序 "+(i+1)+" · 点击调整";
   }
   foreach(var summary in view.ruleViews)MakeReadOnly(summary);
   panel.gameObject.SetActive(false);
   foreach(var component in map.mapRoot.GetComponentsInChildren<Component>(true))if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
   EditorUtility.SetDirty(editor);EditorUtility.SetDirty(view.sequences);
  }
  static void MakeReadOnly(SequenceView row){foreach(var button in row.digits){button.onClick=new Button.ButtonClickedEvent();button.interactable=false;}if(row.primaryEffect!=null){row.primaryEffect.onClick=new Button.ButtonClickedEvent();row.primaryEffect.gameObject.SetActive(false);}if(row.secondaryEffect!=null){row.secondaryEffect.onClick=new Button.ButtonClickedEvent();row.secondaryEffect.gameObject.SetActive(false);}foreach(var component in row.GetComponentsInChildren<Component>(true)){if(component==null)continue;EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}foreach(var button in row.digits)EditorUtility.SetDirty(button.gameObject);if(row.primaryEffect!=null)EditorUtility.SetDirty(row.primaryEffect.gameObject);if(row.secondaryEffect!=null)EditorUtility.SetDirty(row.secondaryEffect.gameObject);}
  static RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Outline));go.transform.SetParent(parent,false);Place(go.transform,x,y,w,h);go.GetComponent<Image>().color=color;go.GetComponent<Image>().raycastTarget=false;var outline=go.GetComponent<Outline>();outline.effectColor=new Color(Gold.r,Gold.g,Gold.b,.42f);outline.effectDistance=new Vector2(1,-1);return (RectTransform)go.transform;}
  static Text Label(string name,Transform parent,float x,float y,float w,float h,string caption,int size,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Place(go.transform,x,y,w,h);var text=go.GetComponent<Text>();text.text=caption;text.font=font;text.fontSize=size;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=size;return text;}
  static Button Click(string name,Transform parent,float x,float y,float w,float h,string caption,int size){var rect=Panel(name,parent,x,y,w,h,Tile);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();button.targetGraphic.raycastTarget=true;Label("Label",rect,4,4,w-8,h-8,caption,size,Cream);return button;}
  static void Place(Transform target,float x,float y,float w,float h){var rect=(RectTransform)target;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);rect.localScale=Vector3.one;}
 }
}
