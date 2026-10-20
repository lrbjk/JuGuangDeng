using System;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class TutorialManualVerification {
  private static int checks;
  private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
  public static void Run(){
   checks=0;EditorSceneManager.OpenScene("Assets/RainbowLi/Scenes/RainbowLiGame.unity");TutorialManualMigration.ApplyCurrent();
   var menu=UnityEngine.Object.FindFirstObjectByType<StartMenuView>(FindObjectsInactive.Include);var handbook=menu.GetComponent<TutorialManualView>();var source=Resources.Load<TextAsset>("TutorialManual");
   Check(source!=null&&source.text.Split(new[]{"\n\n"},StringSplitOptions.None).Length==8,"All eight source paragraphs must be imported.");
   Check(menu.tutorial.text==source.text.Trim(),"In-game text must match the imported document.");
   Check(menu.tutorial.text.Contains("敌人的卡牌也能随意拖动改变位置")&&menu.tutorial.text.EndsWith("希望您顺利完成测试。"),"Middle and final paragraphs must be retained.");
   var scroll=handbook.scroll;Check(scroll.vertical&&!scroll.horizontal&&scroll.movementType==ScrollRect.MovementType.Clamped,"Manual must scroll vertically within viewport.");
   Check(scroll.viewport.GetComponent<RectMask2D>()!=null&&!menu.tutorial.raycastTarget,"Text must be masked without blocking scroll input.");
   TutorialManualView.Ensure(menu);Check(menu.tutorialPanel.GetComponentsInChildren<ScrollRect>(true).Length==1,"Repeated configuration must not duplicate scroll areas.");
   foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,800)})Render(menu,handbook,size.x,size.y);
   Debug.Log($"TUTORIAL MANUAL PASS: {checks} checks; full Word content, scroll bounds, return button, idempotency and rendered previews at two resolutions.");
  }
  private static void Render(StartMenuView menu,TutorialManualView handbook,int width,int height){
   var canvas=menu.GetComponent<Canvas>();foreach(Transform child in canvas.transform)child.gameObject.SetActive(child==menu.panel.transform);
   menu.panel.SetActive(true);menu.settingsPanel.SetActive(false);menu.replacePanel.SetActive(false);handbook.Open();
   var cameraObject=new GameObject("ManualPreviewCamera");var camera=cameraObject.AddComponent<Camera>();camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;
   var target=new RenderTexture(width,height,24);target.Create();camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   foreach(var transform in canvas.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=30;
   Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(handbook.scroll.content);
   Check(handbook.scroll.content.rect.height>=menu.tutorial.preferredHeight-1,"All text must fit in scroll content.");
   Check(handbook.scroll.content.rect.height>handbook.scroll.viewport.rect.height,"Long manual must have scrollable overflow.");
   Check(menu.closeManual.gameObject.activeInHierarchy,"Return button must stay visible.");
   Save(camera,target,"Logs/tutorial-manual-top-"+width+"x"+height+".png");
   handbook.scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();
   Vector3 bottom=handbook.scroll.viewport.InverseTransformPoint(handbook.scroll.content.TransformPoint(new Vector3(0,handbook.scroll.content.rect.yMin,0)));
   Check(Mathf.Abs(bottom.y-handbook.scroll.viewport.rect.yMin)<2,"Bottom scrolling must expose the final paragraph.");
   Save(camera,target,"Logs/tutorial-manual-bottom-"+width+"x"+height+".png");
   handbook.Open();Check(Mathf.Abs(handbook.scroll.verticalNormalizedPosition-1)<.001f,"Reopening must return to first paragraph.");
   menu.tutorialPanel.SetActive(false);Check(!menu.tutorialPanel.activeSelf,"Manual can close normally.");
   camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);
  }
  private static void Save(Camera camera,RenderTexture target,string path){camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
 }
}
