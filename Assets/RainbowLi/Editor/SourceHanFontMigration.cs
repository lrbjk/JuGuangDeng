using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.EditorTools {
 public static class SourceHanFontMigration {
  const string Root="Assets/RainbowLi";
  public static Font Body=>AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/SourceHanSansCN-Bold.otf");
  public static Font Heading=>AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/SourceHanSansCN-Heavy.otf");
  public static bool IsHeading(Text text){
   if(text.GetComponentInParent<Button>()!=null)return false;
   string name=text.name.ToLowerInvariant();
   return name.Contains("title")||name.Contains("heading")||name.Contains("header")||(text.fontSize>=28&&(text.text??"").Length<=24);
  }
  static int Apply(GameObject root){
   int count=0;
   foreach(var text in root.GetComponentsInChildren<Text>(true)){
    text.font=IsHeading(text)?Heading:Body;
    text.fontStyle=text.fontStyle==FontStyle.Italic||text.fontStyle==FontStyle.BoldAndItalic?FontStyle.Italic:FontStyle.Normal;
    EditorUtility.SetDirty(text);if(PrefabUtility.IsPartOfPrefabInstance(text))PrefabUtility.RecordPrefabInstancePropertyModifications(text);count++;
   }
   foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true)){
    if(component==null||component.GetType().Name!="ContainmentPrototype")continue;
    var so=new SerializedObject(component);so.FindProperty("bodyFont").objectReferenceValue=Body;so.FindProperty("titleFont").objectReferenceValue=Heading;so.ApplyModifiedPropertiesWithoutUndo();
    if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
   }
   return count;
  }
  [MenuItem("RainbowLi/UI/全部文字应用思源黑体")]
  public static void ApplyAll(){
   if(Application.isPlaying)throw new InvalidOperationException("请先停止运行，再修改场景字体。");
   if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   if(Body==null||Heading==null)throw new InvalidOperationException("缺少思源黑体 Bold 或 Heavy 字体资源。");
   int count=0;
   foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root+"/Prefabs"})){
    string path=AssetDatabase.GUIDToAssetPath(guid);var prefab=PrefabUtility.LoadPrefabContents(path);
    try{count+=Apply(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
   }
   foreach(string guid in AssetDatabase.FindAssets("t:Scene",new[]{Root+"/Scenes"})){
    string path=AssetDatabase.GUIDToAssetPath(guid);var scene=EditorSceneManager.OpenScene(path);
    foreach(var root in scene.GetRootGameObjects())count+=Apply(root);
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }
   AssetDatabase.SaveAssets();Debug.Log("Source Han fonts applied to "+count+" authored UI texts and legacy prototype fonts.");
  }
 }
}
