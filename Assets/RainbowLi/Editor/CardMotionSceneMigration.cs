using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public static class CardMotionSceneMigration {
 const string Root="Assets/RainbowLi";
 [MenuItem("RainbowLi/动效/接入卡牌飞行到当前场景")]
 public static void ApplyCurrent(){
  if(Application.isPlaying)return;
  var scene=EditorSceneManager.GetActiveScene();
  if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal)){Debug.LogWarning("请打开 RainbowLi 文件夹内的场景。");return;}
  Apply();EditorSceneManager.MarkSceneDirty(scene);
 }
 public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("CARD MOTION SCENE PASS");}
 public static void Apply(){
  var view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null||view.battleHud==null)throw new InvalidOperationException("请先打开带 BattleHudView 的 RainbowLiGame 场景。");
  var fx=view.session.GetComponentInChildren<BattleEffects>();if(fx==null)throw new InvalidOperationException("场景缺少 BattleEffects。");
  var motion=view.GetComponent<CardMotionController>();if(motion==null)motion=Undo.AddComponent<CardMotionController>(view.gameObject);
  Undo.RecordObject(motion,"Bind card motion");Undo.RecordObject(view,"Bind card motion");Undo.RecordObject(fx,"Bind card motion");Undo.RecordObject(view.battle,"Bind card motion");
  var layer=view.transform.Find("CardMotionLayer") as RectTransform;
  if(layer==null){var go=new GameObject("CardMotionLayer",typeof(RectTransform),typeof(CanvasGroup));Undo.RegisterCreatedObjectUndo(go,"Create card motion layer");layer=(RectTransform)go.transform;layer.SetParent(view.transform,false);}
  layer.anchorMin=Vector2.zero;layer.anchorMax=Vector2.one;layer.pivot=new Vector2(.5f,.5f);layer.offsetMin=layer.offsetMax=Vector2.zero;layer.SetAsLastSibling();
  var group=layer.GetComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
  motion.view=view;motion.effects=fx;motion.motionLayer=layer;
  motion.cardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/PokerCard.prefab").GetComponent<CardView>();
  motion.drawPile=view.battleHud.drawCount.transform.parent as RectTransform;motion.discardPile=view.battleHud.discardCount.transform.parent as RectTransform;
  motion.playerTarget=fx.playerTarget;motion.enemyTarget=(RectTransform)view.battleHud.enemyPortrait.transform.parent;motion.ruleOrigin=fx.ruleOrigin;
  fx.enemyTarget=motion.enemyTarget;
  view.cardMotion=motion;fx.cardMotion=motion;view.battle.presentation=motion;
  EditorUtility.SetDirty(motion);EditorUtility.SetDirty(view);EditorUtility.SetDirty(fx);EditorUtility.SetDirty(view.battle);
 }
}
}
