using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class ActorSceneMigration {
  public static void Run(){
   var scene=EditorSceneManager.OpenScene("Assets/RainbowLi/Scenes/RainbowLiGame.unity");
   ApplyToSession(Object.FindFirstObjectByType<GameSession>());
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("ACTOR MIGRATION PASS: PlayerSystem and EnemySystem added without rebuilding UI.");
  }
  public static void ApplyToSession(GameSession session){
   if(session==null||session.definition==null)throw new System.InvalidOperationException("GameSession definition missing");
   var game=session.definition;
   string playerPath="Assets/RainbowLi/Data/DefaultPlayer.asset",enemyPath="Assets/RainbowLi/Data/DefaultEnemy.asset";
   var p=game.player!=null?game.player:AssetDatabase.LoadAssetAtPath<PlayerDefinition>(playerPath);
   if(p==null){p=ScriptableObject.CreateInstance<PlayerDefinition>();p.maxHealth=game.playerHealth;p.handSize=game.handSize;p.startingDeck=game.initialDeck;p.sequences=game.playerRules;AssetDatabase.CreateAsset(p,playerPath);}
   var e=game.enemy!=null?game.enemy:AssetDatabase.LoadAssetAtPath<EnemyDefinition>(enemyPath);
   if(e==null){e=ScriptableObject.CreateInstance<EnemyDefinition>();e.maxHealth=game.enemyHealth;e.healthGrowth=game.enemyHealthGrowth;e.bossHealth=game.bossHealth;e.attack=game.enemyAttack;e.shield=game.enemyGuard;e.bossAttack=game.bossAttack;e.sequence=game.enemyRule;AssetDatabase.CreateAsset(e,enemyPath);}
   game.player=p;game.enemy=e;EditorUtility.SetDirty(game);
   var player=session.GetComponentInChildren<PlayerModule>(true);
   if(player==null){var go=new GameObject("PlayerSystem");go.transform.SetParent(session.transform,false);player=go.AddComponent<PlayerModule>();}
   var enemy=session.GetComponentInChildren<EnemyModule>(true);
   if(enemy==null){var go=new GameObject("EnemySystem");go.transform.SetParent(session.transform,false);enemy=go.AddComponent<EnemyModule>();}
   player.session=session;player.definition=p;enemy.session=session;enemy.definition=e;session.player=player;session.enemy=enemy;
   EditorUtility.SetDirty(player);EditorUtility.SetDirty(enemy);EditorUtility.SetDirty(session);
  }
 }
}
