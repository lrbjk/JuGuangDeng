using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class CardTableSceneMigration {
  public static void Run(){var scene=EditorSceneManager.OpenScene("Assets/RainbowLi/Scenes/RainbowLiGame.unity");Apply();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CARD TABLE SCENE PASS: effect catalog, status displays and original card layout saved.");}
  public static void Apply(){
   var view=Object.FindFirstObjectByType<GameView>();var catalog=AssetDatabase.LoadAssetAtPath<CardEffectCatalog>("Assets/RainbowLi/Data/DefaultCardEffects.asset");
   view.cards.catalog=catalog;view.session.cardSystem=view.cards;view.session.definition.cardEffects=catalog;
   if(view.statusSummary==null){var go=new GameObject("StatusSummary",typeof(RectTransform),typeof(Text));go.transform.SetParent(view.battlePanel.transform,false);var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(20,-294);rect.sizeDelta=new Vector2(1040,42);var text=go.GetComponent<Text>();text.font=view.feedback.font;text.fontSize=14;text.color=new Color(.93f,.8f,.48f);text.raycastTarget=false;view.statusSummary=text;}
   var effects=Object.FindFirstObjectByType<BattleEffects>();effects.gameView=view;effects.actionViews=view.actions;
   EditorUtility.SetDirty(view);EditorUtility.SetDirty(view.cards);EditorUtility.SetDirty(view.session);EditorUtility.SetDirty(view.session.definition);EditorUtility.SetDirty(effects);
  }
 }
}
