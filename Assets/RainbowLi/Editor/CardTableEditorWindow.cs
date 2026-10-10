using System;
using System.IO;
using System.Linq;
using RainbowLi.UnityGame;
using UnityEditor;
using UnityEngine;
namespace RainbowLi.EditorTools {
 public abstract class CardTableEditorWindow : EditorWindow {
  [SerializeField] CardTableDefinition table;
  [SerializeField] UnityEngine.Object actor;
  [SerializeField] string search="";
  [SerializeField] bool enabledOnly;
  Vector2 scroll;int selected=-1;string message="";
  protected abstract bool Enemy {get;}
  public CardTableDefinition Table=>table;
  public void SetTable(CardTableDefinition value){table=value;selected=-1;Repaint();}
  [UnityEditor.Callbacks.OnOpenAsset(1)]
  public static bool OpenAsset(int instanceId,int line){
   var asset=EditorUtility.InstanceIDToObject(instanceId);
   if(asset is PlayerCardTable player){PlayerCardEditorWindow.Open();GetWindow<PlayerCardEditorWindow>().SetTable(player);return true;}
   if(asset is EnemyCardTable enemy){EnemyCardEditorWindow.Open();GetWindow<EnemyCardEditorWindow>().SetTable(enemy);return true;}
   return false;
  }
  protected void OnEnable(){minSize=new Vector2(850,430);Undo.undoRedoPerformed+=Repaint;}
  protected void OnDisable(){Undo.undoRedoPerformed-=Repaint;}
  static readonly string[] Life=CardTableWorkbook.Lifetimes,Polarity=CardTableWorkbook.Polarities;
  static readonly string[] Effects=Enum.GetValues(typeof(CardEffect)).Cast<CardEffect>().Select(CombatStatuses.Name).ToArray();
  static readonly float[] Widths={115,135,115,110,360,155,180,45,48,68,60,68,112,55,75,125};
  void Action(System.Action action){try{action();message="完成。";}catch(Exception e){message=e.Message;}Repaint();}
  protected void OnGUI(){
   EditorGUILayout.LabelField(Enemy?"敌人卡牌配表":"玩家卡牌配表",EditorStyles.boldLabel);
   EditorGUILayout.HelpBox("前七列与《卡牌效果.xlsx》一致；第五列保存效果描述。启用决定是否进入效果池。"+(Enemy?"敌人使用数字列；特殊技能按条件首次额外触发。":"玩家随机起始牌保留三组 1–6 的数字规则。")+" 正负面列用于分类；作用对象由程序效果标识决定：伤害/减益给对手，防御/恢复/增益给自身。修改后保存，重新开始游戏生效。",MessageType.Info);
   EditorGUILayout.BeginHorizontal();var type=Enemy?typeof(EnemyCardTable):typeof(PlayerCardTable);var next=(CardTableDefinition)EditorGUILayout.ObjectField("配表资产",table,type,false);if(next!=table)SetTable(next);
   if(GUILayout.Button("新建配表",GUILayout.Width(85)))Action(CreateTable);EditorGUILayout.EndHorizontal();
   actor=EditorGUILayout.ObjectField(Enemy?"敌人配置":"玩家配置",actor,Enemy?typeof(EnemyDefinition):typeof(PlayerDefinition),false);
   EditorGUILayout.BeginHorizontal();using(new EditorGUI.DisabledScope(table==null)){
    if(GUILayout.Button("保存配表"))Action(SaveTable);
    if(GUILayout.Button("绑定到角色"))Action(Bind);
    if(GUILayout.Button("导入 XLSX"))Action(()=>{string path=EditorUtility.OpenFilePanel("导入卡牌表","","xlsx");if(path!="")Import(CardTableWorkbook.Read(path),true);});
    if(GUILayout.Button("导出 XLSX"))Action(Export);
    if(GUILayout.Button("粘贴 Excel 行"))Action(()=>Import(CardTableWorkbook.ClipboardRows(EditorGUIUtility.systemCopyBuffer),false));
   }EditorGUILayout.EndHorizontal();
   if(table==null){EditorGUILayout.HelpBox("选择已有配表，或新建一份从 26 种效果初始化的独立配表。",MessageType.Info);return;}
   EditorGUILayout.BeginHorizontal();search=EditorGUILayout.TextField("搜索编号 / 名称 / 描述",search);enabledOnly=GUILayout.Toggle(enabledOnly,"仅启用",GUILayout.Width(80));
   if(GUILayout.Button("增加行",GUILayout.Width(75)))Action(()=>Add(false));using(new EditorGUI.DisabledScope(selected<0||selected>=table.rows.Length)){if(GUILayout.Button("复制行",GUILayout.Width(75)))Action(()=>Add(true));if(GUILayout.Button("删除行",GUILayout.Width(75)))Action(Delete);}
   EditorGUILayout.EndHorizontal();
   if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.None);
   string error=Validation();if(error!=null)EditorGUILayout.HelpBox(error,MessageType.Error);
   EditorGUILayout.LabelField("共 "+table.rows.Length+" 行，启用 "+table.rows.Count(r=>r!=null&&r.enabled)+" 行"+(EditorUtility.IsDirty(table)?" · 有未保存修改":""));
   scroll=EditorGUILayout.BeginScrollView(scroll);
   EditorGUILayout.BeginHorizontal();GUILayout.Label("行",EditorStyles.boldLabel,GUILayout.Width(35));for(int c=0;c<Widths.Length;c++)if(Enemy||c<11||c==15)GUILayout.Label(c==2?"效果类型（持续）":c==3?"效果类型（性质）":CardTableWorkbook.Headers[c],EditorStyles.boldLabel,GUILayout.Width(Widths[c]));EditorGUILayout.EndHorizontal();
   Undo.RecordObject(table,"编辑卡牌配表");EditorGUI.BeginChangeCheck();
   for(int i=0;i<table.rows.Length;i++){
    var r=table.rows[i];if(r==null)continue;if(enabledOnly&&!r.enabled||!string.IsNullOrEmpty(search)&&!((r.id??"")+" "+r.name+" "+r.description).Contains(search))continue;
    var color=GUI.backgroundColor;if(i==selected)GUI.backgroundColor=new Color(.5f,.75f,1);EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
    if(GUILayout.Toggle(selected==i,(i+1).ToString(),"Button",GUILayout.Width(35),GUILayout.Height(48)))selected=i;
    r.id=EditorGUILayout.TextField(r.id,GUILayout.Width(Widths[0]));r.name=EditorGUILayout.TextField(r.name,GUILayout.Width(Widths[1]));
    r.lifetime=(EffectLifetime)EditorGUILayout.Popup((int)r.lifetime,Life,GUILayout.Width(Widths[2]));r.polarity=(EffectPolarity)EditorGUILayout.Popup((int)r.polarity,Polarity,GUILayout.Width(Widths[3]));
    r.description=EditorGUILayout.TextArea(r.description,GUILayout.Width(Widths[4]),GUILayout.Height(48));
    string range=EditorGUILayout.TextField(r.range,GUILayout.Width(Widths[5]));if(range!=r.range){r.range=range;try{CardTableWorkbook.SetRange(r,range);}catch(FormatException){}}
    r.notes=EditorGUILayout.TextArea(r.notes,GUILayout.Width(Widths[6]),GUILayout.Height(48));r.enabled=EditorGUILayout.Toggle(r.enabled,GUILayout.Width(Widths[7]));r.number=EditorGUILayout.IntField(r.number,GUILayout.Width(Widths[8]));r.weight=EditorGUILayout.FloatField(r.weight,GUILayout.Width(Widths[9]));r.starting=EditorGUILayout.Toggle(r.starting,GUILayout.Width(Widths[10]));
    if(Enemy){r.special=EditorGUILayout.Toggle(r.special,GUILayout.Width(Widths[11]));r.condition=(ActivationCondition)EditorGUILayout.Popup((int)r.condition,new[]{"回合","生命百分比"},GUILayout.Width(Widths[12]));r.threshold=EditorGUILayout.IntField(r.threshold,GUILayout.Width(Widths[13]));r.specialWeightRatio=EditorGUILayout.FloatField(r.specialWeightRatio,GUILayout.Width(Widths[14]));}
    r.effect=(CardEffect)EditorGUILayout.Popup((int)r.effect,Effects,GUILayout.Width(Widths[15]));EditorGUILayout.EndHorizontal();GUI.backgroundColor=color;
   }
   if(EditorGUI.EndChangeCheck())EditorUtility.SetDirty(table);EditorGUILayout.EndScrollView();
  }
  string Validation(){var probe=Enemy?(CardTableDefinition)CreateInstance<EnemyCardTable>():CreateInstance<PlayerCardTable>();try{probe.rows=table.rows.Select(row=>{var copy=row.Copy();CardTableWorkbook.SetRange(copy,row.range);return copy;}).ToArray();probe.ValidateTable(Enemy);return null;}catch(Exception e){return e.Message;}finally{DestroyImmediate(probe);}}
  public void SaveTable(){var error=Validation();if(error!=null)throw new InvalidOperationException(error);Undo.RecordObject(table,"保存卡牌配表");foreach(var row in table.rows)CardTableWorkbook.SetRange(row,row.range);EditorUtility.SetDirty(table);AssetDatabase.SaveAssetIfDirty(table);}
  public void Import(string[][] cells,bool replace){
   var catalog=AssetDatabase.LoadAssetAtPath<CardEffectCatalog>("Assets/RainbowLi/Data/DefaultCardEffects.asset");var known=table.rows.Concat(catalog.effects.Where(e=>e!=null).Select(CardTableRow.From));var imported=CardTableWorkbook.Parse(cells,known);
   var result=replace?imported:table.rows.Where(r=>!imported.Any(n=>n.id==r.id)).Concat(imported).ToArray();var probe=Enemy?(CardTableDefinition)ScriptableObject.CreateInstance<EnemyCardTable>():ScriptableObject.CreateInstance<PlayerCardTable>();
   try{probe.rows=result;probe.ValidateTable(Enemy);}finally{DestroyImmediate(probe);}
   Undo.RecordObject(table,"导入卡牌配表");table.rows=result;EditorUtility.SetDirty(table);selected=-1;
  }
  void Add(bool copy){Undo.RecordObject(table,"新增卡牌行");var r=copy?table.rows[selected].Copy():CardTableRow.From(AssetDatabase.LoadAssetAtPath<CardEffectDefinition>("Assets/RainbowLi/Data/CardEffects/CardEffect_01.asset"));string stem=r.id+"_",id=stem+1;for(int i=2;table.rows.Any(row=>row.id==id);i++)id=stem+i;r.id=id;table.rows=table.rows.Concat(new[]{r}).ToArray();selected=table.rows.Length-1;EditorUtility.SetDirty(table);}
  void Delete(){Undo.RecordObject(table,"删除卡牌行");table.rows=table.rows.Where((r,i)=>i!=selected).ToArray();selected=-1;EditorUtility.SetDirty(table);}
  void Bind(){SaveTable();if(actor==null)throw new InvalidOperationException("请先选择角色配置资产。");Undo.RecordObject(actor,"绑定卡牌表");if(Enemy)((EnemyDefinition)actor).cardTable=(EnemyCardTable)table;else ((PlayerDefinition)actor).cardTable=(PlayerCardTable)table;EditorUtility.SetDirty(actor);AssetDatabase.SaveAssetIfDirty(actor);}
  void CreateTable(){var path=EditorUtility.SaveFilePanelInProject("新建卡牌配表",Enemy?"EnemyCards":"PlayerCards","asset","选择 RainbowLi 内的保存位置","Assets/RainbowLi/Data");if(path=="")return;if(!path.StartsWith("Assets/RainbowLi/",StringComparison.Ordinal))throw new InvalidOperationException("配表必须保存在 Assets/RainbowLi 内。");var asset=Enemy?(CardTableDefinition)CreateInstance<EnemyCardTable>():CreateInstance<PlayerCardTable>();var catalog=AssetDatabase.LoadAssetAtPath<CardEffectCatalog>("Assets/RainbowLi/Data/DefaultCardEffects.asset");asset.rows=catalog.effects.Select(CardTableRow.From).ToArray();AssetDatabase.CreateAsset(asset,path);Undo.RegisterCreatedObjectUndo(asset,"新建配表");SetTable(asset);}
  void Export(){SaveTable();string path=EditorUtility.SaveFilePanel("导出卡牌表",Path.GetFullPath("Assets/RainbowLi"),Enemy?"敌人卡牌表":"玩家卡牌表","xlsx");if(path=="")return;string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{CardTableWorkbook.Write(temp,table.rows);File.Copy(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
  protected void OpenDefault(){SetTable(AssetDatabase.LoadAssetAtPath<CardTableDefinition>(Enemy?"Assets/RainbowLi/Data/DefaultEnemyCards.asset":"Assets/RainbowLi/Data/DefaultPlayerCards.asset"));actor=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Enemy?"Assets/RainbowLi/Data/DefaultEnemy.asset":"Assets/RainbowLi/Data/DefaultPlayer.asset");}
 }
}
