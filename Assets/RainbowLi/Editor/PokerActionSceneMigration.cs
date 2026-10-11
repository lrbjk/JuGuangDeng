using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;

namespace RainbowLi.EditorTools {
public static class PokerActionSceneMigration {
 const string Root="Assets/RainbowLi";
 [MenuItem("RainbowLi/UI/应用扑克式行动牌桌")]
 public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
 public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void Apply(){
  var v=UnityEngine.Object.FindFirstObjectByType<GameView>();if(v==null||v.battleHud==null)throw new InvalidOperationException("缺少 GameView/BattleHudView。");
  var hud=v.battleHud;var layout=hud.actionArea.GetComponent<PokerActionLayout>();if(layout==null)layout=hud.actionArea.gameObject.AddComponent<PokerActionLayout>();layout.view=v;layout.hud=hud;hud.pokerLayout=layout;
  var parent=hud.actionScroll==null?hud.actionArea.parent:hud.actionScroll.viewport.parent;var background=parent.GetComponent<Image>();if(background!=null)background.color=Color.clear;
  foreach(var buttons in new[]{v.leftButtons,v.rightButtons,v.removeButtons,v.insertButtons})foreach(var b in buttons)b.gameObject.SetActive(false);
  for(int i=0;i<v.actions.Length;i++){v.actions[i].gameObject.SetActive(i<2);v.actionLabels[i].gameObject.SetActive(i<2);}
  layout.Layout(2);v.battleView.dropArea=hud.actionScroll==null?hud.actionArea:hud.actionScroll.viewport;
  foreach(var text in v.GetComponentsInChildren<Text>(true)){
   if(text.name=="ActionHeading"){text.text="行动牌桌 / 从左到右执行 · 拖动玩家牌排序";text.rectTransform.sizeDelta=new Vector2(900,24);}
   if(text.name=="Heading"&&text.text.StartsWith("当前手牌")){text.text="当前手牌 / 拖牌上桌 · 拖回撤回 / 滚轮查看更多";text.rectTransform.sizeDelta=new Vector2(800,30);}
   if(text.name=="Tutorial")text.text="拖动手牌到牌桌 → 拖动玩家牌排序 → 拖回手牌区撤回\n最近三个数字触发牌序，场地由最后一个数字所属方触发";
  }
  EditorUtility.SetDirty(v);EditorUtility.SetDirty(hud);EditorUtility.SetDirty(layout);EditorUtility.SetDirty(v.battleView);
 }
}
}
