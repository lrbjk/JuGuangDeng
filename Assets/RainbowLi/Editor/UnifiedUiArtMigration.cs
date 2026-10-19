using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;

namespace RainbowLi.EditorTools {
 public static class UnifiedUiArtMigration {
  const string Root="Assets/RainbowLi",Art=Root+"/Art/GJ 各种ui/";
  static readonly Color Cyan=new Color(.58f,.88f,.87f);
  static Sprite buttonSprite,dialogSprite,headerSprite,trackSprite,handleSprite;
  public static int Controls,Artwork,Content;

  [MenuItem("RainbowLi/UI/统一设置与全部按钮美术")]
  public static void ApplyCurrent(){
   if(Application.isPlaying)throw new InvalidOperationException("请先停止游戏，再应用场景美术。");
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();
   if(view==null)throw new InvalidOperationException("请打开 RainbowLiGame 场景。");
   var buttons=view.GetComponentsInChildren<Button>(true);var events=buttons.Select(b=>b.onClick).ToArray();
   Undo.RegisterFullObjectHierarchyUndo(view.gameObject,"统一设置与按钮美术");
   Apply(view);
   if(view.GetComponentsInChildren<Button>(true).Length!=buttons.Length||buttons.Where((b,i)=>b==null||!ReferenceEquals(b.onClick,events[i])).Any())throw new InvalidOperationException("按钮数量或事件引用发生变化，请撤销本次美术迁移。");
   EditorSceneManager.MarkSceneDirty(view.gameObject.scene);AssetDatabase.SaveAssets();
   Debug.Log($"UNIFIED UI PASS: {Controls} themed controls, {Artwork} artwork buttons, {Content} content buttons preserved.");
  }
  [MenuItem("RainbowLi/场景预览/设置界面")]
  public static void PreviewSettings(){
   if(Application.isPlaying)return;
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null)return;
   var panel=view.battleHud.settingsPanel;Undo.RecordObject(panel,"预览设置界面");panel.SetActive(true);panel.transform.SetAsLastSibling();
   Selection.activeGameObject=panel;SceneView.RepaintAll();
  }
  public static void Run(){
   var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");
   ApplyCurrent();EditorSceneManager.SaveScene(scene);
  }
  public static void Apply(GameView view){
   buttonSprite=Slice("公共按钮","UiThemeButton",new Vector4(12,12,12,12));
   dialogSprite=Slice("大框","UiThemeDialog",new Vector4(20,20,20,20));
   headerSprite=Slice("界面名称","UiThemeHeading",new Vector4(18,14,18,14));
   trackSprite=Slice("小框1","UiThemeTrack",new Vector4(8,8,8,8));
   handleSprite=AssetDatabase.LoadAllAssetsAtPath(Art+"卡组/滑键.png").OfType<Sprite>().First();
   Settings(view);
   Controls=Artwork=Content=0;
   foreach(var button in view.GetComponentsInChildren<Button>(true))StyleButton(button);
   foreach(var component in view.GetComponentsInChildren<Component>(true)){
    if(component==null)continue;EditorUtility.SetDirty(component);
    if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
   }
   // Runtime-created action/status/operation controls clone these scene templates. Standalone
   // prefab buttons use the same appearance when instantiated from other entry points.
   foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root+"/Prefabs"})){
    string path=AssetDatabase.GUIDToAssetPath(guid);var prefab=PrefabUtility.LoadPrefabContents(path);
    try{
     bool changed=false;
     foreach(var button in prefab.GetComponentsInChildren<Button>(true))if(!IsContent(button)&&button.GetComponent<MainMenuButtonArt>()==null){StyleButton(button,false);changed=true;}
     if(changed)PrefabUtility.SaveAsPrefabAsset(prefab,path);
    }finally{PrefabUtility.UnloadPrefabContents(prefab);}
   }
  }
  public static bool IsContent(Button button)=>button.GetComponentInParent<CardView>(true)!=null||button.GetComponentInParent<LevelNodeView>(true)!=null||button.GetComponentInParent<SequenceView>(true)!=null||button.GetComponentInParent<DeckRowView>(true)!=null;
  public static void StyleButton(Button button,bool count=true){
   if(IsContent(button)){if(count)Content++;return;}
   if(button.GetComponent<MainMenuButtonArt>()!=null){if(count)Artwork++;return;}
   var image=button.targetGraphic as Image;
   if(image==null)image=button.GetComponent<Image>();
   bool dedicated=image!=null&&image.sprite!=null&&image.sprite.name!="UiThemeButton";
   if(!dedicated){
    if(image==null){image=Image("UiThemeSurface",button.transform,buttonSprite);Stretch(image.rectTransform,Vector2.zero,Vector2.zero);image.transform.SetAsFirstSibling();}
    image.sprite=buttonSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;image.raycastTarget=true;
    button.targetGraphic=image;if(count)Controls++;
    foreach(var outline in button.GetComponents<Outline>())outline.enabled=false;
    foreach(var text in button.GetComponentsInChildren<Text>(true)){
     if(text.GetComponentInParent<Button>()!=button)continue;
     text.font=SourceHanFontMigration.Body;text.color=Cyan;text.raycastTarget=false;
     text.resizeTextForBestFit=true;text.resizeTextMaxSize=Mathf.Max(14,text.fontSize);text.resizeTextMinSize=Mathf.Min(12,text.resizeTextMaxSize);
    }
   }else if(count)Artwork++;
   button.transition=Selectable.Transition.ColorTint;
   var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.14f,1.3f,1.3f,1);
   colors.selectedColor=new Color(1.1f,1.25f,1.25f,1);colors.pressedColor=new Color(.6f,.8f,.83f,1);
   colors.disabledColor=new Color(.34f,.43f,.44f,.65f);colors.colorMultiplier=1;colors.fadeDuration=.1f;button.colors=colors;
  }
  static void Settings(GameView view){
   var ui=view.battleHud.settingsPanel.GetComponent<SettingsPanelView>();
   if(ui==null)throw new InvalidOperationException("设置窗口引用缺失。");
   var overlay=(RectTransform)ui.transform;
   // This shared overlay must work after the start menu is hidden.
   overlay.SetParent(view.transform,false);Stretch(overlay,Vector2.zero,Vector2.zero);
   var shade=overlay.GetComponent<Image>();shade.color=new Color(.005f,.015f,.025f,.88f);shade.raycastTarget=true;
   foreach(var outline in overlay.GetComponents<Outline>())outline.enabled=false;
   var dialog=(RectTransform)overlay.Find("Dialog");dialog.anchorMin=dialog.anchorMax=dialog.pivot=Vector2.one*.5f;
   dialog.anchoredPosition=Vector2.zero;dialog.sizeDelta=new Vector2(960,660);dialog.localScale=Vector3.one;
   var frame=dialog.GetComponent<Image>();frame.sprite=dialogSprite;frame.type=UnityEngine.UI.Image.Type.Sliced;frame.color=Color.white;
   foreach(var outline in dialog.GetComponents<Outline>())outline.enabled=false;
   var header=Image("UiSettingsHeader",dialog,headerSprite);Place(header.rectTransform,32,24,896,80);header.type=UnityEngine.UI.Image.Type.Sliced;header.transform.SetAsFirstSibling();
   var caption=dialog.Find("Caption").GetComponent<Text>();Place(caption.rectTransform,60,34,840,60);Text(caption,30,TextAnchor.MiddleLeft);
   var volumeLabel=dialog.Find("VolumeLabel").GetComponent<Text>();Place(volumeLabel.rectTransform,60,142,580,48);Text(volumeLabel,25,TextAnchor.MiddleLeft);
   Place(ui.volumeValue.rectTransform,720,142,180,48);Text(ui.volumeValue,26,TextAnchor.MiddleRight);
   var slider=(RectTransform)ui.volume.transform;Place(slider,60,208,840,52);
   var sliderImage=slider.GetComponent<Image>();if(sliderImage!=null){sliderImage.color=Color.clear;sliderImage.raycastTarget=true;}
   foreach(var outline in slider.GetComponents<Outline>())outline.enabled=false;
   var track=(RectTransform)slider.Find("Track");Stretch(track,new Vector2(0,16),new Vector2(0,16));
   var trackImage=track.GetComponent<Image>();trackImage.sprite=trackSprite;trackImage.type=UnityEngine.UI.Image.Type.Sliced;trackImage.color=new Color(.3f,.5f,.52f,1);trackImage.raycastTarget=false;
   foreach(var outline in track.GetComponents<Outline>())outline.enabled=false;
   var fillArea=(RectTransform)slider.Find("FillArea");Stretch(fillArea,new Vector2(18,22),new Vector2(18,22));
   var fill=ui.volume.fillRect.GetComponent<Image>();fill.color=new Color(.28f,.86f,.87f);fill.raycastTarget=false;
   foreach(var outline in fill.GetComponents<Outline>())outline.enabled=false;
   var handleArea=(RectTransform)slider.Find("HandleArea");Stretch(handleArea,new Vector2(18,0),new Vector2(18,0));
   ui.volume.handleRect.sizeDelta=new Vector2(38,-6);
   var handle=ui.volume.handleRect.GetComponent<Image>();handle.sprite=handleSprite;handle.type=UnityEngine.UI.Image.Type.Simple;handle.preserveAspect=true;handle.color=Color.white;handle.raycastTarget=false;
   foreach(var outline in handle.GetComponents<Outline>())outline.enabled=false;
   ui.volume.targetGraphic=handle;ui.volume.transition=Selectable.Transition.ColorTint;
   var sliderColors=ui.volume.colors;sliderColors.normalColor=Color.white;sliderColors.highlightedColor=new Color(1.2f,1.3f,1.3f);sliderColors.pressedColor=new Color(.6f,.85f,.85f);sliderColors.selectedColor=sliderColors.highlightedColor;ui.volume.colors=sliderColors;
   Row(dialog,"UiSettingsSpeedRow",296);Row(dialog,"UiSettingsShakeRow",394);
   Place(ui.speedValue.rectTransform,76,304,490,64);Text(ui.speedValue,25,TextAnchor.MiddleLeft);
   Place((RectTransform)ui.speed.transform,606,304,278,64);
   Place(ui.shakeValue.rectTransform,76,402,490,64);Text(ui.shakeValue,25,TextAnchor.MiddleLeft);
   Place((RectTransform)ui.shake.transform,606,402,278,64);
   var hint=dialog.Find("Hint").GetComponent<Text>();Place(hint.rectTransform,60,486,840,42);Text(hint,20,TextAnchor.MiddleLeft);
   Place((RectTransform)ui.returnMenu.transform,60,556,390,64);Place((RectTransform)ui.close.transform,510,556,390,64);
   foreach(var label in dialog.GetComponentsInChildren<Text>(true)){
    if(label.GetComponentInParent<Button>()!=null)continue;
    foreach(var old in label.GetComponents<Outline>())old.enabled=false;
   }
   header.transform.SetAsFirstSibling();
  }
  static void Row(RectTransform dialog,string name,float y){
   var row=Image(name,dialog,null);Place(row.rectTransform,60,y,840,80);row.color=new Color(.015f,.065f,.075f,.8f);row.transform.SetAsFirstSibling();
  }
  static Sprite Slice(string file,string name,Vector4 border){
   string path=Art+file+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var entries=importer.spritesheet.ToList();
   entries.RemoveAll(s=>s.name==name);entries.Add(new SpriteMetaData{name=name,rect=new Rect(0,0,texture.width,texture.height),alignment=9,pivot=Vector2.one*.5f,border=border});
   importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
   importer.spritesheet=entries.ToArray();importer.SaveAndReimport();return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(s=>s.name==name);
  }
  static Image Image(string name,Transform parent,Sprite sprite){
   var child=parent.Find(name);var go=child==null?new GameObject(name,typeof(RectTransform),typeof(Image)):child.gameObject;
   go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;return image;
  }
  static void Text(Text label,int size,TextAnchor alignment){label.font=SourceHanFontMigration.Body;label.fontSize=size;label.color=Cyan;label.alignment=alignment;label.raycastTarget=false;}
  static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
  static void Stretch(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=Vector2.one*.5f;r.offsetMin=min;r.offsetMax=-max;r.localScale=Vector3.one;}
 }
}
