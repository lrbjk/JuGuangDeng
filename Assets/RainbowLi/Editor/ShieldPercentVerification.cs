using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class ShieldPercentVerification {
  private static int checks;
  private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
  public static void Run(){
   checks=0;string path="Logs/shield-percent.xlsx";
   try{
    var row=new CardTableRow{id="ironwall_test",name="铁壁",effect=CardEffect.IronWall,lifetime=EffectLifetime.Persistent,description="获得的护盾量增加25%。（X为效果层数，每个回合结束时减少一层。）",range="3",shieldBonusPercent=50};
    var spec=row.EffectSpecs()[0];Check(spec.Min==3&&spec.ShieldBonusPercent==50,"Layers and percentage must remain independent.");
    Check(spec.Text.Contains("50%")&&!spec.Text.Contains("25%"),"Card description must reflect percentage.");
    Check(spec.Copy().ShieldBonusPercent==50,"Spec copies must retain percentage.");
    var g=new ContainmentGame(17);typeof(ContainmentGame).GetMethod("ApplyEffect",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,new object[]{false,CardEffect.IronWall,3,spec});
    Check(g.Player.Statuses[CardEffect.IronWall]==3&&g.Player.Statuses.IronWallBonusPercent==50,"Card settlement must apply configured percentage.");
    Check(CombatResolver.GainShield(g.Player,20)==30,"50% bonus must turn 20 shield into 30.");
    g.Player.Statuses.EndRound();Check(g.Player.Statuses[CardEffect.IronWall]==2&&g.Player.Statuses.IronWallBonusPercent==50,"End round must decay layers, not percentage.");
    var copy=new CombatStatuses();copy.CopyFrom(g.Player.Statuses);Check(copy.IronWallBonusPercent==50,"Forecast snapshots must preserve percentage.");
    var save=g.CaptureSave();var saved=save.Player.Statuses.Single(s=>s.Effect==CardEffect.IronWall);Check(saved.ShieldBonusPercent==50,"Save snapshot must retain percentage.");
    var restored=ContainmentGame.RestoreSave(JsonUtility.FromJson<RunSave>(JsonUtility.ToJson(save)));
    Check(restored.Player.Statuses.IronWallBonusPercent==50&&CombatResolver.GainShield(restored.Player,20)==30,"Save/load must preserve shield arithmetic.");
    Check(JsonUtility.FromJson<SavedStatus>("{\"Value\":3}").ShieldBonusPercent==25,"Old status saves must default to 25%.");
    Check(JsonUtility.FromJson<CardEffectSpec>("{\"Min\":3}").ShieldBonusPercent==25,"Old saved card specs must default to 25%.");
    Check(JsonUtility.FromJson<CardTableRow>("{\"range\":\"3\"}").shieldBonusPercent==25,"Old table rows must default to 25%.");
    g.Player.Statuses.Add(CardEffect.IronWall,1,EffectLifetime.Persistent,0);Check(CombatResolver.GainShield(g.Player,20)==20,"Zero percent must be allowed.");
    g.Player.Statuses.Add(CardEffect.IronWall,1,EffectLifetime.Persistent,75);Check(g.Player.Statuses.IronWallBonusPercent==75,"Reapplying must use the latest percentage without adding percentages.");
    g.Player.Statuses.Clear();g.Player.Statuses.Add(CardEffect.IronWall,2);Check(CombatResolver.GainShield(g.Player,20)==25,"Unconfigured effects must keep legacy 25%.");
    Check(CombatResolver.GainShield(g.Player,3)==3,"Shield bonus must retain integer floor rounding.");
    g.Player.Statuses[CardEffect.IronWall]=0;Check(CombatResolver.GainShield(g.Player,20)==20&&g.Player.Statuses.IronWallBonusPercent==25,"Removing status must reset percentage.");
    if(File.Exists(path))File.Delete(path);CardTableWorkbook.Write(path,new[]{row});
    var cells=CardTableWorkbook.Read(path);Check(cells[0][18]=="护盾增幅(%)","Excel must export percentage column.");
    Check(CardTableWorkbook.Parse(cells,Array.Empty<CardTableRow>()).Single().shieldBonusPercent==50,"XLSX round trip must preserve percentage.");
    var old=CardTableWorkbook.Cells(row).Take(18).ToArray();Check(CardTableWorkbook.Parse(new[]{old},Array.Empty<CardTableRow>()).Single().shieldBonusPercent==25,"Old spreadsheets must retain default 25%.");
    Check(CardTableWorkbook.Parse(new[]{old},new[]{row}).Single().shieldBonusPercent==50,"Old merge imports must preserve configured percentage.");
    Check(BattleStatusPanelView.Explain(CardEffect.IronWall,3,"玩家","敌人",50).Contains("50%"),"Status tooltip must show actual percentage.");
    row.shieldBonusPercent=-1;Check(CardTableValidation.Check(new[]{row},false).Any(e=>e.Field=="护盾增幅(%)"),"Invalid percentages must be rejected.");
    CardSpecificValueVerification.Run();
    Debug.Log($"SHIELD PERCENT PASS: {checks} checks plus fixed-value regression; settlement, status decay, text, save/load and XLSX.");
   }finally{if(File.Exists(path))File.Delete(path);}
  }
 }
}
