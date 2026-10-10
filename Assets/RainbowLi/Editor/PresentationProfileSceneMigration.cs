using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public static class PresentationProfileSceneMigration {
 const string Root="Assets/RainbowLi";const string Path=Root+"/Data/DefaultBattlePresentation.asset";
 [MenuItem("RainbowLi/动效/打开默认动效配置")]
 public static void Open(){var p=AssetDatabase.LoadAssetAtPath<BattlePresentationProfile>(Path);if(p==null){p=ScriptableObject.CreateInstance<BattlePresentationProfile>();AssetDatabase.CreateAsset(p,Path);AssetDatabase.SaveAssets();}Selection.activeObject=p;EditorGUIUtility.PingObject(p);}
 public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Open();var p=AssetDatabase.LoadAssetAtPath<BattlePresentationProfile>(Path);var view=Object.FindFirstObjectByType<GameView>();view.cardMotion.profile=p;view.battle.profile=p;view.cardMotion.effects.profile=p;EditorUtility.SetDirty(view.cardMotion);EditorUtility.SetDirty(view.battle);EditorUtility.SetDirty(view.cardMotion.effects);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void BindCurrent(BattlePresentationProfile profile){var scene=EditorSceneManager.GetActiveScene();if(Application.isPlaying||profile==null||!scene.path.StartsWith(Root+"/",System.StringComparison.Ordinal))return;var view=Object.FindFirstObjectByType<GameView>();if(view==null||view.cardMotion==null||view.battle==null||view.cardMotion.effects==null)return;Undo.RecordObjects(new Object[]{view.cardMotion,view.battle,view.cardMotion.effects},"绑定战斗动效配置");view.cardMotion.profile=profile;view.battle.profile=profile;view.cardMotion.effects.profile=profile;EditorUtility.SetDirty(view.cardMotion);EditorUtility.SetDirty(view.battle);EditorUtility.SetDirty(view.cardMotion.effects);EditorSceneManager.MarkSceneDirty(scene);}
}
}
