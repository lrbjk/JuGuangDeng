using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public static class BuffUiSceneMigration {
 const string Root="Assets/RainbowLi";static Font font;
 [MenuItem("RainbowLi/UI/接入双方状态标签")]
 public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
 public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 static void Apply(){
  var hud=UnityEngine.Object.FindFirstObjectByType<BattleHudView>();if(hud==null)throw new InvalidOperationException("缺少 BattleHudView");if(hud.playerStatuses!=null)return;
  font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Resources/RainbowLiChinese.ttf");var parent=hud.view.battlePanel.transform;
  if(hud.view.statusSummary!=null)hud.view.statusSummary.gameObject.SetActive(false);
  hud.playerStatuses=Panel(parent,false,238);hud.enemyStatuses=Panel(parent,true,342);EditorUtility.SetDirty(hud);
 }
 static BattleStatusPanelView Panel(Transform parent,bool enemy,float y){
  var root=Box(enemy?"EnemyStatuses":"PlayerStatuses",parent,1040,y,388,98,new Color(.06f,.09f,.09f));
  var view=root.gameObject.AddComponent<BattleStatusPanelView>();view.enemy=enemy;
  Label("Heading",root,8,3,372,20,(enemy?"敌人状态":"玩家状态")+"  ·  点击说明 / 滚动查看更多",15);
  var viewport=Box("Viewport",root,8,27,372,68,new Color(.06f,.09f,.09f));viewport.gameObject.AddComponent<RectMask2D>();
  var content=Rect("StatusContent",viewport,0,0,372,68);var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;view.content=content;
  var template=Box("StatusChipTemplate",content,0,0,117,34,new Color(.13f,.31f,.25f));var button=template.gameObject.AddComponent<Button>();button.targetGraphic=template.GetComponent<Image>();Label("Label",template,3,1,111,32,"点燃增益  2层\n增益",13);template.gameObject.SetActive(false);view.chipTemplate=button;
  view.emptyLabel=Label("Empty",viewport,0,0,372,60,"暂无状态",16);
  var detail=Box(enemy?"EnemyStatusDetails":"PlayerStatusDetails",parent,450,66,570,330,new Color(.07f,.11f,.12f));var edge=detail.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.85f,.70f,.40f);edge.effectDistance=new Vector2(2,-2);
  view.detailTitle=Label("Title",detail,22,18,470,42,"状态说明",24);
  view.detailBody=Label("Body",detail,22,72,526,224,"点击状态标签查看说明",19);view.detailBody.alignment=TextAnchor.UpperLeft;
  var close=Box("Close",detail,510,16,40,40,new Color(.25f,.20f,.15f));view.closeButton=close.gameObject.AddComponent<Button>();view.closeButton.targetGraphic=close.GetComponent<Image>();Label("Label",close,0,0,40,40,"×",24);
  view.detailPanel=detail.gameObject;detail.gameObject.SetActive(false);return view;
 }
 static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"状态 UI");var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
 static RectTransform Box(string name,Transform parent,float x,float y,float w,float h,Color color){var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=color;return r;}
 static Text Label(string name,Transform parent,float x,float y,float w,float h,string value,int size){var r=Rect(name,parent,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=new Color(.95f,.88f,.70f);t.alignment=TextAnchor.MiddleLeft;t.text=value;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
}
}
