using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public static class UnifiedSequenceSceneMigration {
 const string Root="Assets/RainbowLi";
 static readonly Color Surface=new Color(.065f,.15f,.14f),Gold=new Color(.93f,.78f,.47f),Cream=new Color(.95f,.92f,.80f),Digit=new Color(.12f,.27f,.24f);
 static Font font;
 [MenuItem("RainbowLi/UI/统一玩家敌人场地牌序")]
 public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
 public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void Apply(){
  var view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null||view.battleHud==null)throw new InvalidOperationException("缺少 BattleHudView。");
  var hud=view.battleHud;font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/SourceHanSansCN-Bold.otf");
  var table=view.transform.Find("Table");var sidebar=table.Find("Sidebar");
  var enemy=sidebar.Find("EnemySequenceGroup");var player=sidebar.Find("PlayerSequenceGroup");var arena=table.Find("ArenaSequence");
  Set(enemy,12,332,368,306);Set(player,12,748,368,306);
  Heading(enemy,"敌人牌序");Heading(player,"玩家牌序");Heading(arena,"场地牌序");
  foreach(var text in hud.enemyRules)text.transform.parent.gameObject.SetActive(false);
  hud.enemySequenceViews=new SequenceView[3];
  for(int i=0;i<3;i++){var row=Row(enemy,"EnemySequenceView_"+i);Style(row,48+i*82);row.BindEmpty("未激活牌序");hud.enemySequenceViews[i]=row;}
  for(int i=0;i<view.ruleViews.Length;i++)Style(view.ruleViews[i],48+i*82);
  hud.arenaSequenceView=Row(arena,"ArenaSequenceView");Style(hud.arenaSequenceView,48);
  view.arena.gameObject.SetActive(false);
  var hint=Text(arena,"SharedTriggerHint");Set(hint.transform,14,128,340,34);hint.text="双方共享 · 最后一个数字所属方触发";Format(hint,14,Gold,TextAnchor.MiddleLeft);
  var resources=sidebar.Find("Resources");Set(resources,12,648,368,44);Set(hud.coins.transform,10,2,160,40);Set(hud.deckCount.transform,190,2,166,40);
  Set(hud.turnLabel.transform,12,696,190,44);var restart=sidebar.Find("NewGame");if(restart!=null)Set(restart,208,700,148,40);
  foreach(var label in hud.enemyRules)EditorUtility.SetDirty(label);
  EditorUtility.SetDirty(hud);EditorUtility.SetDirty(view);
 }
 static SequenceView Row(Transform parent,string name){
  var existing=parent.Find(name);if(existing!=null)return existing.GetComponent<SequenceView>();
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/SequenceCard.prefab");
  var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);instance.name=name;return instance.GetComponent<SequenceView>();
 }
 static void Heading(Transform parent,string title){var text=Text(parent,"Heading");Set(text.transform,12,6,344,32);text.text=title;Format(text,26,Gold,TextAnchor.MiddleCenter);}
 static void Style(SequenceView view,float y){
  Set(view.transform,14,y,340,74);var image=view.GetComponent<Image>();image.color=Surface;image.raycastTarget=false;
  var outline=view.GetComponent<Outline>();if(outline==null)outline=view.gameObject.AddComponent<Outline>();outline.effectColor=new Color(Gold.r,Gold.g,Gold.b,.45f);outline.effectDistance=new Vector2(1,-1);
  Set(view.title.transform,8,2,145,22);Format(view.title,17,Gold,TextAnchor.MiddleLeft);
  Set(view.description.transform,164,26,168,44);Format(view.description,16,Cream,TextAnchor.MiddleLeft);
  for(int i=0;i<3;i++){
   Set(view.digits[i].transform,8+i*50,30,42,36);var background=view.digits[i].GetComponent<Image>();background.color=Digit;
   var colors=view.digits[i].colors;colors.normalColor=Color.white;colors.disabledColor=Color.white;colors.highlightedColor=new Color(1.2f,1.2f,1.2f);view.digits[i].colors=colors;
   Set(view.numbers[i].transform,0,0,42,36);Format(view.numbers[i],21,Cream,TextAnchor.MiddleCenter);
   if(i<2){var arrow=Text(view.transform,"SequenceArrow_"+i);Set(arrow.transform,50+i*50,30,8,36);arrow.text="›";Format(arrow,16,Gold,TextAnchor.MiddleCenter);}
  }
  EditButton(view.primaryEffect,154,86);EditButton(view.secondaryEffect,244,90);
  foreach(var component in view.GetComponentsInChildren<Component>(true))if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
  EditorUtility.SetDirty(view);
 }
 static void EditButton(Button button,float x,float width){if(button==null)return;Set(button.transform,x,2,width,22);button.GetComponent<Image>().color=Digit;var label=button.GetComponentInChildren<Text>(true);Set(label.transform,2,0,width-4,22);Format(label,11,Cream,TextAnchor.MiddleCenter);}
 static Text Text(Transform parent,string name){var child=parent.Find(name);if(child!=null)return child.GetComponent<Text>();var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);return go.GetComponent<Text>();}
 static void Format(Text text,int size,Color color,TextAnchor alignment){text.font=font;text.fontSize=size;text.resizeTextForBestFit=true;text.resizeTextMinSize=Mathf.Min(size,12);text.resizeTextMaxSize=size;text.color=color;text.alignment=alignment;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;}
 static void Set(Transform transform,float x,float y,float width,float height){var rect=(RectTransform)transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;}
}
}
