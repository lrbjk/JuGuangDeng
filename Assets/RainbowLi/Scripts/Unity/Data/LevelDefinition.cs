using System.Collections.Generic;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/关卡路线定义")]
 public sealed class LevelDefinition : ScriptableObject {
  [Range(4,16)] public int floors=8;
  [Range(2,5)] public int lanes=3;
  public bool randomizeEachRun=true;
  [Tooltip("0 使用游戏种子；非零固定路线")] public int seed;
  [Tooltip("关闭随机路线时使用，可以编辑节点类型和下一层连接")] public List<LevelNodeData> authoredNodes=new List<LevelNodeData>();
  [Min(0)] public int eventGold=18,eventHealing=16;
  [Min(1)] public float eliteHealthMultiplier=1.5f;
  [Min(0)] public int eliteAttackBonus=3,eliteGold=40;
  public RouteGraph CreateGraph(int gameSeed){return randomizeEachRun||authoredNodes.Count==0?RouteGenerator.Generate(seed==0?gameSeed:seed,floors,lanes):new RouteGraph(authoredNodes);}
 }
}
