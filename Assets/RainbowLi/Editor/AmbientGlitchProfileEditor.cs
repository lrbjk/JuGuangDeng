using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 [CustomEditor(typeof(AmbientGlitchProfile))]
 public sealed class AmbientGlitchProfileEditor : Editor {
  public const string AssetPath="Assets/RainbowLi/Resources/AmbientGlitchProfile.asset";
  public static AmbientGlitchProfile GetOrCreate(){
   var profile=AssetDatabase.LoadAssetAtPath<AmbientGlitchProfile>(AssetPath);if(profile!=null)return profile;
   if(!AssetDatabase.IsValidFolder("Assets/RainbowLi/Resources"))AssetDatabase.CreateFolder("Assets/RainbowLi","Resources");
   profile=CreateInstance<AmbientGlitchProfile>();AssetDatabase.CreateAsset(profile,AssetPath);AssetDatabase.SaveAssetIfDirty(profile);return profile;
  }
  [MenuItem("RainbowLi/表现/花屏配置")]
  public static void Open(){Selection.activeObject=GetOrCreate();EditorGUIUtility.PingObject(Selection.activeObject);}
  public override void OnInspectorGUI(){
   serializedObject.Update();Draw("effectEnabled","启用花屏");Draw("minIntervalSeconds","最短间隔（秒）");Draw("maxIntervalSeconds","最长间隔（秒）");Draw("duration","持续时间（秒）");Draw("intensity","花屏强度");Draw("horizontalShift","最大横向错位比例");Draw("noiseOpacity","噪点不透明度");Draw("changesPerSecond","图案变化频率（次/秒）");serializedObject.ApplyModifiedProperties();
   EditorGUILayout.HelpBox("间隔上下限相同即为固定间隔。运行时修改间隔会重新计时；其他参数即时应用。保存配置后，退出播放和打包仍会使用这些数值。",MessageType.Info);
   if(GUILayout.Button("保存配置")){((AmbientGlitchProfile)target).Validate();EditorUtility.SetDirty(target);AssetDatabase.SaveAssetIfDirty(target);}
   var effect=Object.FindFirstObjectByType<AmbientScreenFlicker>();
   using(new EditorGUI.DisabledScope(!Application.isPlaying||effect==null||!effect.isActiveAndEnabled||!((AmbientGlitchProfile)target).effectEnabled)){
    if(GUILayout.Button("立即预览一次花屏")){EditorApplication.ExecuteMenuItem("Window/General/Game");effect.profile=(AmbientGlitchProfile)target;effect.PreviewNow();}
   }
   if(!Application.isPlaying)EditorGUILayout.HelpBox("进入播放模式后，可立即预览，无需等待计时。",MessageType.None);
  }
  private void Draw(string name,string label){EditorGUILayout.PropertyField(serializedObject.FindProperty(name),new GUIContent(label));}
 }
 [CustomEditor(typeof(AmbientScreenFlicker))]
 public sealed class AmbientScreenFlickerEditor : Editor {
  public override void OnInspectorGUI(){
   serializedObject.Update();EditorGUILayout.PropertyField(serializedObject.FindProperty("profile"),new GUIContent("花屏策划配置"));serializedObject.ApplyModifiedProperties();
   if(GUILayout.Button("打开花屏策划配置")){var effect=(AmbientScreenFlicker)target;Selection.activeObject=effect.profile!=null?effect.profile:AmbientGlitchProfileEditor.GetOrCreate();}
  }
 }
}
