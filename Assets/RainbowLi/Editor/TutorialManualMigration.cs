using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class TutorialManualMigration {
  [MenuItem("RainbowLi/UI/更新教学手册")]
  public static void ApplyCurrent(){
   if(Application.isPlaying)return;
   var menu=Object.FindFirstObjectByType<StartMenuView>(FindObjectsInactive.Include);
   if(menu==null)throw new System.InvalidOperationException("Scene has no StartMenuView.");
   Undo.RegisterFullObjectHierarchyUndo(menu.gameObject,"更新教学手册");
   var manual=TutorialManualView.Ensure(menu);EditorUtility.SetDirty(manual);EditorUtility.SetDirty(menu.tutorial);
   EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
   Debug.Log("TUTORIAL MANUAL: Word content imported and scroll area configured.");
  }
 }
}
