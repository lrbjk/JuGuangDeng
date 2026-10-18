using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class EndScreenCopySceneMigration {
  const string Root="Assets/RainbowLi";
  static void Place(Component c,Transform parent,float x,float y,float w,float h){var r=(RectTransform)c.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
  public static void Configure(GameView view){
   string path=Root+"/Data/DefaultEndScreenCopy.asset";var copy=AssetDatabase.LoadAssetAtPath<EndScreenCopy>(path);
   if(copy==null){copy=ScriptableObject.CreateInstance<EndScreenCopy>();
    copy.failureTitle="游戏失败";
    copy.failureBody="很遗憾，您未能通过本次测试。请在结束测试后，跟随工作人员的引导前往“记忆封存部门”进行相关后续工作。\n\n为了确保您此后的生活不受到影响，我们建议您在“记忆封存部门”完成工作之前购买相关保险，以确保您今后的生活不会受到太大变化。\n\n感谢您的参与，期待我们能有机会再见。";
    copy.successTitle="游戏成功";
    copy.successBody="恭喜您成功通过本次测试。请在结束测试后，跟随工作人员的引导前往“图书馆”完成名称留存工作。\n\n我们将会根据此次测试结果，对您的情况进行更进一步的评估。在评估结果出来前，您可以自由参观设施的开放区域，以便于了解您此后的工作环境与内容。\n\n最后，真挚的欢迎您加入我们——“魔法封存局”。";
    AssetDatabase.CreateAsset(copy,path);
   }
   var end=view.endView;end.copy=copy;var panel=view.endPanel.transform;
   Place(view.endTitle,panel,400,170,1120,90);view.endTitle.fontSize=44;view.endTitle.alignment=TextAnchor.MiddleCenter;
   Place(view.endSummary,panel,400,310,1120,390);view.endSummary.fontSize=26;view.endSummary.lineSpacing=1.25f;view.endSummary.resizeTextForBestFit=false;view.endSummary.alignment=TextAnchor.UpperLeft;view.endSummary.horizontalOverflow=HorizontalWrapMode.Wrap;view.endSummary.verticalOverflow=VerticalWrapMode.Truncate;view.endSummary.color=new Color(.85f,.95f,.94f);view.endSummary.raycastTarget=false;
   var child=panel.Find("RunStatistics");var go=child==null?new GameObject("RunStatistics",typeof(RectTransform)):child.gameObject;var stats=go.GetComponent<Text>();if(stats==null)stats=go.AddComponent<Text>();Place(stats,panel,400,740,1120,50);stats.font=view.endSummary.font;stats.fontSize=22;stats.alignment=TextAnchor.MiddleCenter;stats.color=new Color(.52f,.82f,.84f);stats.raycastTarget=false;end.statistics=stats;
   Place(end.restart,panel,640,850,280,70);Place(end.mainMenu,panel,1000,850,280,70);foreach(var b in new[]{end.restart,end.mainMenu}){var label=b.GetComponentInChildren<Text>(true);if(label!=null){Place(label,b.transform,0,0,280,70);label.alignment=TextAnchor.MiddleCenter;}}
   EditorUtility.SetDirty(end);EditorUtility.SetDirty(view.endTitle);EditorUtility.SetDirty(view.endSummary);EditorUtility.SetDirty(stats);
  }
  [MenuItem("RainbowLi/UI/接入结算画面文案")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请先打开RainbowLi场景。");Configure(UnityEngine.Object.FindFirstObjectByType<GameView>());EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();}
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Configure(UnityEngine.Object.FindFirstObjectByType<GameView>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("PASS: Success and failure source copy configured for results.");EditorApplication.Exit(0);}
 }
}
