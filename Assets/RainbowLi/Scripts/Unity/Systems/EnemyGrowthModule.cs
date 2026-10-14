using UnityEngine;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class EnemyGrowthModule : MonoBehaviour {
  [Tooltip("按全部已完成关卡数，为后续所有敌人的卡牌效果提供全局数值加成。")]
  public bool enableGrowth=true;
  [Min(1),Tooltip("战斗、精英、事件、休息、商店全部计入；默认每完成 10 关增加一次，新局读取配置。")]
  public int levelsPerStack=10;
  [Min(0),Tooltip("每次卡牌效果增加的数值。攻击、防御、恢复和状态层数均增加，牌序效果不受影响。")]
  public int amountPerStack=1;
  public int CompletedLevels(ContainmentGame game)=>game?.CompletedRoomCount??0;
  public int Count(ContainmentGame game)=>!enableGrowth?0:CompletedLevels(game)/Mathf.Max(1,levelsPerStack);
  public int Amount(ContainmentGame game)=>CombatResolver.Clamp((long)Count(game)*Mathf.Max(0,amountPerStack));
  public string Description(ContainmentGame game){
   if(!enableGrowth)return "敌人全局增益未启用";
   int interval=Mathf.Max(1,levelsPerStack),progress=CompletedLevels(game)%interval;
   return "敌人卡牌效果 +"+Amount(game)+"（全局）\n攻击、防御、恢复、状态层数均增加\n每完成 "+interval+" 关，卡牌效果 +"+Mathf.Max(0,amountPerStack)+"\n距下次增益还需完成 "+(interval-progress)+" 关";
  }
  public void Apply(GameSetup setup){setup.EnemyGrowthEnabled=enableGrowth;setup.EnemyGrowthLevelInterval=Mathf.Max(1,levelsPerStack);setup.EnemyGrowthAmount=Mathf.Max(0,amountPerStack);}
 }
}
