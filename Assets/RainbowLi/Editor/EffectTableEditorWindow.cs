using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public abstract class EffectTableEditorWindow : EditorWindow {
  [SerializeField] EffectTableDefinition table;
  [SerializeField] UnityEngine.Object binding;
  [SerializeField] string search="",pool="";
  [SerializeField] bool enabledOnly,secondary;
  [SerializeField] int bindingX;
  Vector2 scroll;int selected=-1;string message="";
  protected abstract bool Sequence {get;}
  public EffectTableDefinition Table=>table;
  public void SetTable(EffectTableDefinition value){if(value!=null&&value.IsSequence!=Sequence)throw new InvalidOperationException("配表类型不匹配。");table=value;selected=-1;Repaint();}
  protected void OnEnable(){minSize=new Vector2(900,460);Undo.undoRedoPerformed+=Repaint;}
  protected void OnDisable(){Undo.undoRedoPerformed-=Repaint;}
  [UnityEditor.Callbacks.OnOpenAsset(1)] public static bool OpenAsset(int instance,int line){var asset=EditorUtility.InstanceIDToObject(instance);if(asset is EngravingEffectTable e){EngravingEditorWindow.Open();GetWindow<EngravingEditorWindow>().SetTable(e);return true;}if(asset is SequenceEffectTable s){SequenceEffectEditorWindow.Open();GetWindow<SequenceEffectEditorWindow>().SetTable(s);return true;}return false;}
  protected void OpenDefault(){SetTable(AssetDatabase.LoadAssetAtPath<EffectTableDefinition>(DefaultPath));}
  string DefaultPath=>"Assets/RainbowLi/Data/"+(Sequence?"DefaultSequenceEffects.asset":"DefaultEngravingEffects.asset");
  void Action(System.Action action){try{action();message="完成。";}catch(Exception e){message=e.Message;}Repaint();}
  protected void OnGUI(){
   EditorGUILayout.LabelField(Sequence?"牌序效果配表":"铭刻效果配表",EditorStyles.boldLabel);
   EditorGUILayout.HelpBox("前 "+(Sequence?6:5)+" 列与原 Excel 一致。配表可保存全部效果；“待核心程序”行保留配置，不能绑定到当前战斗。X倍率仅影响数值，不改变原文和范围。牌序铁壁可单独配置护盾增幅(%)，X仍为层数。铭刻当前没有铁壁、弱化或坚固效果。",MessageType.Info);
   EditorGUILayout.BeginHorizontal();var next=(EffectTableDefinition)EditorGUILayout.ObjectField("配表资产",table,Sequence?typeof(SequenceEffectTable):typeof(EngravingEffectTable),false);if(next!=table)SetTable(next);if(GUILayout.Button("新建配表",GUILayout.Width(90)))Action(CreateTable);EditorGUILayout.EndHorizontal();
   using(new EditorGUI.DisabledScope(table==null)){
    EditorGUILayout.BeginHorizontal();
    if(GUILayout.Button("保存配表"))Action(SaveTable);
    if(GUILayout.Button("导入 XLSX（替换）"))Action(()=>ImportFile(true));
    if(GUILayout.Button("导入 XLSX（按编号合并）"))Action(()=>ImportFile(false));
    if(GUILayout.Button("导出 XLSX"))Action(Export);
    if(GUILayout.Button("从 Excel 粘贴"))Action(()=>Import(CardTableWorkbook.ClipboardRows(EditorGUIUtility.systemCopyBuffer),false));
    EditorGUILayout.EndHorizontal();
    EditorGUILayout.BeginHorizontal();search=EditorGUILayout.TextField("搜索",search);enabledOnly=GUILayout.Toggle(enabledOnly,"只看启用",GUILayout.Width(90));if(Sequence)pool=EditorGUILayout.TextField("所属池筛选",pool);if(GUILayout.Button("新增行",GUILayout.Width(80)))Action(()=>Add(false));using(new EditorGUI.DisabledScope(selected<0||table==null||selected>=table.rows.Length)){if(GUILayout.Button("复制行",GUILayout.Width(80)))Action(()=>Add(true));if(GUILayout.Button("删除行",GUILayout.Width(80)))Action(Delete);}EditorGUILayout.EndHorizontal();
   }
   if(table==null)return;
   string error=Validation(table.rows);if(error!=null)EditorGUILayout.HelpBox(error,MessageType.Error);
   if(message!="")EditorGUILayout.HelpBox(message,MessageType.Info);
   DrawRows();DrawBinding();
  }
  float[] Widths=>Sequence?new float[]{130,135,360,160,240,190,50,70,185,65,80,100}:new float[]{130,135,360,160,190,50,70,185,65,80};
  void DrawRows(){var widths=Widths;scroll=EditorGUILayout.BeginScrollView(scroll);EditorGUILayout.BeginHorizontal();GUILayout.Label("选择",GUILayout.Width(42));var headers=EffectTableWorkbook.Headers(Sequence);for(int i=0;i<headers.Length;i++)GUILayout.Label(headers[i],EditorStyles.boldLabel,GUILayout.Width(widths[i]));GUILayout.Label("运行状态",GUILayout.Width(110));EditorGUILayout.EndHorizontal();
   Undo.RecordObject(table,"编辑效果配表");EditorGUI.BeginChangeCheck();
   for(int i=0;i<table.rows.Length;i++){
    var r=table.rows[i];if(r==null)continue;if(enabledOnly&&!r.enabled)continue;if(search!=""&&!string.Join(" ",r.id,r.name,r.description,r.notes,r.behavior).Contains(search))continue;if(Sequence&&pool!=""&&!table.GetPool(pool).Contains(r))continue;
    Color old=GUI.backgroundColor;if(i==selected)GUI.backgroundColor=new Color(.7f,.9f,1);EditorGUILayout.BeginHorizontal();if(GUILayout.Toggle(selected==i,(i+1).ToString(),"Button",GUILayout.Width(42)))selected=i;
    int c=0;r.id=EditorGUILayout.TextField(r.id,GUILayout.Width(widths[c++]));r.name=EditorGUILayout.TextField(r.name,GUILayout.Width(widths[c++]));r.description=EditorGUILayout.TextArea(r.description,GUILayout.Width(widths[c++]),GUILayout.Height(50));r.range=EditorGUILayout.TextField(r.range,GUILayout.Width(widths[c++]));if(Sequence)r.pools=EditorGUILayout.TextField(r.pools,GUILayout.Width(widths[c++]));r.notes=EditorGUILayout.TextArea(r.notes,GUILayout.Width(widths[c++]),GUILayout.Height(50));r.enabled=EditorGUILayout.Toggle(r.enabled,GUILayout.Width(widths[c++]));r.weight=EditorGUILayout.FloatField(r.weight,GUILayout.Width(widths[c++]));
    var names=Enum.GetNames(Sequence?typeof(SequenceBehavior):typeof(EngravingBehavior));int current=Array.IndexOf(names,r.behavior);int choice=EditorGUILayout.Popup(current,names,GUILayout.Width(widths[c++]));if(choice>=0)r.behavior=names[choice];r.multiplier=EditorGUILayout.IntField(r.multiplier,GUILayout.Width(widths[c++]));r.fixedValue=EditorGUILayout.IntField(r.fixedValue,GUILayout.Width(widths[c++]));if(Sequence){using(new EditorGUI.DisabledScope(r.behavior!="IronWall"))r.shieldBonusPercent=EditorGUILayout.IntField(r.shieldBonusPercent,GUILayout.Width(widths[c++]));}GUILayout.Label(table.IsRuntimeSupported(r)?"可接入现有结算":"待核心程序",GUILayout.Width(110));EditorGUILayout.EndHorizontal();GUI.backgroundColor=old;
   }
   if(EditorGUI.EndChangeCheck())EditorUtility.SetDirty(table);EditorGUILayout.EndScrollView();
  }
  void DrawBinding(){EditorGUILayout.LabelField("将选中行绑定到效果配置",EditorStyles.boldLabel);binding=EditorGUILayout.ObjectField(Sequence?"牌序配置":"铭刻配置",binding,Sequence?typeof(SequenceDefinition):typeof(EngravingDefinition),false);EditorGUILayout.BeginHorizontal();bindingX=EditorGUILayout.IntField("X（0 使用范围下限）",bindingX);if(Sequence)secondary=GUILayout.Toggle(secondary,"绑定为第二效果",GUILayout.Width(150));using(new EditorGUI.DisabledScope(selected<0||selected>=table.rows.Length||!table.IsRuntimeSupported(table.rows[selected])||!table.rows[selected].enabled)){if(GUILayout.Button("绑定到配置",GUILayout.Width(110)))Action(Bind);if(GUILayout.Button("新建效果配置",GUILayout.Width(120)))Action(CreateDefinition);}EditorGUILayout.EndHorizontal();}
  string Validation(EffectTableRow[] rows){var probe=CreateProbe();try{probe.rows=rows;probe.ValidateTable();return null;}catch(Exception e){return e.Message;}finally{DestroyImmediate(probe);}}
  EffectTableDefinition CreateProbe()=>Sequence?(EffectTableDefinition)CreateInstance<SequenceEffectTable>():CreateInstance<EngravingEffectTable>();
  public void SaveTable(){string error=Validation(table.rows);if(error!=null)throw new InvalidOperationException(error);EditorUtility.SetDirty(table);AssetDatabase.SaveAssetIfDirty(table);}
  public void Import(string[][] cells,bool replace){var defaults=AssetDatabase.LoadAssetAtPath<EffectTableDefinition>(DefaultPath);var known=table.rows.Concat(defaults==null?Array.Empty<EffectTableRow>():defaults.rows);var imported=EffectTableWorkbook.Parse(cells,Sequence,known);if(imported.Select(r=>r.id).Distinct().Count()!=imported.Length)throw new InvalidOperationException("导入包含重复效果编号。");var result=replace?imported:table.rows.Select(r=>imported.FirstOrDefault(n=>n.id==r.id)??r).Concat(imported.Where(n=>!table.rows.Any(r=>r.id==n.id))).ToArray();string error=Validation(result);if(error!=null)throw new InvalidOperationException(error);Undo.RecordObject(table,"导入效果配表");table.rows=result;EditorUtility.SetDirty(table);selected=-1;}
  void ImportFile(bool replace){string path=EditorUtility.OpenFilePanel("导入效果表","","xlsx");if(path!="")Import(CardTableWorkbook.Read(path),replace);}
  void Add(bool copy){Undo.RecordObject(table,"新增效果行");var r=copy?table.rows[selected].Copy():new EffectTableRow {name="新效果X",description="请填写具体效果",range="1",pools=Sequence?"默认池":"",behavior=Sequence?nameof(SequenceBehavior.Damage):nameof(EngravingBehavior.FollowUpDamage)};string stem=copy?r.id+"_":"新效果_";int suffix=1;while(table.rows.Any(n=>n.id==stem+suffix))suffix++;r.id=stem+suffix;table.rows=table.rows.Concat(new[]{r}).ToArray();selected=table.rows.Length-1;EditorUtility.SetDirty(table);}
  void Delete(){Undo.RecordObject(table,"删除效果行");table.rows=table.rows.Where((r,i)=>i!=selected).ToArray();selected=-1;EditorUtility.SetDirty(table);}
  void CreateTable(){string path=EditorUtility.SaveFilePanelInProject("新建效果表",Sequence?"SequenceEffects":"EngravingEffects","asset","保存到 RainbowLi 内","Assets/RainbowLi/Data");if(path=="")return;CheckPath(path);var a=CreateProbe();var source=AssetDatabase.LoadAssetAtPath<EffectTableDefinition>(DefaultPath);a.rows=source==null?Array.Empty<EffectTableRow>():source.rows.Select(r=>r.Copy()).ToArray();AssetDatabase.CreateAsset(a,path);Undo.RegisterCreatedObjectUndo(a,"新建效果表");SetTable(a);}
  void Export(){SaveTable();string path=EditorUtility.SaveFilePanel("导出效果表",Path.GetFullPath("Assets/RainbowLi"),Sequence?"牌序效果":"铭刻效果","xlsx");if(path=="")return;string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{EffectTableWorkbook.Write(temp,table);File.Copy(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
  public void BindSelected(UnityEngine.Object target,int rowIndex,int x,bool second=false){SaveTable();if(target==null)throw new InvalidOperationException("请先选择配置资产。");var r=table.rows[rowIndex];table.RequireSupported(r);r.TryRange(out int min,out _);r.ResolveX(x==0?min:x);if(Sequence&&!(target is SequenceDefinition)||!Sequence&&!(target is EngravingDefinition))throw new InvalidOperationException("配置资产类型不匹配。");if(Sequence){var current=(SequenceDefinition)target;string other=second?current.primaryEffectId:current.secondEffectEnabled?current.secondaryEffectId:null;if(current.effectTable!=null&&!string.IsNullOrWhiteSpace(other)){var existing=table.Find(other);table.RequireSupported(existing);existing.TryRange(out int otherMin,out _);int otherX=second?current.primaryX:current.secondaryX;existing.ResolveX(otherX==0?otherMin:otherX);}}Undo.RecordObject(target,"绑定效果配表");if(Sequence){var d=(SequenceDefinition)target;d.effectTable=(SequenceEffectTable)table;if(second){d.secondEffectEnabled=true;d.secondaryEffectId=r.id;d.secondaryX=x;}else{d.primaryEffectId=r.id;d.primaryX=x;}}else{var d=(EngravingDefinition)target;d.effectTable=(EngravingEffectTable)table;d.effectId=r.id;d.effectX=x;}EditorUtility.SetDirty(target);AssetDatabase.SaveAssetIfDirty(target);}
  void Bind(){BindSelected(binding,selected,bindingX,secondary);}
  void CreateDefinition(){SaveTable();string path=EditorUtility.SaveFilePanelInProject("新建效果配置",table.rows[selected].id,"asset","保存到 RainbowLi 内","Assets/RainbowLi/Data");if(path=="")return;CheckPath(path);var r=table.rows[selected];table.RequireSupported(r);r.TryRange(out int min,out _);r.ResolveX(bindingX==0?min:bindingX);var d=Sequence?(ScriptableObject)CreateInstance<SequenceDefinition>():CreateInstance<EngravingDefinition>();if(d is SequenceDefinition s)s.displayName=r.name;else ((EngravingDefinition)d).description=r.description;AssetDatabase.CreateAsset(d,path);Undo.RegisterCreatedObjectUndo(d,"新建效果配置");binding=d;Bind();}
  static void CheckPath(string path){if(!path.StartsWith("Assets/RainbowLi/",StringComparison.Ordinal)||path.Contains(".."))throw new InvalidOperationException("配置须保存在 Assets/RainbowLi 内。");}
 }
}
