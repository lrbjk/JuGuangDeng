using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public static class ScenePreview {
 [MenuItem("RainbowLi/场景预览/任务选择")] public static void Map(){Show(0);}
 [MenuItem("RainbowLi/场景预览/战斗牌桌")] public static void Battle(){Show(1);}
 [MenuItem("RainbowLi/场景预览/奖励")] public static void Reward(){Show(2);}
 [MenuItem("RainbowLi/场景预览/商店与维护")] public static void Service(){Show(3);}
 [MenuItem("RainbowLi/场景预览/结算")] public static void End(){Show(4);}
 private static void Show(int index){
  if(Application.isPlaying)return;var view=Object.FindFirstObjectByType<GameView>();
  if(view==null){Debug.LogWarning("请先打开 RainbowLiGame 场景");return;}
  var panels=new[]{view.mapPanel,view.battlePanel,view.rewardPanel,view.servicePanel,view.endPanel};
  for(int i=0;i<panels.Length;i++){Undo.RecordObject(panels[i],"RainbowLi UI Preview");panels[i].SetActive(i==index);}
  Selection.activeGameObject=panels[index];SceneView.FrameLastActiveSceneView();
 }
}}
