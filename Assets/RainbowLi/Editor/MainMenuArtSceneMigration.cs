using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;

namespace RainbowLi.EditorTools {
 public static class MainMenuArtSceneMigration {
  const string Root="Assets/RainbowLi",Art=Root+"/Art/GJ 各种ui/主界面/";
  static readonly Color Cyan=new Color(.58f,.88f,.87f);

  [MenuItem("RainbowLi/UI/应用主界面美术到当前场景")]
  public static void ApplyCurrent(){
   if(Application.isPlaying)return;
   var scene=EditorSceneManager.GetActiveScene();
   if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");
   Undo.RegisterFullObjectHierarchyUndo(UnityEngine.Object.FindFirstObjectByType<GameView>().gameObject,"应用主界面美术");
   Apply();PreviewCurrent();EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();
  }
  [MenuItem("RainbowLi/场景预览/主界面")]
  public static void PreviewCurrent(){
   if(Application.isPlaying)return;
   var menu=UnityEngine.Object.FindFirstObjectByType<StartMenuView>();
   if(menu==null)return;
   Undo.RecordObject(menu.panel,"主界面预览");menu.panel.SetActive(true);
   foreach(var overlay in new[]{menu.settingsPanel,menu.tutorialPanel,menu.replacePanel})if(overlay!=null){Undo.RecordObject(overlay,"主界面预览");overlay.SetActive(false);}
   menu.panel.transform.SetAsLastSibling();Selection.activeGameObject=menu.panel;SceneView.RepaintAll();
  }
  public static void Run(){
   var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");
   Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   Debug.Log("MAIN MENU ART PASS: editable responsive menu saved.");
  }

