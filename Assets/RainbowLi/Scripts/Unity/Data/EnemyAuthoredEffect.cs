using System;
using System.Collections.Generic;
namespace RainbowLi.UnityGame {
 public enum EnemyContentOperation { 未配置,伤害,护盾,治疗,强壮,烧伤,坚固,铁壁,招架,流血,易伤,中毒,再生,弱化,填入数字,卡牌效果加成,行动后随机数字,后续自身行动重复,后续数字随机变化,全部数字随机填入,随机卡牌效果 }
 public enum EnemyContentTarget { 自身,对手,公共牌序 }
 public enum EnemyContentAmount { 固定值,基础攻击 }
 public enum EnemyContentNumber { 无,每次随机,当前卡牌数字,本场固定随机,任意数字 }
 public enum EnemyContentTrigger { 无,血量首次达到阈值,使用过指定技能 }
 public enum EnemyContentReward { 未配置,金币,卡牌,牌序效果,铭刻 }
 [Serializable] public sealed class EnemyAuthoredEffect {
  public EnemyContentOperation operation;
  public EnemyContentTarget target;
  public EnemyContentAmount amountSource;
  public float multiplier=1;
  public int amount=1,offset;
  public EnemyContentNumber numberMode;
  public int numberMin=1,numberMax=6;
  [UnityEngine.TextArea] public string notes;
  // No rounding policy is invented here: 1.5 * base attack remains a decimal for the resolver.
  public double EvaluateAmount(int baseAttack)=>Math.Max(0,(amountSource==EnemyContentAmount.基础攻击?baseAttack:amount)*(double)multiplier+offset);
 }
 public static class EnemyConfiguredContent {
  public static string[] Check(NormalEnemyRow row){
   var issues=new List<string>();
   if(!row.configured)issues.Add(row.title+"：尚未完成数值配置。");
   foreach(var s in row.skills??Array.Empty<EnemySkillContent>()){
    if(s==null)continue;CheckEffects(s.effects,row.title+"/"+s.name,issues);
    if(s.special&&s.trigger==EnemyContentTrigger.无)issues.Add(s.name+"：特殊技能缺少数值触发条件。");
    if(s.trigger==EnemyContentTrigger.使用过指定技能&&!(Array.Exists(row.skills,p=>p!=null&&p.name==s.requiredSkill&&!ReferenceEquals(p,s))))issues.Add(s.name+"：前置技能不存在或引用自身。");
   }
   foreach(var s in row.sequences??Array.Empty<EnemySequenceContent>())if(s!=null)CheckEffects(s.effects,row.title+"/"+s.name,issues);
   foreach(var r in row.rewards??Array.Empty<EnemyRewardContent>())if(r!=null){if(r.kind==EnemyContentReward.未配置)issues.Add(r.name+"：奖励类型未配置。");if(r.kind==EnemyContentReward.卡牌&&r.choiceCount<1)issues.Add(r.name+"：卡牌候选数至少为1。");}
   return issues.ToArray();
  }
  static void CheckEffects(EnemyAuthoredEffect[] effects,string name,List<string> issues){
   if(effects==null||effects.Length==0){issues.Add(name+"：缺少数值效果。");return;}
   foreach(var e in effects){
    if(e==null||e.operation==EnemyContentOperation.未配置){issues.Add(name+"：效果类型未配置。");continue;}
    if(float.IsNaN(e.multiplier)||float.IsInfinity(e.multiplier)||e.multiplier<=0||e.amount<0)issues.Add(name+"：数值倍率或数量无效。");
    if(e.numberMode!=EnemyContentNumber.无&&(e.numberMin<1||e.numberMax<e.numberMin||e.numberMax==int.MaxValue))issues.Add(name+"：数字生成范围无效。");
   }
  }
 }
}
