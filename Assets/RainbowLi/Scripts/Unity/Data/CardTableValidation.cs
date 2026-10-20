using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace RainbowLi.UnityGame {
 public sealed class CardTableIssue {
  public int Row;public string Field,Message;
  public override string ToString()=>(Row>0?"第 "+Row+" 行 · ":"")+Field+"："+Message;
 }
 public static class CardTableRange {
  public static void Parse(string text,out bool usesNumber,out int min,out int max){
   string n=(text??"").Trim().Replace('－','-').Replace('–','-').Replace('—','-').Replace('~','-').Replace('～','-');
   if(n=="为卡牌对应数字"||n=="卡牌对应数字"||n=="数字"){usesNumber=true;min=1;max=6;return;}
   var m=Regex.Match(n,@"^(\d+)\s*(?:-\s*(\d+))?$");
   if(!m.Success||!int.TryParse(m.Groups[1].Value,out min))throw new FormatException("填写 1、1-2 或 为卡牌对应数字。");
   max=min;if(m.Groups[2].Success&&!int.TryParse(m.Groups[2].Value,out max))throw new FormatException("范围数字太大。");
   usesNumber=false;
  }
 }
 public static class CardTableValidation {
  public static CardTableIssue[] Check(CardTableRow[] rows,bool enemy){
   var errors=new List<CardTableIssue>();var ids=new Dictionary<string,int>(StringComparer.Ordinal);
   if(rows==null){errors.Add(new CardTableIssue{Field="配表",Message="行数组不能为空。"});return errors.ToArray();}
   for(int i=0;i<rows.Length;i++){
    var r=rows[i];int line=i+1;
    Action<string,string> error=(field,message)=>errors.Add(new CardTableIssue{Row=line,Field=field,Message=message});
    if(r==null){error("行","空行对象，请删除或补齐。");continue;}
    if(string.IsNullOrWhiteSpace(r.id))error("效果编号","不能为空。");
    else if(r.id!=r.id.Trim())error("效果编号","首尾不能含空格。");
    else if(ids.TryGetValue(r.id,out int first))error("效果编号","与第 "+first+" 行重复："+r.id);else ids[r.id]=line;
    if(string.IsNullOrWhiteSpace(r.name))error("效果名称","不能为空。");
    if(r.number<1||r.number>6)error("数字","须为 1–6。");
    if(!FinitePositive(r.weight))error("权重","须是大于零的有限数值。");
    CheckEffect(r.effect,r.lifetime,r.polarity,r.range,error,r.fixedValue);
    if(r.effect==CardEffect.IronWall&&(r.shieldBonusPercent<0||r.shieldBonusPercent>10000))error("护盾增幅(%)","须为 0–10000；25 表示护盾增加 25%。");
    if((r.effect==CardEffect.Weaken||r.effect==CardEffect.Fortify)&&(r.reductionPercent<0||r.reductionPercent>100))error("减幅(%)","须为 0–100；50 表示伤害减少 50%。");
    if(!Enum.IsDefined(typeof(ActivationCondition),r.condition))error("触发条件","未知条件。");
    if(r.special){
     if(float.IsNaN(r.specialWeightRatio)||float.IsInfinity(r.specialWeightRatio)||r.specialWeightRatio<.3f||r.specialWeightRatio>.7f)error("特殊权重比","须为 0.3–0.7 的有限数值。");
     if(r.threshold<1||r.condition==ActivationCondition.HealthPercentAtMost&&r.threshold>100)error("阈值","回合须大于零，生命百分比须为 1–100。");
    }
    var extra=r.additionalEffects??Array.Empty<CardTableEffect>();
    if(extra.Length>99)error("追加效果","最多 99 个；包含主效果共 100 个。");
    for(int j=0;j<extra.Length;j++){int ordinal=j+2;var e=extra[j];if(e==null){error("效果 "+ordinal,"不能为空。");continue;}if(e.effect==CardEffect.IronWall&&(e.shieldBonusPercent<0||e.shieldBonusPercent>10000))error("效果 "+ordinal+" / 护盾增幅(%)","须为 0–10000。");if((e.effect==CardEffect.Weaken||e.effect==CardEffect.Fortify)&&(e.reductionPercent<0||e.reductionPercent>100))error("效果 "+ordinal+" / 减幅(%)","须为 0–100。");CheckEffect(e.effect,e.lifetime,e.polarity,e.range,(field,message)=>error("效果 "+ordinal+" / "+field,message));}
   }
   if(!rows.Any(r=>r!=null&&r.enabled))errors.Add(new CardTableIssue{Field="配表",Message="至少启用一行卡牌。"});
   else if(enemy&&!rows.Any(r=>r!=null&&r.enabled&&!r.special))errors.Add(new CardTableIssue{Field="敌人卡牌池",Message="至少启用一种基础卡牌。"});
   else if(!enemy&&!rows.Any(r=>r!=null&&r.enabled&&r.starting))errors.Add(new CardTableIssue{Field="玩家起始牌",Message="至少启用一种起始牌效果。"});
   return errors.ToArray();
  }
  static bool FinitePositive(float f)=>f>0&&!float.IsNaN(f)&&!float.IsInfinity(f);
  static void CheckEffect(CardEffect effect,EffectLifetime life,EffectPolarity polarity,string range,Action<string,string> error,int fixedValue=0){
   if(!Enum.IsDefined(typeof(CardEffect),effect))error("结算效果","未知效果。");
   if(!Enum.IsDefined(typeof(EffectLifetime),life))error("持续方式","未知类型。");
   if(!Enum.IsDefined(typeof(EffectPolarity),polarity))error("性质","未知类型。");
   if(fixedValue!=0){if(fixedValue<1||fixedValue>10000)error("具体数值","须为 0–10000；0 沿用基础取值范围。");return;}
   try{CardTableRange.Parse(range,out _,out int min,out int max);if(min<1||max<min||max>10000)error("基础取值范围","须为 1–10000，且上限不小于下限。");}catch(FormatException e){error("基础取值范围",e.Message);}
  }
 }
}