  public static void Apply(){
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();
   if(view==null)throw new InvalidOperationException("场景缺少 GameView。");
   var menu=view.GetComponent<StartMenuView>();
   if(menu==null||menu.panel==null)throw new InvalidOperationException("请先接入游戏开始界面与菜单流程。");
   ImportBackground();
   var panel=(RectTransform)menu.panel.transform;
   Stretch(panel,Vector2.zero,Vector2.zero);Clear(panel,true);

   var right=Image("ArtMainSurface",panel,S("主界面背景","MenuMainSurface"));
   Stretch(right.rectTransform,new Vector2(442,0),Vector2.zero);right.type=UnityEngine.UI.Image.Type.Sliced;right.transform.SetAsFirstSibling();
   var sidebar=(RectTransform)panel.Find("Sidebar");
   sidebar.anchorMin=Vector2.zero;sidebar.anchorMax=new Vector2(0,1);sidebar.pivot=new Vector2(0,1);
   sidebar.anchoredPosition=Vector2.zero;sidebar.sizeDelta=new Vector2(442,0);sidebar.localScale=Vector3.one;
   Clear(sidebar);var left=Image("ArtSidebar",sidebar,S("主界面背景","MenuSidebar"));
   Stretch(left.rectTransform,Vector2.zero,Vector2.zero);left.type=UnityEngine.UI.Image.Type.Sliced;left.transform.SetAsFirstSibling();

   var credits=(RectTransform)menu.credits.transform.parent;Clear(credits);
   Stretch(credits,new Vector2(88,470),new Vector2(24,100));
   var heading=Image("ArtCreditsHeading",sidebar,S("制作人员","制作人员_0"));
   Place(heading.rectTransform,sidebar,118,108,253,58);
   // Remove only the old generated heading; preserve author-supplied credit names.
   const string oldHeading="制作人员名单\n（研发团队）";
   if(menu.credits.text.StartsWith(oldHeading,StringComparison.Ordinal))menu.credits.text=menu.credits.text.Substring(oldHeading.Length).TrimStart('\r','\n');
   Stretch(menu.credits.rectTransform,new Vector2(12,12),new Vector2(10,82));Style(menu.credits,20,TextAnchor.UpperLeft);
   var flavor=(RectTransform)menu.flavor.transform.parent;Clear(flavor);
   Bottom(flavor,sidebar,100,144,285,238);
   Stretch(menu.flavor.rectTransform,new Vector2(10,10),new Vector2(10,10));Style(menu.flavor,23,TextAnchor.UpperLeft);
   Skin(menu.manual,sidebar,"教学手册",null);Bottom((RectTransform)menu.manual.transform,sidebar,74,26,344,78);

   var main=(RectTransform)menu.introductionScroll.transform;
   main.SetParent(right.transform,false);Stretch(main,new Vector2(112,304),new Vector2(96,128));Clear(main,true);
   var viewport=menu.introductionScroll.viewport;Stretch(viewport,Vector2.zero,new Vector2(24,0));Clear(viewport);
   var text=menu.introduction.rectTransform;
   text.anchorMin=new Vector2(0,1);text.anchorMax=Vector2.one;text.pivot=new Vector2(.5f,1);
   text.anchoredPosition=new Vector2(0,-12);text.sizeDelta=new Vector2(-32,0);text.localScale=Vector3.one;
   Style(menu.introduction,28,TextAnchor.UpperLeft);
   var textOutline=menu.introduction.GetComponent<Outline>()??menu.introduction.gameObject.AddComponent<Outline>();
   textOutline.effectColor=new Color(.015f,.03f,.03f,.9f);textOutline.effectDistance=new Vector2(1.5f,-1.5f);
   var fitter=text.GetComponent<ContentSizeFitter>()??text.gameObject.AddComponent<ContentSizeFitter>();
   fitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
   var scroll=menu.introductionScroll;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
   if(scroll.verticalScrollbar!=null){
    var track=(RectTransform)scroll.verticalScrollbar.transform;
    track.anchorMin=new Vector2(1,0);track.anchorMax=Vector2.one;track.pivot=new Vector2(1,1);
    track.offsetMin=new Vector2(-12,12);track.offsetMax=new Vector2(0,-12);
    scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
    var handle=scroll.verticalScrollbar.handleRect.GetComponent<Image>();if(handle!=null)handle.color=Cyan;
   }

   var actions=(RectTransform)menu.settings.transform.parent;actions.SetParent(right.transform,false);
   actions.anchorMin=actions.anchorMax=Vector2.zero;actions.anchorMax=new Vector2(1,0);actions.pivot=new Vector2(.5f,0);
   actions.offsetMin=new Vector2(90,0);actions.offsetMax=new Vector2(-46,196);Clear(actions);
   Action(menu.settings,actions,"设置","设置亮",0);
   Action(menu.start,actions,"开始测试","开始测试-亮",1);
   Action(menu.continueGame,actions,"继续测试","继续测试亮",2);
   Action(menu.quit,actions,"退出测试","退出测试亮",3);
   if(menu.resume!=null){
    Place((RectTransform)menu.resume.transform,actions,0,0,260,48);
    var r=(RectTransform)menu.resume.transform;r.anchorMin=r.anchorMax=new Vector2(1,0);r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(-12,4);
   }
   if(menu.message!=null){
    var r=menu.message.rectTransform;Stretch(r,new Vector2(12,0),new Vector2(290,150));Style(menu.message,20,TextAnchor.MiddleLeft);
   }
   foreach(var overlay in new[]{menu.settingsPanel,menu.tutorialPanel,menu.replacePanel}){
    if(overlay==null)continue;var r=(RectTransform)overlay.transform;
    // Settings is shared with battle: it must remain a sibling of the menu.
    r.SetParent(overlay==menu.settingsPanel?view.transform:panel,false);Stretch(r,Vector2.zero,Vector2.zero);
    var dialog=r.Find("Dialog") as RectTransform;if(dialog!=null){dialog.anchorMin=dialog.anchorMax=dialog.pivot=Vector2.one*.5f;dialog.anchoredPosition=Vector2.zero;}
    r.SetAsLastSibling();
   }
   foreach(var component in panel.GetComponentsInChildren<Component>(true)){
    if(component==null)continue;EditorUtility.SetDirty(component);
    if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
   }
   EditorUtility.SetDirty(menu);
  }

