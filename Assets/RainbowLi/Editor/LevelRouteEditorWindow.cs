using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public sealed class LevelRouteEditorWindow : EditorWindow {
 const string Root="Assets/RainbowLi/";
 [SerializeField] private LevelDefinition definition;
 private int selected=-1,linkFrom=-1,newFloor,newLane,previewSeed=104729;
 private bool linkMode,dragging;
 private Vector2 graphScroll,propertyScroll;
 private string result="";
 [MenuItem("RainbowLi/关卡/关卡编辑器")]
 public static void Open(){Open(Selection.activeObject as LevelDefinition??AssetDatabase.LoadAssetAtPath<LevelDefinition>(Root+"Data/DefaultLevels.asset"));}
 public static void Open(LevelDefinition asset){var w=GetWindow<LevelRouteEditorWindow>("关卡路线编辑器");w.minSize=new Vector2(930,620);w.definition=asset;w.Show();}
 private void OnEnable(){Undo.undoRedoPerformed+=OnUndo;}
 private void OnDisable(){Undo.undoRedoPerformed-=OnUndo;}
 private void OnUndo(){Repaint();}
 public static string ValidateDefinition(LevelDefinition d){
  if(d==null)return "请先选择关卡配置。";
  if(d.authoredNodes==null||d.authoredNodes.Count==0)return "路线没有节点。";
  if(d.authoredNodes.Any(n=>n==null||n.id<0||n.floor<0||n.lane<0))return "节点 ID、层、列必须为非负整数。";
  if(d.authoredNodes.GroupBy(n=>n.id).Any(g=>g.Count()>1))return "存在重复节点 ID。";
  if(d.authoredNodes.GroupBy(n=>new {n.floor,n.lane}).Any(g=>g.Count()>1))return "同一层、同一列不能放置多个节点。";
  int top=d.authoredNodes.Max(n=>n.floor);
  if(d.authoredNodes.Count(n=>n.kind==LevelKind.Boss)!=1||d.authoredNodes.Count(n=>n.floor==top)!=1||d.authoredNodes.SingleOrDefault(n=>n.kind==LevelKind.Boss)?.floor!=top)return "最终层必须只有一个首领，其他层不能设置首领。";
  if(d.independentStacks){try{new RouteGraph(d.authoredNodes,true,false,-1,true);}catch(ArgumentException e){return "三列独立关卡配置无效："+e.Message;}return null;}
  if(d.cardDungeon){try{new RouteGraph(d.authoredNodes,true,true,d.startingLane);}catch(ArgumentException e){return "卡片地牢配置无效：检查起点列，以及每个格子是否有下一行左、中、右出口。\n"+e.Message;}return null;}
  if(d.stackedLayout)return null;
  foreach(var n in d.authoredNodes){if(n.next==null)return "节点 "+n.id+" 的连线列表为空。";if(n.next.Distinct().Count()!=n.next.Count)return "节点 "+n.id+" 存在重复连线。";if(n.floor<top&&n.next.Count==0)return "节点 "+n.id+" 是死路，请连接下一层。";foreach(int id in n.next){var next=d.authoredNodes.Find(v=>v.id==id);if(next==null||next.floor!=n.floor+1)return "节点 "+n.id+" 的连线必须指向下一层已有节点。";}}
  try{new RouteGraph(d.authoredNodes);}catch(ArgumentException){return "存在不可达节点或首领出口，请检查每一层连线。";}
  return null;
 }
 public static void ToggleConnection(LevelDefinition d,int from,int to){var a=d.authoredNodes.Find(n=>n.id==from);var b=d.authoredNodes.Find(n=>n.id==to);if(a==null||b==null||b.floor!=a.floor+1)throw new ArgumentException("只能连接下一层节点。");Undo.RecordObject(d,"编辑关卡连线");if(!a.next.Remove(to))a.next.Add(to);EditorUtility.SetDirty(d);}
 public static void Generate(LevelDefinition d,int seed){Undo.RecordObject(d,"生成路线模板");d.authoredNodes=RouteGenerator.Generate(seed,d.floors,d.independentStacks?3:d.lanes).Nodes;d.presentations.Clear();EditorUtility.SetDirty(d);}
 public static void RemoveNode(LevelDefinition d,int id){Undo.RecordObject(d,"删除关卡节点");d.authoredNodes.RemoveAll(n=>n.id==id);foreach(var n in d.authoredNodes)n.next.RemoveAll(v=>v==id);d.presentations.RemoveAll(p=>p!=null&&p.nodeId==id);EditorUtility.SetDirty(d);}
 private bool Writable()=>definition!=null&&AssetDatabase.GetAssetPath(definition).StartsWith(Root,StringComparison.Ordinal);
 private void OnGUI(){
  using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)){
   definition=(LevelDefinition)EditorGUILayout.ObjectField(definition,typeof(LevelDefinition),false,GUILayout.Width(290));
   if(GUILayout.Button("新建配置",EditorStyles.toolbarButton,GUILayout.Width(90)))Create();
   using(new EditorGUI.DisabledScope(!Writable())){if(GUILayout.Button("校验并保存",EditorStyles.toolbarButton,GUILayout.Width(110)))Save();}
   GUILayout.FlexibleSpace();GUILayout.Label("拖动格子调整行 / 列",EditorStyles.miniLabel);
  }
  if(!Writable()){EditorGUILayout.HelpBox("选择 Assets/RainbowLi 内的 LevelDefinition 配置。编辑器只修改 RainbowLi 文件夹。",MessageType.Info);return;}
  using(new EditorGUILayout.HorizontalScope()){
   using(new EditorGUILayout.VerticalScope(GUILayout.Width(position.width-342))){
    using(new EditorGUILayout.HorizontalScope()){
     if(!definition.independentStacks&&!definition.stackedLayout&&!definition.cardDungeon)linkMode=GUILayout.Toggle(linkMode,"连线模式",EditorStyles.miniButton,GUILayout.Width(90));else{linkMode=false;GUILayout.Label(definition.independentStacks?"三列独立卡堆":definition.cardDungeon?"卡片地牢":"单列推进",GUILayout.Width(90));}if(!linkMode)linkFrom=-1;
     newFloor=EditorGUILayout.IntField("行 (从 1 起)",newFloor+1,GUILayout.Width(180))-1;newLane=EditorGUILayout.IntField("列",newLane+1,GUILayout.Width(105))-1;
     if(GUILayout.Button("添加节点",GUILayout.Width(85)))AddNode();
    }
    var canvas=GUILayoutUtility.GetRect(300,position.height-145,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));DrawGraph(canvas);
    EditorGUILayout.HelpBox(definition.independentStacks?"三列独立卡堆：每列按行号顺序排列。完成一张前牌后，只补上同列后牌；另外两列不变。":definition.cardDungeon?"玩家从起点列出发，每步只能进入下一行左 / 中 / 右格子。完成后更新玩家位置，下次可以换方向。":definition.stackedLayout?"起点选定一列后，其他列隐藏并不可进入；完成当前关卡只露出正后方的下一关。":linkMode?"先点起点，再点下一层终点；重复连接可删除连线。右侧也可逐项勾选。":"层数从下往上。点击节点编辑类型与说明；拖动会吸附到网格；Ctrl+Z 撤销。",MessageType.Info);
   }
   using(new EditorGUILayout.VerticalScope(GUILayout.Width(315))){propertyScroll=EditorGUILayout.BeginScrollView(propertyScroll);DrawProperties();EditorGUILayout.EndScrollView();}
  }
  string error=ValidateDefinition(definition);EditorGUILayout.HelpBox(error??(string.IsNullOrEmpty(result)?"路线有效。修改后点击“校验并保存”；运行中的路线需重新开始才会读取新配置。":result),error==null?MessageType.Info:MessageType.Warning);
 }
 private Rect NodeRect(LevelNodeData n,int top)=>new Rect(32+n.lane*145,40+(top-n.floor)*112,118,76);
 private void DrawGraph(Rect viewport){
  int top=Mathf.Max(definition.floors-1,definition.authoredNodes.Count==0?0:definition.authoredNodes.Max(n=>n.floor));int cols=Mathf.Max(definition.lanes,definition.authoredNodes.Count==0?1:definition.authoredNodes.Max(n=>n.lane)+1);
  graphScroll=GUI.BeginScrollView(viewport,graphScroll,new Rect(0,0,Mathf.Max(viewport.width-18,cols*145+70),(top+1)*112+100));
  EditorGUI.DrawRect(new Rect(0,0,cols*145+70,(top+1)*112+100),new Color(.12f,.13f,.15f));
  for(int f=0;f<=top;f++){GUI.Label(new Rect(7,47+(top-f)*112,35,22),(f+1).ToString(),EditorStyles.whiteMiniLabel);EditorGUI.DrawRect(new Rect(30,125+(top-f)*112,cols*145,1),new Color(.22f,.23f,.25f));}
  Handles.BeginGUI();if(!definition.independentStacks&&!definition.stackedLayout&&!definition.cardDungeon)foreach(var n in definition.authoredNodes)foreach(int id in n.next){var next=definition.authoredNodes.Find(v=>v.id==id);if(next==null)continue;var a=NodeRect(n,top);var b=NodeRect(next,top);Handles.color=n.id==selected?new Color(.96f,.74f,.35f):new Color(.46f,.5f,.54f);Handles.DrawAAPolyLine(3,new Vector3(a.center.x,a.y),new Vector3(b.center.x,b.yMax));}Handles.EndGUI();
  var e=Event.current;
  foreach(var n in definition.authoredNodes){var r=NodeRect(n,top);EditorGUI.DrawRect(r,n.id==selected?new Color(.45f,.33f,.18f):n.kind==LevelKind.Boss||n.kind==LevelKind.Elite?new Color(.32f,.17f,.21f):new Color(.22f,.26f,.28f));GUI.Label(new Rect(r.x+8,r.y+8,r.width-16,58),"#"+n.id+"  "+LevelNodeView.KindName(n.kind)+"\n第 "+(n.floor+1)+" 层 / 第 "+(n.lane+1)+" 列",EditorStyles.whiteLabel);
   if(e.type==EventType.MouseDown&&e.button==0&&r.Contains(e.mousePosition)){
    if(linkMode){if(linkFrom<0){linkFrom=n.id;selected=n.id;}else{try{ToggleConnection(definition,linkFrom,n.id);result="连线已修改，记得保存。";}catch(ArgumentException ex){result=ex.Message;}linkFrom=-1;}}
    else{selected=n.id;dragging=true;Undo.RecordObject(definition,"移动关卡节点");}e.Use();Repaint();break;
   }
  }
  if(dragging&&e.type==EventType.MouseDrag){var n=definition.authoredNodes.Find(v=>v.id==selected);if(n!=null){n.lane=Mathf.Clamp(Mathf.RoundToInt((e.mousePosition.x-91)/145),0,4);n.floor=Mathf.Clamp(top-Mathf.RoundToInt((e.mousePosition.y-78)/112),0,15);EditorUtility.SetDirty(definition);}e.Use();Repaint();}
  if(e.type==EventType.MouseUp)dragging=false;
  GUI.EndScrollView();
 }
 private void DrawProperties(){
  EditorGUILayout.LabelField("路线配置",EditorStyles.boldLabel);EditorGUI.BeginChangeCheck();int adventureLayer=Mathf.Max(1,EditorGUILayout.IntField("当前冒险大层数",definition.adventureLayer));EditorGUILayout.LabelField("BOSS 解锁战斗数",(5L+adventureLayer).ToString());bool random=EditorGUILayout.Toggle("每局随机生成",definition.randomizeEachRun);int floors=EditorGUILayout.IntSlider("生成层数",definition.floors,4,16);int lanes=EditorGUILayout.IntSlider("生成列数",definition.lanes,2,5);int seed=EditorGUILayout.IntField("运行种子 (0 随机)",definition.seed);
  bool independent=EditorGUILayout.Toggle("三列独立关卡",definition.independentStacks);
  bool dungeon=EditorGUILayout.Toggle("玩家移动的卡片地牢",definition.cardDungeon);int startLane=EditorGUILayout.IntField("起点列 (-1 中间 / 0 起)",definition.startingLane);
  bool stacked=definition.stackedLayout;using(new EditorGUI.DisabledScope(dungeon)){stacked=EditorGUILayout.Toggle("旧版单列推进",stacked);}
  if(EditorGUI.EndChangeCheck()){Undo.RecordObject(definition,"路线设置");definition.independentStacks=independent;definition.adventureLayer=adventureLayer;definition.cardDungeon=dungeon;definition.startingLane=Mathf.Max(-1,startLane);definition.stackedLayout=dungeon||stacked;definition.randomizeEachRun=random;definition.floors=floors;definition.lanes=independent?3:lanes;definition.seed=seed;EditorUtility.SetDirty(definition);}
  if(random)EditorGUILayout.HelpBox("当前为随机模式。图中节点仅为模板，不会按固定节点运行。取消“每局随机生成”即可使用本图。",MessageType.Warning);
  previewSeed=EditorGUILayout.IntField("模板生成种子",previewSeed);if(GUILayout.Button("重新生成模板（可撤销）")){Generate(definition,previewSeed);selected=-1;result="已生成模板。";}
  EditorGUILayout.Space();var n=definition.authoredNodes.Find(v=>v.id==selected);if(n==null){EditorGUILayout.HelpBox("点击左侧节点，编辑关卡类型、文字、图标与出口。",MessageType.Info);return;}
  EditorGUILayout.LabelField("节点 #"+n.id,EditorStyles.boldLabel);EditorGUI.BeginChangeCheck();var kind=(LevelKind)EditorGUILayout.Popup("关卡类型",(int)n.kind,new[]{"战斗","精英","休息","商店","事件","首领"});int floor=EditorGUILayout.IntSlider("行",n.floor+1,1,16)-1;int lane=EditorGUILayout.IntSlider("列",n.lane+1,1,5)-1;
  if(EditorGUI.EndChangeCheck()){Undo.RecordObject(definition,"节点属性");n.kind=kind;n.floor=floor;n.lane=lane;EditorUtility.SetDirty(definition);}
  var p=definition.presentations.Find(v=>v!=null&&v.nodeId==n.id);EditorGUI.BeginChangeCheck();string title=EditorGUILayout.TextField("显示名称",p?.title??"");EditorGUILayout.LabelField("悬停说明（空为默认）");string body=EditorGUILayout.TextArea(p?.description??"",GUILayout.Height(80));var icon=(Sprite)EditorGUILayout.ObjectField("替换图标",p?.icon,typeof(Sprite),false);
  if(EditorGUI.EndChangeCheck()){Undo.RecordObject(definition,"节点展示");if(p==null){p=new LevelDefinition.NodePresentation{nodeId=n.id};definition.presentations.Add(p);}p.title=title;p.description=body;p.icon=icon;EditorUtility.SetDirty(definition);}
  if(definition.independentStacks)EditorGUILayout.HelpBox("列为 0、1、2；每列按行号由小到大排列。完成前牌只替补同列后牌，其他两列保留。无需连线。",MessageType.Info);
  else if(definition.cardDungeon)EditorGUILayout.HelpBox("行 / 列决定空间位置，进入该格后玩家移动到这里。下一步可以选择其前方左、中、右格子。无需手动连线，可中途换列。",MessageType.Info);
  else if(definition.stackedLayout)EditorGUILayout.HelpBox("列决定所属关卡堆；层决定前后顺序，越小越靠前。同列前关未完成时，后关不可进入。所选列清空后显露首领。",MessageType.Info);
  else {EditorGUILayout.Space();EditorGUILayout.LabelField("连接下一层",EditorStyles.boldLabel);foreach(var next in definition.authoredNodes.Where(v=>v.floor==n.floor+1).ToArray()){bool before=n.next.Contains(next.id);bool after=EditorGUILayout.ToggleLeft("#"+next.id+" "+LevelNodeView.KindName(next.kind)+" / 列 "+(next.lane+1),before);if(before!=after)ToggleConnection(definition,n.id,next.id);}
  foreach(int id in n.next.Where(id=>!definition.authoredNodes.Any(v=>v.id==id&&v.floor==n.floor+1)).ToArray())if(GUILayout.Button("移除无效出口 #"+id)){Undo.RecordObject(definition,"移除无效连线");n.next.Remove(id);EditorUtility.SetDirty(definition);}}
  EditorGUILayout.Space();if(GUILayout.Button("删除节点（可撤销）")){RemoveNode(definition,n.id);selected=-1;}
  if(GUILayout.Button("在 Inspector 查看全局奖励与精英参数")){Selection.activeObject=definition;EditorGUIUtility.PingObject(definition);}
 }
 private void AddNode(){newFloor=Mathf.Clamp(newFloor,0,15);newLane=Mathf.Clamp(newLane,0,4);if(definition.authoredNodes.Any(n=>n.floor==newFloor&&n.lane==newLane)){result="此位置已有节点。";return;}Undo.RecordObject(definition,"添加关卡节点");int id=definition.authoredNodes.Count==0?0:definition.authoredNodes.Max(n=>n.id)+1;definition.authoredNodes.Add(new LevelNodeData{id=id,floor=newFloor,lane=newLane,kind=LevelKind.Combat});selected=id;EditorUtility.SetDirty(definition);}
 private void Create(){string path=EditorUtility.SaveFilePanelInProject("新建关卡配置","LevelRoute","asset","保存到 Assets/RainbowLi/Data",Root+"Data");if(string.IsNullOrEmpty(path))return;if(!path.StartsWith(Root,StringComparison.Ordinal)){result="只允许保存到 RainbowLi 文件夹。";return;}var d=CreateInstance<LevelDefinition>();d.randomizeEachRun=false;d.authoredNodes=RouteGenerator.Generate(previewSeed,d.floors,d.independentStacks?3:d.lanes).Nodes;AssetDatabase.CreateAsset(d,path);definition=d;selected=-1;}
 private void Save(){string error=ValidateDefinition(definition);if(error!=null){result=error;return;}EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);result="已保存。将配置拖到场景 LevelSystem / LevelModule 的 Definition，然后重新开始游戏。";}
}
[CustomEditor(typeof(LevelDefinition))]
public sealed class LevelDefinitionInspector : UnityEditor.Editor {
 public override void OnInspectorGUI(){if(GUILayout.Button("打开图形关卡编辑器",GUILayout.Height(30)))LevelRouteEditorWindow.Open((LevelDefinition)target);DrawDefaultInspector();var d=(LevelDefinition)target;if(!d.randomizeEachRun){string error=LevelRouteEditorWindow.ValidateDefinition(d);EditorGUILayout.HelpBox(error??"固定路线有效。重新开始游戏后读取修改。",error==null?MessageType.Info:MessageType.Warning);}}
}
}

