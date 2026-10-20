using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using RainbowLi.UnityGame;
using UnityEditor;
using UnityEngine;
namespace RainbowLi.EditorTools {
 public abstract class CardTableEditorWindow : EditorWindow {
  [SerializeField] CardTableDefinition table;
  [SerializeField] UnityEngine.Object actor;
  [SerializeField] string search="";
  [SerializeField] bool enabledOnly;
  Vector2 scroll,compoundScroll;int selected=-1;string message="";
  readonly HashSet<int> checkedRows=new HashSet<int>();
  bool replaceImport,showBulk,showCompound;
  bool setEnabled,setWeight,setNumber,setRange,setStarting,setEffect;
  bool bulkEnabled=true,bulkStarting=true;float bulkWeight=10;int bulkNumber=1,bulkEffect;string bulkRange="1";
  protected abstract bool Enemy {get;}
  public CardTableDefinition Table=>table;
  public void SetTable(CardTableDefinition value){table=value;selected=-1;checkedRows.Clear();Repaint();}
  [UnityEditor.Callbacks.OnOpenAsset(1)]
  public static bool OpenAsset(int instanceId,int line){
   var asset=EditorUtility.InstanceIDToObject(instanceId);
   if(asset is PlayerCardTable player){PlayerCardEditorWindow.Open();GetWindow<PlayerCardEditorWindow>().SetTable(player);return true;}
   if(asset is EnemyCardTable enemy){EnemyCardEditorWindow.Open();GetWindow<EnemyCardEditorWindow>().SetTable(enemy);return true;}
   return false;
  }
  protected void OnEnable(){minSize=new Vector2(850,430);Undo.undoRedoPerformed+=UndoRefresh;}
  protected void OnDisable(){Undo.undoRedoPerformed-=UndoRefresh;}
  void UndoRefresh(){checkedRows.Clear();selected=-1;Repaint();}
  static readonly string[] Life=CardTableWorkbook.Lifetimes,Polarity=CardTableWorkbook.Polarities;
  static readonly string[] Effects=Enum.GetValues(typeof(CardEffect)).Cast<CardEffect>().Select(CombatStatuses.Name).ToArray();
  static readonly float[] Widths={115,135,115,110,360,155,180,45,48,68,60,68,112,55,75,125};
  void Action(System.Action action){try{action();message="完成。";}catch(Exception e){message=e.Message;}Repaint();}
  protected void OnGUI(){
   EditorGUILayout.LabelField(Enemy?"敌人卡牌配表":"玩家卡牌配表",EditorStyles.boldLabel);
   EditorGUILayout.HelpBox("支持九列卡效设计表与编辑器导出表；第五列保存效果描述。启用决定是否进入效果池。"+(Enemy?"敌人使用数字列；特殊技能按条件首次额外触发。":"玩家随机起始牌保留三组 1–6 的数字规则；配置池权重后先抽池，再按池内权重抽效果。")+" 正负面列用于分类；作用对象由程序效果标识决定：伤害/减益给对手，防御/恢复/增益给自身。修改后保存，重新开始游戏生效。",MessageType.Info);
   EditorGUILayout.BeginHorizontal();var type=Enemy?typeof(EnemyCardTable):typeof(PlayerCardTable);var next=(CardTableDefinition)EditorGUILayout.ObjectField("配表资产",table,type,false);if(next!=table)SetTable(next);
   if(GUILayout.Button("新建配表",GUILayout.Width(85)))Action(CreateTable);EditorGUILayout.EndHorizontal();
   actor=EditorGUILayout.ObjectField(Enemy?"敌人配置":"玩家配置",actor,Enemy?typeof(EnemyDefinition):typeof(PlayerDefinition),false);
   EditorGUILayout.BeginHorizontal();using(new EditorGUI.DisabledScope(table==null)){
    if(GUILayout.Button("保存配表"))Action(SaveTable);
    if(GUILayout.Button("绑定到角色"))Action(Bind);
    if(GUILayout.Button("导入 XLSX（预览）"))Action(()=>{string path=EditorUtility.OpenFilePanel("导入卡牌表","","xlsx");if(path!="")CardTableImportPreviewWindow.Open(PreviewFile(path,replaceImport));});
    if(GUILayout.Button("导出 XLSX"))Action(Export);
    if(GUILayout.Button("粘贴 Excel 行（预览）"))Action(()=>CardTableImportPreviewWindow.Open(Preview(CardTableWorkbook.ClipboardRows(EditorGUIUtility.systemCopyBuffer),false)));
   }EditorGUILayout.EndHorizontal();
   replaceImport=EditorGUILayout.ToggleLeft("XLSX 使用整表替换（缺失行列入删除预览；默认按编号合并）",replaceImport);
   if(table==null){EditorGUILayout.HelpBox("选择已有配表，或新建一份从 26 种效果初始化的独立配表。",MessageType.Info);return;}
   if(table.rows==null){EditorGUILayout.HelpBox("行数组为空，请在 Inspector 初始化配表。",MessageType.Error);return;}
   EditorGUILayout.BeginHorizontal();search=EditorGUILayout.TextField("搜索编号 / 名称 / 描述",search);enabledOnly=GUILayout.Toggle(enabledOnly,"仅启用",GUILayout.Width(80));
   if(GUILayout.Button("增加行",GUILayout.Width(75)))Action(()=>Add(false));using(new EditorGUI.DisabledScope(selected<0||selected>=table.rows.Length)){if(GUILayout.Button("复制行",GUILayout.Width(75)))Action(()=>Add(true));if(GUILayout.Button("删除行",GUILayout.Width(75)))Action(Delete);}
   EditorGUILayout.EndHorizontal();
   if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.None);
   var issues=CardTableValidation.Check(table.rows,Enemy);if(issues.Length>0)EditorGUILayout.HelpBox("发现 "+issues.Length+" 项错误，请查看各行下方提示；错误未修复前不能保存或批量应用。",MessageType.Error);
   foreach(var issue in issues.Where(e=>e.Row==0))EditorGUILayout.HelpBox(issue.ToString(),MessageType.Error);
   if(table.rows.Any(r=>r!=null&&r.enabled&&r.HasCompoundEffects))EditorGUILayout.HelpBox("已启用复合效果：每行生成一张完整卡牌，追加效果按列表顺序执行；随机卡池、起始牌组和敌人技能均已接入。",MessageType.Warning);
   EditorGUILayout.LabelField("共 "+table.rows.Length+" 行，启用 "+table.rows.Count(r=>r!=null&&r.enabled)+" 行"+(EditorUtility.IsDirty(table)?" · 有未保存修改":""));
   DrawBulk();
   EditorGUILayout.HelpBox("具体数值：主效果的固定 X，填 1–10000 覆盖取值范围和卡牌数字；填 0 沿用原配置。不会修改追加效果的数值。",MessageType.Info);
   scroll=EditorGUILayout.BeginScrollView(scroll);
   EditorGUILayout.BeginHorizontal();GUILayout.Label("行",EditorStyles.boldLabel,GUILayout.Width(35));for(int c=0;c<Widths.Length;c++)if(Enemy||c<11||c==15){GUILayout.Label(c==2?"效果类型（持续）":c==3?"效果类型（性质）":CardTableWorkbook.Headers[c],EditorStyles.boldLabel,GUILayout.Width(Widths[c]));if(c==5)GUILayout.Label("具体数值",EditorStyles.boldLabel,GUILayout.Width(90));}GUILayout.Label("卡效所属池",EditorStyles.boldLabel,GUILayout.Width(240));EditorGUILayout.EndHorizontal();
   Undo.RecordObject(table,"编辑卡牌配表");EditorGUI.BeginChangeCheck();
   for(int i=0;i<table.rows.Length;i++){
    var r=table.rows[i];if(r==null){EditorGUILayout.HelpBox("第 "+(i+1)+" 行对象为空，请在 Inspector 删除或补齐。",MessageType.Error);continue;}if(enabledOnly&&!r.enabled||!string.IsNullOrEmpty(search)&&!((r.id??"")+" "+r.name+" "+r.description).Contains(search))continue;
    var color=GUI.backgroundColor;if(i==selected)GUI.backgroundColor=new Color(.5f,.75f,1);EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
    bool tick=EditorGUILayout.Toggle(checkedRows.Contains(i),GUILayout.Width(18));if(tick)checkedRows.Add(i);else checkedRows.Remove(i);
    if(GUILayout.Toggle(selected==i,(i+1).ToString(),"Button",GUILayout.Width(35),GUILayout.Height(48))&&selected!=i){selected=i;showCompound=true;}
    r.id=EditorGUILayout.TextField(r.id,GUILayout.Width(Widths[0]));r.name=EditorGUILayout.TextField(r.name,GUILayout.Width(Widths[1]));
    r.lifetime=(EffectLifetime)EditorGUILayout.Popup((int)r.lifetime,Life,GUILayout.Width(Widths[2]));r.polarity=(EffectPolarity)EditorGUILayout.Popup((int)r.polarity,Polarity,GUILayout.Width(Widths[3]));
    r.description=EditorGUILayout.TextArea(r.description,GUILayout.Width(Widths[4]),GUILayout.Height(48));
    string range=EditorGUILayout.TextField(r.range,GUILayout.Width(Widths[5]));if(range!=r.range){r.range=range;try{CardTableWorkbook.SetRange(r,range);}catch(FormatException){}}
    r.fixedValue=EditorGUILayout.IntField(r.fixedValue,GUILayout.Width(90));
    r.notes=EditorGUILayout.TextArea(r.notes,GUILayout.Width(Widths[6]),GUILayout.Height(48));r.enabled=EditorGUILayout.Toggle(r.enabled,GUILayout.Width(Widths[7]));r.number=EditorGUILayout.IntField(r.number,GUILayout.Width(Widths[8]));r.weight=EditorGUILayout.FloatField(r.weight,GUILayout.Width(Widths[9]));r.starting=EditorGUILayout.Toggle(r.starting,GUILayout.Width(Widths[10]));
    if(Enemy){r.special=EditorGUILayout.Toggle(r.special,GUILayout.Width(Widths[11]));r.condition=(ActivationCondition)EditorGUILayout.Popup((int)r.condition,new[]{"回合","生命百分比"},GUILayout.Width(Widths[12]));r.threshold=EditorGUILayout.IntField(r.threshold,GUILayout.Width(Widths[13]));r.specialWeightRatio=EditorGUILayout.FloatField(r.specialWeightRatio,GUILayout.Width(Widths[14]));}
    r.effect=(CardEffect)EditorGUILayout.Popup((int)r.effect,Effects,GUILayout.Width(Widths[15]));r.pools=EditorGUILayout.TextField(r.pools,GUILayout.Width(240));EditorGUILayout.EndHorizontal();GUI.backgroundColor=color;
    foreach(var issue in issues.Where(e=>e.Row==i+1))EditorGUILayout.HelpBox(issue.Field+"："+issue.Message,MessageType.Error);
   }
   if(EditorGUI.EndChangeCheck())EditorUtility.SetDirty(table);EditorGUILayout.EndScrollView();DrawCompound();
  }
  bool Visible(CardTableRow r)=>r!=null&&(!enabledOnly||r.enabled)&&(string.IsNullOrEmpty(search)||((r.id??"")+" "+r.name+" "+r.description).Contains(search));
  void DrawBulk(){
   EditorGUILayout.BeginHorizontal();showBulk=EditorGUILayout.Foldout(showBulk,"批量编辑 · 已勾选 "+checkedRows.Count+" 行",true);if(GUILayout.Button("勾选当前筛选",GUILayout.Width(120)))for(int i=0;i<table.rows.Length;i++)if(Visible(table.rows[i]))checkedRows.Add(i);if(GUILayout.Button("清空勾选",GUILayout.Width(90)))checkedRows.Clear();EditorGUILayout.EndHorizontal();
   if(!showBulk)return;EditorGUILayout.HelpBox("只有勾选的字段会改到勾选行；筛选不会清掉先前勾选。整批校验失败时没有任何行被修改。",MessageType.Info);
   EditorGUILayout.BeginHorizontal();setEnabled=GUILayout.Toggle(setEnabled,"修改启用");bulkEnabled=EditorGUILayout.Toggle(bulkEnabled,GUILayout.Width(25));setStarting=GUILayout.Toggle(setStarting,"修改起始牌");bulkStarting=EditorGUILayout.Toggle(bulkStarting,GUILayout.Width(25));setWeight=GUILayout.Toggle(setWeight,"修改权重");bulkWeight=EditorGUILayout.FloatField(bulkWeight,GUILayout.Width(75));EditorGUILayout.EndHorizontal();
   EditorGUILayout.BeginHorizontal();setNumber=GUILayout.Toggle(setNumber,"修改数字");bulkNumber=EditorGUILayout.IntField(bulkNumber,GUILayout.Width(60));setRange=GUILayout.Toggle(setRange,"修改X范围");bulkRange=EditorGUILayout.TextField(bulkRange,GUILayout.Width(120));setEffect=GUILayout.Toggle(setEffect,"修改结算效果");bulkEffect=EditorGUILayout.Popup(bulkEffect,Effects,GUILayout.Width(125));using(new EditorGUI.DisabledScope(checkedRows.Count==0||!(setEnabled||setStarting||setWeight||setNumber||setRange||setEffect)))if(GUILayout.Button("应用批量修改"))Action(()=>CardTableBulkEdit.Apply(table,checkedRows,r=>{if(setEnabled)r.enabled=bulkEnabled;if(setStarting)r.starting=bulkStarting;if(setWeight)r.weight=bulkWeight;if(setNumber)r.number=bulkNumber;if(setRange)r.range=bulkRange;if(setEffect)r.effect=(CardEffect)bulkEffect;}));EditorGUILayout.EndHorizontal();
  }
  void DrawCompound(){
   if(selected<0||selected>=table.rows.Length||table.rows[selected]==null)return;var row=table.rows[selected];showCompound=EditorGUILayout.Foldout(showCompound,"选中行 "+row.id+" / 复合效果配置（主效果为序号1）",true);if(!showCompound)return;
   EditorGUILayout.HelpBox("执行顺序按列表排列，作用对象仍由效果行为决定。运行时按顺序执行全部启用效果，每个效果数值在生成时确定。",MessageType.Info);
   Undo.RecordObject(table,"编辑卡牌复合效果");EditorGUI.BeginChangeCheck();var list=(row.additionalEffects??Array.Empty<CardTableEffect>()).ToList();int remove=-1,up=-1;
   EditorGUILayout.BeginHorizontal();foreach(var column in new[]{new{Title="序号",Width=55},new{Title="启用",Width=20},new{Title="结算效果",Width=120},new{Title="持续方式",Width=110},new{Title="性质",Width=100},new{Title="X范围",Width=85}})GUILayout.Label(column.Title,GUILayout.Width(column.Width));GUILayout.Label("效果描述");EditorGUILayout.EndHorizontal();
   compoundScroll=EditorGUILayout.BeginScrollView(compoundScroll,GUILayout.MaxHeight(160));
   for(int i=0;i<list.Count;i++){var e=list[i];if(e==null)continue;EditorGUILayout.BeginHorizontal();EditorGUILayout.LabelField("效果 "+(i+2),GUILayout.Width(55));e.enabled=EditorGUILayout.Toggle(e.enabled,GUILayout.Width(20));e.effect=(CardEffect)EditorGUILayout.Popup((int)e.effect,Effects,GUILayout.Width(120));e.lifetime=(EffectLifetime)EditorGUILayout.Popup((int)e.lifetime,Life,GUILayout.Width(110));e.polarity=(EffectPolarity)EditorGUILayout.Popup((int)e.polarity,Polarity,GUILayout.Width(100));e.range=EditorGUILayout.TextField(e.range,GUILayout.Width(85));e.description=EditorGUILayout.TextField(e.description);using(new EditorGUI.DisabledScope(i==0))if(GUILayout.Button("↑",GUILayout.Width(25)))up=i;if(GUILayout.Button("删除",GUILayout.Width(45)))remove=i;EditorGUILayout.EndHorizontal();}
   EditorGUILayout.EndScrollView();if(EditorGUI.EndChangeCheck())EditorUtility.SetDirty(table);
   if(remove>=0){list.RemoveAt(remove);row.additionalEffects=list.ToArray();EditorUtility.SetDirty(table);}if(up>0){var e=list[up];list[up]=list[up-1];list[up-1]=e;row.additionalEffects=list.ToArray();EditorUtility.SetDirty(table);}
   using(new EditorGUI.DisabledScope(list.Count>=99))if(GUILayout.Button("添加追加效果")){list.Add(new CardTableEffect());row.additionalEffects=list.ToArray();EditorUtility.SetDirty(table);}
  }
  string Validation(){var errors=CardTableValidation.Check(table.rows,Enemy);return errors.Length==0?null:string.Join("\n",errors.Select(e=>e.ToString()));}
  public void SaveTable(){var error=Validation();if(error!=null)throw new InvalidOperationException(error);Undo.RecordObject(table,"保存卡牌配表");foreach(var row in table.rows)CardTableWorkbook.SetRange(row,row.range);EditorUtility.SetDirty(table);AssetDatabase.SaveAssetIfDirty(table);}
  public void Import(string[][] cells,bool replace){
   var plan=Preview(cells,replace);if(replace)foreach(var change in plan.Changes.Where(c=>c.Kind=="删除"))change.Selected=true;plan.Apply();selected=-1;checkedRows.Clear();
  }
  public CardTableImportPlan Preview(string[][] cells,bool replace,string[][] compound=null){var catalog=AssetDatabase.LoadAssetAtPath<CardEffectCatalog>("Assets/RainbowLi/Data/DefaultCardEffects.asset");var plan=new CardTableImportPlan(table,cells,replace,catalog==null?Array.Empty<CardTableRow>():catalog.effects.Where(e=>e!=null).Select(CardTableRow.From),compound);plan.Applied+=UndoRefresh;return plan;}
  public CardTableImportPlan PreviewFile(string path,bool replace)=>Preview(CardTableWorkbook.Read(path),replace,CardTableWorkbook.ReadSheet(path,"复合效果",8));
  void Add(bool copy){Undo.RecordObject(table,"新增卡牌行");var r=copy?table.rows[selected].Copy():CardTableRow.From(AssetDatabase.LoadAssetAtPath<CardEffectDefinition>("Assets/RainbowLi/Data/CardEffects/CardEffect_01.asset"));string stem=r.id+"_",id=stem+1;for(int i=2;table.rows.Any(row=>row.id==id);i++)id=stem+i;r.id=id;table.rows=table.rows.Concat(new[]{r}).ToArray();selected=table.rows.Length-1;EditorUtility.SetDirty(table);}
  void Delete(){Undo.RecordObject(table,"删除卡牌行");table.rows=table.rows.Where((r,i)=>i!=selected).ToArray();selected=-1;checkedRows.Clear();EditorUtility.SetDirty(table);}
  void Bind(){SaveTable();if(actor==null)throw new InvalidOperationException("请先选择角色配置资产。");Undo.RecordObject(actor,"绑定卡牌表");if(Enemy)((EnemyDefinition)actor).cardTable=(EnemyCardTable)table;else ((PlayerDefinition)actor).cardTable=(PlayerCardTable)table;EditorUtility.SetDirty(actor);AssetDatabase.SaveAssetIfDirty(actor);}
  void CreateTable(){var path=EditorUtility.SaveFilePanelInProject("新建卡牌配表",Enemy?"EnemyCards":"PlayerCards","asset","选择 RainbowLi 内的保存位置","Assets/RainbowLi/Data");if(path=="")return;if(!path.StartsWith("Assets/RainbowLi/",StringComparison.Ordinal))throw new InvalidOperationException("配表必须保存在 Assets/RainbowLi 内。");var asset=Enemy?(CardTableDefinition)CreateInstance<EnemyCardTable>():CreateInstance<PlayerCardTable>();var catalog=AssetDatabase.LoadAssetAtPath<CardEffectCatalog>("Assets/RainbowLi/Data/DefaultCardEffects.asset");asset.rows=catalog.effects.Select(CardTableRow.From).ToArray();AssetDatabase.CreateAsset(asset,path);Undo.RegisterCreatedObjectUndo(asset,"新建配表");SetTable(asset);}
  void Export(){SaveTable();string path=EditorUtility.SaveFilePanel("导出卡牌表",Path.GetFullPath("Assets/RainbowLi"),Enemy?"敌人卡牌表":"玩家卡牌表","xlsx");if(path=="")return;string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{CardTableWorkbook.Write(temp,table.rows);File.Copy(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
  protected void OpenDefault(){SetTable(AssetDatabase.LoadAssetAtPath<CardTableDefinition>(Enemy?"Assets/RainbowLi/Data/DefaultEnemyCards.asset":"Assets/RainbowLi/Data/DefaultPlayerCards.asset"));actor=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Enemy?"Assets/RainbowLi/Data/DefaultEnemy.asset":"Assets/RainbowLi/Data/DefaultPlayer.asset");}
 }
}
