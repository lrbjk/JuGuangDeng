using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public sealed class EffectPoolWeightEditorWindow:EditorWindow {
  EffectPoolWeightTable table;EffectPoolWeightRow[] pending;Vector2 scroll;string message;
  [MenuItem("RainbowLi/配表/卡牌效果池权重编辑器")]public static void OpenCards()=>OpenDefault(EffectPoolKind.Card);
  [MenuItem("RainbowLi/配表/牌序效果池权重编辑器")]public static void OpenSequences()=>OpenDefault(EffectPoolKind.Sequence);
  static void OpenDefault(EffectPoolKind kind){var path="Assets/RainbowLi/Data/"+(kind==EffectPoolKind.Card?"DefaultCardPoolWeights.asset":"DefaultSequencePoolWeights.asset");Open(AssetDatabase.LoadAssetAtPath<EffectPoolWeightTable>(path));}
  public static void Open(EffectPoolWeightTable asset){var w=GetWindow<EffectPoolWeightEditorWindow>("效果池权重配表");w.table=asset;w.pending=null;w.minSize=new Vector2(640,400);}
  void Guard(Action a){try{a();}catch(Exception e){message=e.Message;}}
  void OnGUI(){var next=(EffectPoolWeightTable)EditorGUILayout.ObjectField("效果池权重表",table,typeof(EffectPoolWeightTable),false);if(next!=table){table=next;pending=null;message=null;}
   EditorGUILayout.HelpBox("先按这里的权重选择效果池，再按池内效果权重选择效果。权重0表示不参与；总和不必为100。此入口配置池选择数据，现有随机生成仍需运行时接入新快照。",MessageType.Info);
   using(new EditorGUILayout.HorizontalScope()){
    if(GUILayout.Button("新建权重表")){var path=EditorUtility.SaveFilePanelInProject("新建效果池权重表","EffectPoolWeights","asset","保存到RainbowLi","Assets/RainbowLi/Data");if(path.StartsWith("Assets/RainbowLi/",StringComparison.Ordinal)){table=CreateInstance<EffectPoolWeightTable>();AssetDatabase.CreateAsset(table,path);AssetDatabase.SaveAssets();pending=null;}}
    GUI.enabled=table!=null;
    if(GUILayout.Button("导入Excel预览"))Guard(()=>{var path=EditorUtility.OpenFilePanel("权重表","","xlsx");if(path.Length>0){pending=EffectPoolWeightWorkbook.Read(path,table.kind);message="读取"+pending.Length+"行，应用后替换当前表。";}});
    if(GUILayout.Button("导出Excel"))Guard(()=>{var path=EditorUtility.SaveFilePanel("导出权重表","",table.kind==EffectPoolKind.Card?"卡牌效果池权重表":"卡序效果池权重表","xlsx");if(path.Length>0){EffectPoolWeightWorkbook.Write(path,table.rows);message="导出完成。";}});
    if(GUILayout.Button("校验"))Guard(()=>{table.ValidateTable();message="校验通过。";});GUI.enabled=true;
   }
   if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.Info);if(table==null)return;
   if(pending!=null){EditorGUILayout.LabelField("导入预览（将替换当前"+(table.rows?.Length??0)+"行）",EditorStyles.boldLabel);double sum=pending.Sum(r=>(double)r.weight);foreach(var r in pending)EditorGUILayout.LabelField(r.id+" / "+r.name+" / 权重"+r.weight+" / "+(100*r.weight/sum).ToString("0.00")+"%");using(new EditorGUILayout.HorizontalScope()){if(GUILayout.Button("应用导入（可撤销）")){Undo.RecordObject(table,"导入效果池权重");table.rows=pending.Select(r=>r.Copy()).ToArray();pending=null;EditorUtility.SetDirty(table);AssetDatabase.SaveAssetIfDirty(table);}if(GUILayout.Button("取消"))pending=null;}return;}
   var so=new SerializedObject(table);so.Update();var kind=so.FindProperty("kind");kind.enumValueIndex=EditorGUILayout.Popup("权重表类型",kind.enumValueIndex,new[]{"卡牌效果池","牌序效果池"});var rows=so.FindProperty("rows");
   using(new EditorGUILayout.HorizontalScope()){EditorGUILayout.LabelField("编号",GUILayout.Width(155));EditorGUILayout.LabelField("名称",GUILayout.Width(170));EditorGUILayout.LabelField("权重",GUILayout.Width(90));EditorGUILayout.LabelField("抽取比例（只读）");}
   double total=table.TotalWeight;scroll=EditorGUILayout.BeginScrollView(scroll);for(int i=0;i<rows.arraySize;i++){var row=rows.GetArrayElementAtIndex(i);using(new EditorGUILayout.HorizontalScope()){
    EditorGUILayout.PropertyField(row.FindPropertyRelative("id"),GUIContent.none,GUILayout.Width(155));EditorGUILayout.PropertyField(row.FindPropertyRelative("name"),GUIContent.none,GUILayout.Width(170));EditorGUILayout.PropertyField(row.FindPropertyRelative("weight"),GUIContent.none,GUILayout.Width(90));float weight=row.FindPropertyRelative("weight").floatValue;GUILayout.Label(total>0?(weight*100/total).ToString("0.00")+"%":"—");if(GUILayout.Button("删除",GUILayout.Width(45))){rows.DeleteArrayElementAtIndex(i);break;}
   }}EditorGUILayout.EndScrollView();if(GUILayout.Button("新增效果池")){int at=rows.arraySize;rows.InsertArrayElementAtIndex(at);var r=rows.GetArrayElementAtIndex(at);r.FindPropertyRelative("id").stringValue="";r.FindPropertyRelative("name").stringValue="";r.FindPropertyRelative("weight").floatValue=10;}
   so.ApplyModifiedProperties();EditorGUILayout.LabelField("总权重",table.TotalWeight.ToString("0.###"));if(GUILayout.Button("保存配置")){AssetDatabase.SaveAssetIfDirty(table);message="已保存。";}
  }
 }
 [CustomEditor(typeof(EffectPoolWeightTable))]public sealed class EffectPoolWeightInspector:UnityEditor.Editor {public override void OnInspectorGUI(){if(GUILayout.Button("打开效果池权重编辑器"))EffectPoolWeightEditorWindow.Open((EffectPoolWeightTable)target);DrawDefaultInspector();}}
}
