using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class CardTooltipArtSceneMigration {
  const string Root="Assets/RainbowLi";
  static void TextArea(Text text,Vector2 min,Vector2 max,int fontSize){var r=text.rectTransform;r.anchorMin=min;r.anchorMax=max;r.pivot=new Vector2(0,1);r.offsetMin=r.offsetMax=Vector2.zero;text.fontSize=fontSize;text.resizeTextForBestFit=true;text.resizeTextMinSize=14;text.resizeTextMaxSize=fontSize;text.alignment=TextAnchor.MiddleLeft;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.raycastTarget=false;foreach(var shadow in text.GetComponents<Shadow>())shadow.enabled=false;}
  static void Apply(){
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null||view.tooltip==null)throw new InvalidOperationException("缺少卡牌效果TooltipView");
   string art=Root+"/Art/GJ 战斗ui分解/二级ui.png";var importer=(TextureImporter)AssetImporter.GetAtPath(art);importer.spriteBorder=new Vector4(500,170,180,200);importer.SaveAndReimport();
   var tip=view.tooltip;var image=tip.panel.GetComponent<Image>();var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/GJ 战斗ui分解/二级ui.png");if(sprite==null)throw new InvalidOperationException("缺少二级ui.png Sprite");
   image.sprite=sprite;image.color=Color.white;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=874f/720f;image.material=null;image.preserveAspect=false;image.raycastTarget=false;foreach(var shadow in tip.panel.GetComponents<Shadow>())shadow.enabled=false;
   tip.illustrated=true;tip.artWidth=720;var root=(RectTransform)tip.panel.transform;root.pivot=new Vector2(0,1);root.sizeDelta=new Vector2(720,416);
   TextArea(tip.title,new Vector2(.17f,.65f),new Vector2(.54f,.755f),22);tip.title.color=new Color(.52f,.89f,.91f);tip.title.fontStyle=FontStyle.Normal;
   TextArea(tip.body,new Vector2(.205f,.355f),new Vector2(.775f,.605f),19);tip.body.color=Color.white;tip.body.alignment=TextAnchor.UpperLeft;
   tip.title.transform.SetAsLastSibling();tip.body.transform.SetAsLastSibling();foreach(var g in tip.panel.GetComponentsInChildren<Graphic>(true))g.raycastTarget=false;
   tip.panel.SetActive(false);EditorUtility.SetDirty(tip);EditorUtility.SetDirty(image);EditorUtility.SetDirty(tip.title);EditorUtility.SetDirty(tip.body);
  }
  [MenuItem("RainbowLi/UI/应用卡牌效果二级UI")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请先打开RainbowLi场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("PASS: Secondary card-effect UI artwork configured.");EditorApplication.Exit(0);}
 }
}
