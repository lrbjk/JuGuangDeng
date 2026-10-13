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
  public RuntimeEngraving Copy()=>(RuntimeEngraving)MemberwiseClone();
 }
 [Serializable] public sealed class EffectPoolEntry {
  public string Code,Pools; public bool Enabled=true; public float Weight=10;
  public int Min,Max,Multiplier=1,FixedValue; public RuleEffect Sequence; public EngravingEffect Engraving;
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
