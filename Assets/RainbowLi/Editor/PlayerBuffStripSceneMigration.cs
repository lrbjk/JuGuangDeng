using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class PlayerBuffStripSceneMigration {
  const string Root="Assets/RainbowLi";
  static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height){var child=parent.Find(name);var go=child==null?new GameObject(name,typeof(RectTransform)):child.gameObject;var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);return r;}
  public static void Configure(BattleStatusPanelView panel){
   const float width=410;var root=(RectTransform)panel.transform;root.anchorMin=root.anchorMax=root.pivot=new Vector2(0,1);root.anchoredPosition=new Vector2(1405,-717);root.sizeDelta=new Vector2(width,58);root.SetAsLastSibling();
   panel.singleRow=true;panel.iconMode=true;panel.iconSize=46;panel.iconGap=6;
   var scroll=panel.GetComponent<ScrollRect>();if(scroll==null)throw new InvalidOperationException("玩家Buff缺少ScrollRect");
   var viewport=scroll.viewport;viewport.anchorMin=viewport.anchorMax=viewport.pivot=new Vector2(0,1);viewport.anchoredPosition=Vector2.zero;viewport.sizeDelta=new Vector2(width,46);
   var background=panel.GetComponent<Graphic>();if(background!=null)background.raycastTarget=false;var maskImage=viewport.GetComponent<Graphic>();if(maskImage!=null)maskImage.raycastTarget=false;
   panel.content.anchorMin=panel.content.anchorMax=panel.content.pivot=new Vector2(0,1);panel.content.anchoredPosition=Vector2.zero;panel.content.sizeDelta=new Vector2(width,46);
   scroll.horizontal=true;scroll.vertical=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=52;scroll.StopMovement();
   var track=Rect(root,"HorizontalStatusScrollbar",0,50,width,8);var image=track.GetComponent<Image>();if(image==null)image=track.gameObject.AddComponent<Image>();image.color=new Color(.025f,.1f,.12f,.9f);image.raycastTarget=true;
   var bar=track.GetComponent<Scrollbar>();if(bar==null)bar=track.gameObject.AddComponent<Scrollbar>();
   var handle=Rect(track,"Handle",0,0,70,8);handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.offsetMin=handle.offsetMax=Vector2.zero;
   var handleImage=handle.GetComponent<Image>();if(handleImage==null)handleImage=handle.gameObject.AddComponent<Image>();handleImage.color=new Color(.35f,.8f,.82f,.95f);handleImage.raycastTarget=true;
   bar.handleRect=handle;bar.targetGraphic=handleImage;bar.direction=Scrollbar.Direction.LeftToRight;bar.numberOfSteps=0;var navigation=bar.navigation;navigation.mode=Navigation.Mode.None;bar.navigation=navigation;
   scroll.horizontalScrollbar=bar;scroll.horizontalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;scroll.horizontalNormalizedPosition=0;bar.value=0;
   EditorUtility.SetDirty(panel);EditorUtility.SetDirty(scroll);EditorUtility.SetDirty(bar);
  }
  static void Apply(){var hud=UnityEngine.Object.FindFirstObjectByType<BattleHudView>();if(hud==null||hud.playerStatuses==null)throw new InvalidOperationException("缺少玩家Buff栏");Configure(hud.playerStatuses);}
  [MenuItem("RainbowLi/UI/修复玩家Buff栏裁切")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开RainbowLi场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("PASS: Player buff strip widened, scrollbar configured.");EditorApplication.Exit(0);}
 }
}
