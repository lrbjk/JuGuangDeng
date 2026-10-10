using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public static class BattlePlaybackSceneMigration {
 const string Root="Assets/RainbowLi";
 public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 [MenuItem("RainbowLi/UI/接入战斗播放控制")]
 public static void ApplyCurrent(){var s=EditorSceneManager.GetActiveScene();if(Application.isPlaying||!s.path.StartsWith(Root+"/",StringComparison.Ordinal))return;Apply();EditorSceneManager.MarkSceneDirty(s);}
 static void Apply(){var view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null)throw new InvalidOperationException("缺少 GameView");var existing=view.battlePanel.GetComponentInChildren<BattlePlaybackView>(true);if(existing!=null){StateLayout(existing.stateLabel);return;}
  var go=new GameObject("PlaybackControls",typeof(RectTransform),typeof(BattlePlaybackView));var root=(RectTransform)go.transform;root.SetParent(view.battlePanel.transform,false);Place(root,900,460,540,44);
  var ui=go.GetComponent<BattlePlaybackView>();ui.battle=view.battle;ui.pause=view.pause;ui.step=view.step;ui.pauseLabel=view.pauseLabel;
  view.pause.transform.SetParent(root,false);Place((RectTransform)view.pause.transform,0,0,82,42);
  view.step.transform.SetParent(root,false);Place((RectTransform)view.step.transform,90,0,82,42);view.step.GetComponentInChildren<Text>().text="单步";
  ui.speed=CopyButton(view.step,root,"Speed",182,100,"1× 倍速");UnityEventTools.AddPersistentListener(ui.speed.onClick,view.battle.CycleSpeed);ui.speedLabel=ui.speed.GetComponentInChildren<Text>();
  ui.skip=CopyButton(view.step,root,"Skip",290,130,"跳过本回合");UnityEventTools.AddPersistentListener(ui.skip.onClick,view.battle.Skip);
  var label=new GameObject("PlaybackState",typeof(RectTransform),typeof(Text));label.transform.SetParent(root,false);Place((RectTransform)label.transform,430,0,110,42);ui.stateLabel=label.GetComponent<Text>();ui.stateLabel.font=view.pauseLabel.font;StateLayout(ui.stateLabel);ui.stateLabel.color=new Color(.94f,.82f,.57f);ui.stateLabel.raycastTarget=false;
  var feedback=(RectTransform)view.feedback.transform;feedback.sizeDelta=new Vector2(870,feedback.sizeDelta.y);
  EditorUtility.SetDirty(view.battle);EditorUtility.SetDirty(ui);
 }
 static void StateLayout(Text label){Place(label.rectTransform,430,0,110,52);label.fontSize=16;label.resizeTextForBestFit=true;label.resizeTextMinSize=12;label.resizeTextMaxSize=16;label.alignment=TextAnchor.MiddleLeft;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;EditorUtility.SetDirty(label);}
 static Button CopyButton(Button source,Transform parent,string name,float x,float width,string text){var b=UnityEngine.Object.Instantiate(source,parent);b.name=name;b.onClick=new Button.ButtonClickedEvent();Place((RectTransform)b.transform,x,0,width,42);b.GetComponentInChildren<Text>().text=text;return b;}
 static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
}
}
