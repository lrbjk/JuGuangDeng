using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi {
 [Serializable] public sealed class RuntimeEngraving {
  public string Code; public EngravingEffect Effect; public int BaseX,Multiplier=1,FixedValue,MinX,MaxX;public bool RollX;
  public int Value=>CombatResolver.Clamp((long)BaseX*Multiplier);
  public string Description {
   get {string[] names={"毒素","灼烧","追击","成长","变化","泛用","收尾","冲锋","阻断","桥接","传承","千变万化","轮盘赌","独木成林","谢幕","开幕","充能","毒爆","引燃"};return names[(int)Effect]+(Value>0?" "+Value:"");}
  }
  // Full player-facing rules use resolved values, matching the runtime settlement.
  public string RulesText {
   get {switch(Effect){
    case EngravingEffect.Poison:return "行动后，对对方施加 "+Value+" 层中毒。";
    case EngravingEffect.Burn:return "行动后，对对方施加 "+Value+" 层烧伤。";
    case EngravingEffect.FollowUpDamage:return "此卡作为最后一个数字触发牌序时，对对方追加 "+Value+" 点伤害。";
    case EngravingEffect.Growth:return "此卡参与触发牌序时，卡牌效果永久 +"+Value+"，跨战斗保留。";
    case EngravingEffect.ExtraNumber:return "构成牌序时，此卡同时视作原数字或数字 "+Value+"。";
    case EngravingEffect.Wildcard:return "构成牌序时，此卡可视作任意数字（1—6）。";
    case EngravingEffect.SequenceTailBonus:return "此卡位于触发牌序的末位时，追加结算卡牌效果 +"+(FixedValue>0?FixedValue:3)+" 的增量。";
    case EngravingEffect.SequenceHeadBonus:return "此卡位于触发牌序的首位时，追加结算卡牌效果 +"+(FixedValue>0?FixedValue:3)+" 的增量。";
    case EngravingEffect.BlockSequence:return "此卡及其前后相邻卡牌的数字无法构成牌序。";
    case EngravingEffect.Bridge:return "构成牌序时，此卡可跨过中间数字，与后续卡牌连接。";
    case EngravingEffect.Inherit:return "行动后保留此卡效果，在下一张玩家卡牌行动时追加触发。";
    case EngravingEffect.RandomizeFollowing:return "此卡行动后，本回合后续填入的数字随机变为 1—6。";
    case EngravingEffect.Roulette:return "此卡行动时，50% 概率效果数值为 0，50% 概率效果数值翻倍。";
    case EngravingEffect.OnlyPlayerCard:return "仅此一张玩家卡牌在场时，随机触发本回合弃置的最多3张卡牌效果，各+2。";
    case EngravingEffect.LastActionBonus:return "此卡在行动条最后行动时，本次卡牌效果 +"+(FixedValue>0?FixedValue:2)+"。";
    case EngravingEffect.FirstActionBonus:return "此卡在行动条最先行动时，本次卡牌效果 +"+(FixedValue>0?FixedValue:2)+"。";
    case EngravingEffect.Charge:return "每次正常行动后，此卡后续卡牌效果 +1；本场战斗内累积，战斗结束清除。";
    case EngravingEffect.PoisonExplosion:return "行动后，消耗对方全部中毒，造成消耗层数 1.5 倍的伤害（向下取整）。";
    case EngravingEffect.BurnExplosion:return "行动后，消耗对方全部烧伤，造成消耗层数 2 倍的伤害。";
    default:return "未知铭刻效果，请检查配表。";
   }}
  }
  public RuntimeEngraving Copy()=>(RuntimeEngraving)MemberwiseClone();
 }
 [Serializable] public sealed class EffectPoolEntry {
  public string Code,Pools; public bool Enabled=true; public float Weight=10;
  public int ShieldBonusPercent=25;public int Min,Max,Multiplier=1,FixedValue; public RuleEffect Sequence; public EngravingEffect Engraving;
  public bool InPool(string pool)=>EffectPoolNames.Contains(Pools,pool);
 }
 [Serializable] public sealed class EffectPoolWeight {
  public string Id,Name; public float Weight;
  public EffectPoolWeight Copy()=>(EffectPoolWeight)MemberwiseClone();
 }
 public static class EffectPoolNames {
  public static string Normalize(string name){
   switch((name??"").Trim()){
    case "默认卡池":return "默认池";
    case "伤害卡池":return "伤害池";
    case "增益效果池":return "增益池";
    case "减益效果池":return "减益池";
    case "持续状态效果池":return "持续效果池";
    case "临时状态效果池":return "临时效果池";
    default:return (name??"").Trim();
   }
  }
  public static bool Contains(string names,string pool)=>string.IsNullOrWhiteSpace(pool)||(names??"").Split(new[]{',','，','、'},StringSplitOptions.RemoveEmptyEntries).Any(p=>Normalize(p)==Normalize(pool));
 }
 [Serializable] public sealed class NumberEntry {
  public int Number; public bool Enemy,Blocked; public int[] Candidates; public BattleAction Action;
  public bool Accepts(int number)=>!Blocked&&(Candidates==null?Number==number:Array.IndexOf(Candidates,number)>=0);
 }
 public sealed class RecoverableRandom : Random {
  private uint state;
  public uint State {get=>state;set=>state=value==0?0x9e3779b9:value;}
  public RecoverableRandom(int seed){State=(uint)seed;if(State==0)State=0x9e3779b9;}
  private uint Draw(){uint s=State;s^=s<<13;s^=s>>17;s^=s<<5;return State=s;}
  protected override double Sample()=>Draw()/4294967296.0;
  public override double NextDouble()=>Sample();
  public override int Next()=>Next(int.MaxValue);
  public override int Next(int maxValue)=>Next(0,maxValue);
  public override int Next(int minValue,int maxValue){if(minValue>maxValue)throw new ArgumentOutOfRangeException();return (int)(minValue+(long)(Sample()*((long)maxValue-minValue)));}
  public override void NextBytes(byte[] bytes){if(bytes==null)throw new ArgumentNullException(nameof(bytes));for(int i=0;i<bytes.Length;i++)bytes[i]=(byte)Draw();}
 }
}

