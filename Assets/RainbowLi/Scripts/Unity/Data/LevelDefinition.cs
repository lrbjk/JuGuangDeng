using System.Collections.Generic;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/关卡路线定义")]
 public sealed class LevelDefinition : ScriptableObject {
  [Header("BOSS 解锁")]
  [Min(1),Tooltip("冒险大层数，从 1 开始；与卡片行号独立。完成 5 + 当前冒险层数场普通或精英战斗后，可随时挑战 BOSS。新局读取。")]
  public int adventureLayer=1;
  [Range(4,16)] public int floors=8;
  [Range(2,5)] public int lanes=3;
  public bool randomizeEachRun=true;
  [Tooltip("起点三选一；选定后隐藏其他列，只沿所选列推进。BOSS 独立按完成战斗数解锁。")]
  public bool stackedLayout=true;
  [Header("卡片地牢（优先于旧路线模式）")]
  [Tooltip("玩家每步可进入前方左、中、右卡片，完成后移动位置，中途可换方向。")]
  public bool cardDungeon=true;
  [Tooltip("-1 从中间列开始；其他值从 0 起指定玩家列。")][Min(-1)] public int startingLane=-1;
  [Tooltip("0 使用游戏种子；非零固定路线")] public int seed;
  [Tooltip("关闭随机路线时使用，可以编辑节点类型和下一层连接")] public List<LevelNodeData> authoredNodes=new List<LevelNodeData>();
  [System.Serializable] public sealed class NodePresentation {
   public int nodeId;
   public string title;
   [TextArea(2,5)] public string description;
   public Sprite icon;
  }
  [Header("固定节点展示（按节点 ID 绑定）")]
  [Tooltip("只在固定路线下生效；不会替换节点实际玩法类型。")]
  public List<NodePresentation> presentations=new List<NodePresentation>();
  public NodePresentation Presentation(int id)=>randomizeEachRun?null:presentations.Find(p=>p!=null&&p.nodeId==id);
  [Min(0)] public int eventGold=18,eventHealing=16;
  [Min(1)] public float eliteHealthMultiplier=1.5f;
  [Min(0)] public int eliteAttackBonus=3,eliteGold=40;
  public RouteGraph CreateGraph(int gameSeed){var graph=randomizeEachRun||authoredNodes.Count==0?RouteGenerator.Generate(seed==0?gameSeed:seed,floors,lanes):new RouteGraph(authoredNodes,stackedLayout,cardDungeon,startingLane);return cardDungeon?new RouteGraph(graph.Nodes,true,true,startingLane):stackedLayout?new RouteGraph(graph.Nodes,true):graph;}
 }
}
