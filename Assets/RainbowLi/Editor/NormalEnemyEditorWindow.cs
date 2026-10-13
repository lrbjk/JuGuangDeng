using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public sealed class NormalEnemyEditorWindow : EditorWindow {
  NormalEnemyTable table;NormalEnemyRow[] pending;Vector2 scroll;int selected;string message;
  [MenuItem("RainbowLi/配表/普通敌人内容编辑器")]
  public static void Open(){GetWindow<NormalEnemyEditorWindow>("普通敌人配表");}
  public static void Open(NormalEnemyTable asset){var w=GetWindow<NormalEnemyEditorWindow>("普通敌人配表");w.table=asset;w.pending=null;w.selected=0;}
  void OnGUI(){
   var next=(NormalEnemyTable)EditorGUILayout.ObjectField("普通敌人内容表",table,typeof(NormalEnemyTable),false);
   if(next!=table){table=next;pending=null;selected=0;}
   EditorGUILayout.HelpBox("按Excel工作表配置一种敌人，技能权重与奖励概率分别独立填写。绑定到EnemyDefinition后，启用且已配置的敌人会进入战斗随机池；复合技能、前置触发、行动范围、牌序及奖励均由运行时执行。保存后重新开始游戏生效。",MessageType.Info);
   using(new EditorGUILayout.HorizontalScope()){
    if(GUILayout.Button("新建内容表"))Create();
    GUI.enabled=table!=null;
    if(GUILayout.Button("导入Excel预览"))Guard(()=>{string path=EditorUtility.OpenFilePanel("选择普通敌人原格式Excel","","xlsx");if(path.Length>0){pending=NormalEnemyWorkbook.PreserveUnchangedConfigurations(NormalEnemyWorkbook.Read(path),table.rows);message="已读取"+pending.Length+"个敌人；检查预览后再应用。";}});
    if(GUILayout.Button("导出Excel"))Guard(()=>{string path=EditorUtility.SaveFilePanel("导出普通敌人Excel","","普通敌人相关内容表格","xlsx");if(path.Length>0){NormalEnemyWorkbook.Write(path,table.rows);message="Excel导出完成。";}});
    if(GUILayout.Button("校验"))Guard(()=>{table.ValidateTable();table.ValidateConfiguredTable();message="完整数值配表校验通过。运行时接入状态见上方说明。";});
    GUI.enabled=true;
   }
   if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.Info);
   if(table==null)return;
   if(pending!=null){
    EditorGUILayout.LabelField("导入预览："+pending.Length+"个敌人 / 将替换当前"+(table.rows?.Length??0)+"个敌人",EditorStyles.boldLabel);
    foreach(var r in pending)EditorGUILayout.LabelField(r.title+"  血量"+r.health+" / 攻击"+r.attack+" / 技能"+r.skillCount+" / 奖励"+r.rewards.Length);
    using(new EditorGUILayout.HorizontalScope()){
     if(GUILayout.Button("应用导入（可撤销）")){Undo.RecordObject(table,"导入普通敌人表");table.rows=pending.Select(r=>r.Copy()).ToArray();pending=null;selected=0;EditorUtility.SetDirty(table);AssetDatabase.SaveAssets();message="导入已保存。";}
     if(GUILayout.Button("取消导入"))pending=null;
    }return;
   }
   var so=new SerializedObject(table);so.Update();var rows=so.FindProperty("rows");
   using(new EditorGUILayout.HorizontalScope()){
    if(GUILayout.Button("新增敌人")){Undo.RecordObject(table,"新增普通敌人");var n=(table.rows?.Length??0)+1;table.rows=(table.rows??Array.Empty<NormalEnemyRow>()).Concat(new[]{new NormalEnemyRow{id="普通敌人"+n,sheetName="普通敌人"+n,title="普通敌人"+n,skillCount=0,sequenceCount=0}}).ToArray();EditorUtility.SetDirty(table);selected=table.rows.Length-1;so.Update();}
    GUI.enabled=rows.arraySize>0;
    if(GUILayout.Button("删除当前敌人")&&EditorUtility.DisplayDialog("删除配置","只删除当前敌人配置。可以通过Undo恢复。","删除","取消")){rows.DeleteArrayElementAtIndex(selected);selected=0;so.ApplyModifiedProperties();AssetDatabase.SaveAssets();return;}
    GUI.enabled=true;
   }
   if(rows.arraySize==0)return;selected=Mathf.Clamp(selected,0,rows.arraySize-1);
   selected=EditorGUILayout.Popup("选择敌人",selected,table.rows.Select(r=>r?.title??"空行").ToArray());
   scroll=EditorGUILayout.BeginScrollView(scroll);var row=rows.GetArrayElementAtIndex(selected);
   Field(row,"id","敌人ID（程序引用）");Field(row,"sheetName","Excel工作表名");Field(row,"title","敌人标题");Field(row,"enabled","启用（Unity内配置）");
   Field(row,"configured","已完成数值配置");
   Header("基础数值");Field(row,"health","血量");Field(row,"actions","回合技能使用数");Field(row,"attack","基础攻击值");Field(row,"healthThresholdPercent","血量阈值（%）");Field(row,"skillCount","技能总数");Field(row,"sequenceCount","牌序效果数");Field(row,"initialEffects","初始效果");
   Header("基础技能与特殊技能");
   DrawArray(row,"skills",p=>{Field(p,"slot","表格行名称");Field(p,"special","特殊技能");Field(p,"name","技能名称");Field(p,"description","技能效果（完整原文）");Field(p,"numberRange","数字范围");Field(p,"weight","抽取权重");Field(p,"conditionLabel","附加字段名（触发条件/变化范围）");Field(p,"condition","触发条件/变化范围");Field(p,"trigger","数值触发条件");Field(p,"requiredSkill","前置技能名称");Effects(p);Field(p,"notes","备注");});
   Header("牌序效果");DrawArray(row,"sequences",p=>{Field(p,"slot","表格行名称");Field(p,"name","效果名称");Field(p,"description","具体效果");Field(p,"numbers","数字排列");Effects(p);});
   Header("奖励内容");DrawArray(row,"rewards",p=>{Field(p,"slot","表格行名称");Field(p,"name","奖励类型");Field(p,"kind","数值奖励类型");Field(p,"amount","数量/数量范围");Field(p,"probability","出现概率（0–100）");Field(p,"choiceCount","卡牌候选数量");Field(p,"canSkip","允许跳过");Field(p,"notes","奖励说明");});
   EditorGUILayout.EndScrollView();if(so.ApplyModifiedProperties())EditorUtility.SetDirty(table);
   if(GUILayout.Button("保存内容表")){AssetDatabase.SaveAssets();message="配置已保存。";}
  }
  static void Header(string s){EditorGUILayout.Space();EditorGUILayout.LabelField(s,EditorStyles.boldLabel);}
  static void Effects(SerializedProperty p){EditorGUILayout.LabelField("按顺序执行的数值效果",EditorStyles.boldLabel);DrawArray(p,"effects",e=>{Field(e,"operation","效果类型");Field(e,"target","作用对象");Field(e,"amountSource","数值来源");Field(e,"amount","固定数量/层数/次数");Field(e,"multiplier","倍率");Field(e,"offset","加减值");Field(e,"numberMode","填入数字方式");Field(e,"numberMin","生成数字下限");Field(e,"numberMax","生成数字上限");Field(e,"notes","规则备注");});}
  static void Field(SerializedProperty parent,string name,string label)=>EditorGUILayout.PropertyField(parent.FindPropertyRelative(name),new GUIContent(label),true);
  static void DrawArray(SerializedProperty row,string name,Action<SerializedProperty> draw){var array=row.FindPropertyRelative(name);int size=EditorGUILayout.IntField("条目数",array.arraySize);if(size>=0&&size<=100&&size!=array.arraySize)array.arraySize=size;for(int i=0;i<array.arraySize;i++)using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox)){EditorGUILayout.LabelField("条目 "+(i+1),EditorStyles.boldLabel);draw(array.GetArrayElementAtIndex(i));}}
  void Guard(Action a){try{a();}catch(Exception e){message=e.Message;}}
  void Create(){string p=EditorUtility.SaveFilePanelInProject("新建普通敌人表","NormalEnemies","asset","选择RainbowLi下的位置","Assets/RainbowLi/Data");if(p.Length==0)return;if(!p.StartsWith("Assets/RainbowLi/",StringComparison.Ordinal)){message="请保存到Assets/RainbowLi目录。";return;}table=CreateInstance<NormalEnemyTable>();AssetDatabase.CreateAsset(table,p);AssetDatabase.SaveAssets();selected=0;pending=null;}
 }
 [CustomEditor(typeof(NormalEnemyTable))] public sealed class NormalEnemyTableInspector : UnityEditor.Editor {
  public override void OnInspectorGUI(){if(GUILayout.Button("打开普通敌人策划配表"))NormalEnemyEditorWindow.Open((NormalEnemyTable)target);EditorGUILayout.HelpBox("策划使用专用编辑器修改或导入Excel；完整运行时接口为GetSnapshot/GetEnabledSnapshots。",MessageType.Info);DrawDefaultInspector();}
 }
}
