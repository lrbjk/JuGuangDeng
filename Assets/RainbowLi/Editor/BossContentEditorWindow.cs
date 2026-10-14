using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public sealed class BossContentEditorWindow:EditorWindow {
  BossContentTable table;BossContentRow[] pending;int selected;Vector2 scroll;string message;
  [MenuItem("RainbowLi/配表/BOSS内容编辑器")]
  public static void Open(){var w=GetWindow<BossContentEditorWindow>("BOSS策划配表");if(w.table==null)w.table=AssetDatabase.LoadAssetAtPath<BossContentTable>("Assets/RainbowLi/Data/DefaultBosses.asset");w.minSize=new Vector2(700,550);}
  public static void Open(BossContentTable asset){Open();var w=GetWindow<BossContentEditorWindow>();w.table=asset;w.pending=null;w.selected=0;}
  void Guard(Action action){try{action();}catch(Exception e){message=e.Message;}}
  void OnGUI(){var next=(BossContentTable)EditorGUILayout.ObjectField("BOSS内容表",table,typeof(BossContentTable),false);if(next!=table){table=next;selected=0;pending=null;}
   EditorGUILayout.HelpBox("绑定到EnemyDefinition后，BOSS按出没层数进入战斗。阶段替换被动、固定回合触发、牌序次数条件、锁定位置与卡牌出现时优先生效均由运行时执行。保存后重新开始游戏生效。",MessageType.Info);
   using(new EditorGUILayout.HorizontalScope()){
    if(GUILayout.Button("新建BOSS表")){var path=EditorUtility.SaveFilePanelInProject("新建BOSS表","BossTable","asset","保存到RainbowLi","Assets/RainbowLi/Data");if(path.StartsWith("Assets/RainbowLi/",StringComparison.Ordinal)){table=CreateInstance<BossContentTable>();AssetDatabase.CreateAsset(table,path);AssetDatabase.SaveAssets();selected=0;pending=null;}}
    GUI.enabled=table!=null;
    if(GUILayout.Button("导入Excel预览"))Guard(()=>{var path=EditorUtility.OpenFilePanel("BOSS表格","","xlsx");if(path.Length>0){pending=BossContentWorkbook.PreserveUnchanged(BossContentWorkbook.Read(path),table.rows);message="读取"+pending.Length+"个BOSS，请检查后应用。";}});
    if(GUILayout.Button("导出Excel"))Guard(()=>{var path=EditorUtility.SaveFilePanel("导出BOSS表格","","BOSS相关内容表格","xlsx");if(path.Length>0){BossContentWorkbook.Write(path,table.rows);message="已导出，含完整数值配置页。";}});
    if(GUILayout.Button("校验完整配置"))Guard(()=>{table.ValidateTable(true);message="全部BOSS配置校验通过。";});GUI.enabled=true;
   }
   if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.Info);if(table==null)return;
   if(pending!=null){foreach(var r in pending)EditorGUILayout.LabelField(r.content.title+" / 层数"+r.spawnLayers+" / 技能"+r.content.skillCount+" / 阶段"+r.phases.Length);using(new EditorGUILayout.HorizontalScope()){if(GUILayout.Button("应用导入（可撤销）")){Undo.RecordObject(table,"导入BOSS表");table.rows=pending.Select(r=>r.Copy()).ToArray();pending=null;EditorUtility.SetDirty(table);AssetDatabase.SaveAssets();}if(GUILayout.Button("取消"))pending=null;}return;}
   var so=new SerializedObject(table);so.Update();var rows=so.FindProperty("rows");
   using(new EditorGUILayout.HorizontalScope()){if(GUILayout.Button("新增BOSS")){Undo.RecordObject(table,"新增BOSS");int n=rows.arraySize+1;table.rows=(table.rows??Array.Empty<BossContentRow>()).Concat(new[]{new BossContentRow{content=new NormalEnemyRow{id="BOSS"+n,sheetName="BOSS"+n,title="新BOSS"+n,skillCount=0,sequenceCount=0},phases=new[]{new BossPhase{label="初始效果"}}}}).ToArray();EditorUtility.SetDirty(table);so.Update();selected=rows.arraySize-1;}if(rows.arraySize>0&&GUILayout.Button("删除当前BOSS")&&EditorUtility.DisplayDialog("删除BOSS","删除配置，可Undo恢复。","删除","取消")){rows.DeleteArrayElementAtIndex(Mathf.Clamp(selected,0,rows.arraySize-1));selected=0;so.ApplyModifiedProperties();AssetDatabase.SaveAssets();return;}}
   if(rows.arraySize==0)return;selected=EditorGUILayout.Popup("选择BOSS",Mathf.Clamp(selected,0,rows.arraySize-1),table.rows.Select(r=>r?.content.title??"空行").ToArray());scroll=EditorGUILayout.BeginScrollView(scroll);var boss=rows.GetArrayElementAtIndex(selected);var c=boss.FindPropertyRelative("content");
   Header("基础数值");foreach(var item in new[]{new[]{"id","BOSS ID"},new[]{"sheetName","Excel工作表"},new[]{"title","原表标题"},new[]{"enabled","启用"},new[]{"configured","已完成数值配置"},new[]{"health","血量"},new[]{"attack","基础攻击范围"},new[]{"actions","回合技能数量"},new[]{"skillCount","技能总数"},new[]{"sequenceCount","牌序效果数"},new[]{"initialEffects","初始效果原文"}})Field(c,item[0],item[1]);Field(boss,"spawnLayers","出没关卡层数");Field(boss,"thresholdText","原表血量阈值说明");
   Header("阶段与回合被动（进入新阶段替换旧阶段被动）");DrawArray(boss,"phases",p=>{Field(p,"index","阶段编号，0为初始");Field(p,"label","原表阶段栏目名");Field(p,"thresholdPercent","进入阈值（%）");Field(p,"description","阶段原文");DrawArray(p,"passives",b=>{Field(b,"timing","触发时机");Effects(b);});});
   Header("技能组");DrawArray(c,"skills",s=>{Field(s,"slot","技能行ID");Field(s,"name","技能名称");Field(s,"special","特殊技能");Field(s,"description","技能原文");Field(s,"numberRange","数字范围");Field(s,"weight","抽取权重，固定触发填0");Field(s,"conditionLabel","条件栏目名");Field(s,"condition","条件原文");Effects(s);Field(s,"notes","备注");});
   Header("技能触发、锁定和优先级（按技能行ID绑定）");DrawArray(boss,"skillRules",r=>{Field(r,"slot","技能行ID");Field(r,"trigger","触发条件");Field(r,"thresholdPercent","首次血量阈值（%）");Field(r,"requiredStage","所需/切换阶段");Field(r,"requiredSkill","前置技能名");Field(r,"sequenceCountThreshold","牌序次数严格超过此值");Field(r,"roundInterval","回合间隔");Field(r,"fixedTrigger","固定触发，不进入随机池");Field(r,"priorityOnAppearance","卡牌出现时优先生效");Field(r,"lockInPhase","强化阶段卡牌位置锁定");});
   Header("牌序效果");DrawArray(c,"sequences",s=>{Field(s,"slot","牌序行ID");Field(s,"name","牌序名称");Field(s,"description","效果原文");Field(s,"numbers","原表排列要求");Effects(s);});DrawArray(boss,"sequenceRules",r=>{Field(r,"slot","牌序行ID");Field(r,"mode","数字排列方式");Field(r,"requiredNumber","必须包含数字");Field(r,"fixedNumbers","固定数字排列");});
   Header("奖励");DrawArray(c,"rewards",r=>{Field(r,"slot","奖励行ID");Field(r,"name","原表类型");Field(r,"kind","奖励类型");Field(r,"amount","数量范围");Field(r,"probability","独立概率（0-100）");Field(r,"choiceCount","卡牌候选数");Field(r,"canSkip","允许跳过");Field(r,"notes","说明");});EditorGUILayout.EndScrollView();so.ApplyModifiedProperties();if(GUILayout.Button("保存BOSS表")){AssetDatabase.SaveAssetIfDirty(table);message="已保存。";}
  }
  static void Header(string s){EditorGUILayout.Space();EditorGUILayout.LabelField(s,EditorStyles.boldLabel);}
  static void Field(SerializedProperty p,string field,string label)=>EditorGUILayout.PropertyField(p.FindPropertyRelative(field),new GUIContent(label),true);
  static void DrawArray(SerializedProperty p,string name,Action<SerializedProperty> draw){var a=p.FindPropertyRelative(name);int n=EditorGUILayout.IntField("条目数",a.arraySize);if(n>=0&&n<=100&&n!=a.arraySize)a.arraySize=n;for(int i=0;i<a.arraySize;i++)using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))draw(a.GetArrayElementAtIndex(i));}
  static void Effects(SerializedProperty p){DrawArray(p,"effects",e=>{Field(e,"operation","效果类型");Field(e,"target","作用对象");Field(e,"amountSource","数值来源");Field(e,"amount","数量/层数/次数");Field(e,"multiplier","倍率");Field(e,"offset","加减值");Field(e,"numberMode","数字生成方式");Field(e,"numberMin","数字下限");Field(e,"numberMax","数字上限");Field(e,"notes","规则备注");});}
 }
 [CustomEditor(typeof(BossContentTable))] public sealed class BossContentTableInspector:UnityEditor.Editor {
  public override void OnInspectorGUI(){if(GUILayout.Button("打开BOSS策划配表"))BossContentEditorWindow.Open((BossContentTable)target);DrawDefaultInspector();}
 }
}
