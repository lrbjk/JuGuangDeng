using System.Linq;
using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public sealed class CardTableImportPreviewWindow:EditorWindow {
  CardTableImportPlan plan;Vector2 scroll;string error="";
  public static void Open(CardTableImportPlan plan){var w=CreateInstance<CardTableImportPreviewWindow>();w.plan=plan;w.titleContent=new GUIContent("卡牌导入预览");w.minSize=new Vector2(760,480);w.ShowUtility();}
  void OnGUI(){
   if(plan==null||plan.Target==null){EditorGUILayout.HelpBox("源配表已关闭，请重新导入。",MessageType.Info);return;}
   EditorGUILayout.LabelField(plan.Target.name+" / "+(plan.Replace?"整表替换预览":"按编号合并预览"),EditorStyles.boldLabel);
   EditorGUILayout.HelpBox("当前资产还没有改变。勾选要应用的差异；删除项默认不勾选。应用后可 Ctrl+Z 撤销，再到配表窗口保存。",MessageType.Info);
   EditorGUILayout.LabelField(string.Join(" · ",plan.Changes.GroupBy(c=>c.Kind).Select(g=>g.Key+" "+g.Count()))+" · 错误 "+plan.Errors.Count);
   EditorGUILayout.BeginHorizontal();if(GUILayout.Button("勾选新增和修改"))foreach(var c in plan.Changes)c.Selected=c.Kind=="新增"||c.Kind=="修改";if(GUILayout.Button("清空选择"))foreach(var c in plan.Changes)c.Selected=false;EditorGUILayout.EndHorizontal();
   scroll=EditorGUILayout.BeginScrollView(scroll);
   foreach(var issue in plan.Errors)EditorGUILayout.HelpBox(issue,MessageType.Error);
   foreach(var c in plan.Changes){EditorGUILayout.BeginVertical(EditorStyles.helpBox);using(new EditorGUI.DisabledScope(c.Kind=="不变"))c.Selected=EditorGUILayout.ToggleLeft(c.Kind+" · "+c.Id+" · "+(c.After?.name??c.Before?.name),c.Selected);EditorGUILayout.LabelField(c.Details,EditorStyles.wordWrappedLabel);EditorGUILayout.EndVertical();}
   var issues=CardTableValidation.Check(plan.Result(),plan.Target is EnemyCardTable);foreach(var issue in issues)EditorGUILayout.HelpBox("应用结果 · "+issue,MessageType.Error);EditorGUILayout.EndScrollView();
   if(error!="")EditorGUILayout.HelpBox(error,MessageType.Error);
   EditorGUILayout.BeginHorizontal();using(new EditorGUI.DisabledScope(plan.Errors.Count>0||issues.Length>0||!plan.Changes.Any(c=>c.Selected)))if(GUILayout.Button("应用已勾选差异")){try{plan.Apply();Close();}catch(System.Exception e){error=e.Message;}}
   if(GUILayout.Button("取消，不修改配表"))Close();EditorGUILayout.EndHorizontal();
  }
 }
}