  static void ImportBackground(){
   string path=Art+"主界面背景.png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   var entries=importer.spritesheet.ToList();
   entries.RemoveAll(s=>s.name=="MenuSidebar"||s.name=="MenuMainSurface");
   entries.Add(new SpriteMetaData{name="MenuSidebar",rect=new Rect(0,0,442,1080),pivot=Vector2.one*.5f,alignment=9,border=new Vector4(85,445,40,180)});
   entries.Add(new SpriteMetaData{name="MenuMainSurface",rect=new Rect(442,0,1478,1080),pivot=Vector2.one*.5f,alignment=9,border=new Vector4(90,270,90,120)});
   importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
   importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
   importer.spritesheet=entries.ToArray();importer.SaveAndReimport();
  }
  static Sprite S(string file,string name=null){
   var sprites=AssetDatabase.LoadAllAssetsAtPath(Art+file+".png").OfType<Sprite>();
   return name==null?sprites.First():sprites.First(s=>s.name==name);
  }
  static Image Image(string name,Transform parent,Sprite sprite){
   var child=parent.Find(name);var go=child==null?new GameObject(name,typeof(RectTransform),typeof(Image)):child.gameObject;
   go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;
   return image;
  }
  static void Skin(Button button,Transform parent,string normal,string highlighted){
   if(button==null)throw new InvalidOperationException("主菜单缺少按钮引用。");
   button.transform.SetParent(parent,false);Clear(button.transform,true);
   foreach(var label in button.GetComponentsInChildren<Text>(true))label.gameObject.SetActive(false);
   var image=button.GetComponent<Image>();image.sprite=S(normal);image.type=UnityEngine.UI.Image.Type.Simple;image.color=Color.white;image.raycastTarget=true;
   button.targetGraphic=image;button.transition=Selectable.Transition.None;
   var art=button.GetComponent<MainMenuButtonArt>()??button.gameObject.AddComponent<MainMenuButtonArt>();art.normal=image;
   if(highlighted!=null){
    var sprite=S(highlighted);var glow=Image("ArtHighlight",button.transform,sprite);
    // The supplied highlight textures have 20px of transparent glow padding on each side.
    Place(glow.rectTransform,button.transform,-20,-20,sprite.rect.width,sprite.rect.height);glow.enabled=false;art.glow=glow;
   }
   var audio=button.GetComponent<AudioUiFeedback>()??button.gameObject.AddComponent<AudioUiFeedback>();audio.mainMenuButton=true;
  }
  static void Action(Button button,RectTransform parent,string normal,string highlighted,int index){
   Skin(button,parent,normal,highlighted);var r=(RectTransform)button.transform;
   r.anchorMin=r.anchorMax=new Vector2((index+.5f)/4,0);r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(0,118);
   r.sizeDelta=new Vector2(283,86);r.localScale=Vector3.one;
  }
  static void Clear(Transform t,bool raycast=false){var graphic=t.GetComponent<Graphic>();if(graphic!=null){graphic.color=Color.clear;graphic.raycastTarget=raycast;}foreach(var outline in t.GetComponents<Outline>())outline.enabled=false;}
  static void Style(Text text,int size,TextAnchor alignment){text.font=SourceHanFontMigration.Body;text.fontSize=size;text.color=Cyan;text.alignment=alignment;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;}
  static void Stretch(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=Vector2.one*.5f;r.offsetMin=min;r.offsetMax=-max;r.localScale=Vector3.one;r.localRotation=Quaternion.identity;}
  static void Place(RectTransform r,Transform parent,float x,float y,float w,float h){r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;r.localRotation=Quaternion.identity;}
  static void Bottom(RectTransform r,Transform parent,float x,float y,float w,float h){Place(r,parent,x,y,w,h);r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);}
 }
}
