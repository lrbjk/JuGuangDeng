using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class ReductionPercentVerification {
  private static int checks;
  private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
  private static int Resolve(CombatantState actor,CardEffect effect,int amount=20){
   actor.Health=actor.MaxHealth=100;actor.Shield=0;
   return effect==CardEffect.Weaken?CombatResolver.OutgoingDamage(actor,amount):CombatResolver.Damage(null,actor,amount).Damage;
  }
  public static void Run(){
   checks=0;string path="Logs/reduction-percent.xlsx";
   try{
    foreach(var effect in new[]{CardEffect.Weaken,CardEffect.Fortify}){
     var row=new CardTableRow{id=effect.ToString(),name=CombatStatuses.Name(effect),effect=effect,lifetime=EffectLifetime.Persistent,description=(effect==CardEffect.Weaken?"造成":"受到")+"的伤害减少25%，向下取整。（X为层数）",range="3",reductionPercent=50};
     var spec=row.EffectSpecs()[0];Check(spec.ReductionPercent==50&&spec.Min==3&&spec.Text.Contains("50%"),"Configured spec must separate percentage from layers and update text.");
     Check(spec.Copy().ReductionPercent==50,"Spec copy must preserve reduction.");
     var g=new ContainmentGame(23);typeof(ContainmentGame).GetMethod("ApplyEffect",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,new object[]{false,effect,3,spec});
     CombatantState actor=effect==CardEffect.Weaken?(CombatantState)g.Enemy:g.Player;
     Check(actor.Statuses[effect]==3&&actor.Statuses.ReductionPercentOf(effect)==50,"Card settlement must configure the correct actor.");
     Check(Resolve(actor,effect)==10,"50% reduction must turn 20 damage into 10.");
     actor.Statuses.EndRound();Check(actor.Statuses[effect]==2&&actor.Statuses.ReductionPercentOf(effect)==50,"End round must reduce layers without changing percentage.");
     var snapshot=new CombatStatuses();snapshot.CopyFrom(actor.Statuses);Check(snapshot.ReductionPercentOf(effect)==50,"Forecast status copy must preserve percentage.");
     var restored=ContainmentGame.RestoreSave(JsonUtility.FromJson<RunSave>(JsonUtility.ToJson(g.CaptureSave())));
     var restoredActor=effect==CardEffect.Weaken?(CombatantState)restored.Enemy:restored.Player;
     Check(restoredActor.Statuses.ReductionPercentOf(effect)==50&&Resolve(restoredActor,effect)==10,"Save/load must preserve actual arithmetic.");
     actor.Statuses.Add(effect,1,EffectLifetime.Persistent,null,0);Check(Resolve(actor,effect)==20,"0% must not reduce damage.");
     actor.Statuses.Add(effect,1,EffectLifetime.Persistent,null,100);Check(Resolve(actor,effect)==0,"100% must reduce damage to zero.");
     actor.Statuses.Add(effect,1,EffectLifetime.Persistent,null,25);Check(Resolve(actor,effect,3)==2,"Fractional results must retain floor rounding.");
     actor.Statuses[effect]=0;Check(actor.Statuses.ReductionPercentOf(effect)==25&&Resolve(actor,effect)==20,"Removing status must restore default configuration and normal damage.");
     actor.Statuses.Add(effect,2);Check(Resolve(actor,effect)==15,"Legacy unconfigured status must keep 25% reduction.");
     Check(BattleStatusPanelView.Explain(effect,3,"玩家","敌人",25,50).Contains("50%"),"Status details must show configured reduction.");
     if(File.Exists(path))File.Delete(path);CardTableWorkbook.Write(path,new[]{row});
     var cells=CardTableWorkbook.Read(path);Check(cells[0][19]=="减幅(%)"&&CardTableWorkbook.Parse(cells,Array.Empty<CardTableRow>()).Single().reductionPercent==50,"Excel round trip must preserve reduction.");
     var old=CardTableWorkbook.Cells(row).Take(19).ToArray();Check(CardTableWorkbook.Parse(new[]{old},Array.Empty<CardTableRow>()).Single().reductionPercent==25,"Old spreadsheets must default to 25%.");
     Check(CardTableWorkbook.Parse(new[]{old},new[]{row}).Single().reductionPercent==50,"Old merge imports must preserve configured reduction.");
     foreach(int invalid in new[]{-1,101}){row.reductionPercent=invalid;Check(CardTableValidation.Check(new[]{row},false).Any(e=>e.Field=="减幅(%)"),"Invalid reduction must be rejected.");}
    }
    Check(JsonUtility.FromJson<SavedStatus>("{\"Value\":3}").ReductionPercent==25&&JsonUtility.FromJson<CardEffectSpec>("{}").ReductionPercent==25&&JsonUtility.FromJson<CardTableRow>("{}").reductionPercent==25,"Old saved statuses, specs and rows must default to 25%.");
    var combined=new ContainmentGame(42);combined.Player.Statuses.Add(CardEffect.Weaken,2,null,null,50);combined.Enemy.Statuses.Add(CardEffect.Fortify,2,null,null,20);combined.Enemy.Health=100;combined.Enemy.MaxHealth=100;
    Check(CombatResolver.Damage(combined.Player,combined.Enemy,20).Damage==8,"Outgoing and incoming reduction must apply independently in original order.");
    combined.Player.Statuses.Add(CardEffect.Fortify,2,null,null,70);Check(combined.Player.Statuses.WeakenReductionPercent==50&&combined.Player.Statuses.FortifyReductionPercent==70,"Different status percentages must not overwrite each other.");
    ShieldPercentVerification.Run();
    Debug.Log($"REDUCTION PERCENT PASS: {checks} checks plus 49 shield/fixed-value regressions; Weaken, Fortify, boundary values, save/load, text and XLSX.");
   }finally{if(File.Exists(path))File.Delete(path);}
  }
 }
}
