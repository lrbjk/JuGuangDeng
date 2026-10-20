using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class UnifiedPercentVerification {
  private static int checks;
  private const BindingFlags Private=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
  private static object Call(object target,string method,params object[] args)=>typeof(ContainmentGame).GetMethod(method,Private).Invoke(target,args);
  private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
  private static ContainmentGame Game(GameSetup setup=null){var g=new ContainmentGame(37,setup);g.Player.Health=g.Player.MaxHealth=g.Enemy.Health=g.Enemy.MaxHealth=200;return g;}
  public static void Run(){
   checks=0;CardRoutes();SequenceRoutes();EnemyRoutes();ReductionPercentVerification.Run();
   Debug.Log($"UNIFIED PERCENT PASS: {checks} checks plus 88 regression checks; compound cards, independent assets, sequence pools/secondary/equip/save, normal enemies, bosses/passives and Excel.");
  }
  private static void CardRoutes(){
   var definition=ScriptableObject.CreateInstance<CardEffectDefinition>();var table=ScriptableObject.CreateInstance<PlayerCardTable>();
   string path="Logs/unified-compound.xlsx";
   try{
    definition.effect=CardEffect.IronWall;definition.effectId="independent";definition.displayName="铁壁";definition.description="获得的护盾量增加25%";definition.shieldBonusPercent=55;
    Check(definition.CreateSpec().ShieldBonusPercent==55&&definition.CreateSpec().Text.Contains("55%"),"Independent asset must configure its runtime spec and text.");
    Check(CardTableRow.From(definition).shieldBonusPercent==55,"Asset to table conversion must preserve percentage.");
    var row=new CardTableRow{id="compound_test",name="复合卡",effect=CardEffect.Guard,description="护盾 X",range="2",additionalEffects=new[]{
     new CardTableEffect{effect=CardEffect.IronWall,description="获得的护盾量增加25%",range="3",lifetime=EffectLifetime.Persistent,shieldBonusPercent=55},
     new CardTableEffect{effect=CardEffect.Weaken,description="造成的伤害减少25%",range="3",lifetime=EffectLifetime.Persistent,reductionPercent=40},
     new CardTableEffect{effect=CardEffect.Fortify,description="受到的伤害减少25%",range="3",lifetime=EffectLifetime.Persistent,reductionPercent=60}}};
    var specs=row.EffectSpecs();Check(specs[1].ShieldBonusPercent==55&&specs[2].ReductionPercent==40&&specs[3].ReductionPercent==60,"Compound specs must preserve independent percentages.");
    Check(specs[1].Text.Contains("55%")&&specs[2].Text.Contains("40%")&&specs[3].Text.Contains("60%"),"Compound effect text must show configured percentages.");
    var card=new CardEffectGroup{Specs=specs}.CreateCard(3);var g=Game();Call(g,"ApplyCardEffect",new BattleAction{Card=card,Number=3,Effect=card.Effect});
    Check(g.Player.Statuses.IronWallBonusPercent==55&&g.Enemy.Statuses.WeakenReductionPercent==40&&g.Player.Statuses.FortifyReductionPercent==60,"Compound card settlement must use each effect's percentage and target.");
    Check(card.RulesText.Contains("55%")&&card.RulesText.Contains("60%"),"Compound card details must show percentages.");
    if(File.Exists(path))File.Delete(path);CardTableWorkbook.Write(path,new[]{row});
    var compound=CardTableWorkbook.ReadSheet(path,"复合效果",10);Check(compound[1][8]=="55"&&compound[2][9]=="40","Compound Excel must export percentage columns.");
    table.rows=new[]{row.Copy()};var plan=new CardTableImportPlan(table,CardTableWorkbook.Read(path),false,Array.Empty<CardTableRow>(),compound);
    Check(plan.Errors.Count==0&&plan.Changes.Single().After.additionalEffects[2].reductionPercent==60,"Compound XLSX import must preserve percentages.");
    var legacy=compound.Select(c=>c.Take(8).ToArray()).ToArray();var oldPlan=new CardTableImportPlan(table,new[]{CardTableWorkbook.Cells(row)},false,Array.Empty<CardTableRow>(),legacy);
    Check(oldPlan.Errors.Count==0&&oldPlan.Changes.Single().After.additionalEffects[0].shieldBonusPercent==55,"Legacy compound merge must retain configured percentages.");
    row.additionalEffects[1].reductionPercent=101;Check(CardTableValidation.Check(new[]{row},false).Any(e=>e.Field.Contains("减幅")),"Invalid compound percentages must be rejected.");
    Check(JsonUtility.FromJson<CardTableEffect>("{}").shieldBonusPercent==25&&definition.reductionPercent==25,"Legacy additional effects and standalone assets must default to 25%.");
   }finally{if(File.Exists(path))File.Delete(path);UnityEngine.Object.DestroyImmediate(definition);UnityEngine.Object.DestroyImmediate(table);}
  }
  private static void SequenceRoutes(){
   var table=ScriptableObject.CreateInstance<SequenceEffectTable>();var definition=ScriptableObject.CreateInstance<SequenceDefinition>();
   string path="Logs/unified-sequence.xlsx";
   try{
    var first=new EffectTableRow{id="iron55",name="铁壁X",description="护盾增加25%",behavior="IronWall",range="3",pools="默认池",shieldBonusPercent=55};var second=first.Copy();second.id="iron80";second.shieldBonusPercent=80;table.rows=new[]{first,second};table.ValidateTable();
    definition.effectTable=table;definition.primaryEffectId=first.id;definition.secondaryEffectId=second.id;definition.secondEffectEnabled=true;var rule=definition.CreateRule();
    Check(rule.ShieldBonusPercent==55&&rule.AdditionalEffect.ShieldBonusPercent==80,"Primary and secondary sequence effects must keep separate percentages.");
    Check(rule.Description.Contains("55%")&&rule.Description.Contains("80%"),"Sequence descriptions must include both percentages.");
    var setup=new GameSetup();setup.PlayerRules.Add(rule);setup.SequencePool.Add(RuntimeTableAdapter.Sequence(first));var g=Game(setup);
    Check(g.PlayerRules[0].ShieldBonusPercent==55&&g.PlayerRules[0].AdditionalEffect.ShieldBonusPercent==80,"Game rule copies must preserve sequence percentages.");
    Call(g,"ApplyAdvancedRule",new BattleAction{Rule=g.PlayerRules[0]});Check(g.Player.Statuses.IronWallBonusPercent==55,"Sequence settlement must apply configured shield bonus.");
    g.NumberEntries.Clear();for(int n=1;n<=3;n++)g.NumberEntries.Add(new NumberEntry{Number=n,Action=new BattleAction{Number=n}});
    var matches=new List<BattleAction>();Call(g,"Match",rule,false,matches,false);var secondary=matches.Single(a=>a.Rule.Name.EndsWith("效果2"));Call(g,"ApplyAdvancedRule",secondary);
    Check(g.Player.Statuses.IronWallBonusPercent==80,"Actual secondary sequence matching must retain and apply its percentage.");
    var drawn=(SequenceEffect)Call(g,"DrawSequenceEffect","默认池");Check(drawn.ShieldBonusPercent==55,"Random sequence pool must preserve percentage.");
    var equipped=new SequenceRule{Numbers=new[]{1,2,3}};Call(null,"SetRuleEffect",equipped,drawn);Check(equipped.ShieldBonusPercent==55,"Equipping a sequence effect must preserve percentage.");
    var copy=(SequenceEffect)Call(null,"CopyEffect",drawn);Check(copy.ShieldBonusPercent==55,"Effect copies must preserve percentage.");
    var restored=ContainmentGame.RestoreSave(JsonUtility.FromJson<RunSave>(JsonUtility.ToJson(g.CaptureSave())),setup);
    Check(restored.PlayerRules[0].ShieldBonusPercent==55&&restored.PlayerRules[0].AdditionalEffect.ShieldBonusPercent==80,"Sequence save/load must preserve both effects.");
    if(File.Exists(path))File.Delete(path);EffectTableWorkbook.Write(path,table);var rows=EffectTableWorkbook.Parse(CardTableWorkbook.Read(path),true,Array.Empty<EffectTableRow>());
    Check(rows[0].shieldBonusPercent==55&&rows[1].shieldBonusPercent==80,"Sequence Excel round trip must preserve percentages.");
    var legacy=EffectTableWorkbook.Cells(first,true).Take(11).ToArray();Check(EffectTableWorkbook.Parse(new[]{legacy},true,new[]{first}).Single().shieldBonusPercent==55,"Legacy sequence imports must retain configured percentages.");
    definition.effectTable=null;definition.effect=RuleEffect.IronWall;definition.shieldBonusPercent=35;definition.secondEffectEnabled=false;Check(definition.CreateRule().ShieldBonusPercent==35,"Manual sequence definitions must support shield percentage.");
    Check(JsonUtility.FromJson<SequenceEffect>("{}").ShieldBonusPercent==25&&JsonUtility.FromJson<SequenceRule>("{}").ShieldBonusPercent==25,"Old sequence saves must default to 25%.");
   }finally{if(File.Exists(path))File.Delete(path);UnityEngine.Object.DestroyImmediate(table);UnityEngine.Object.DestroyImmediate(definition);}
  }
  private static EnemyAuthoredEffect[] Authored()=>new[]{
   new EnemyAuthoredEffect{operation=EnemyContentOperation.铁壁,amount=2,shieldBonusPercent=60},
   new EnemyAuthoredEffect{operation=EnemyContentOperation.坚固,amount=2,reductionPercent=40},
   new EnemyAuthoredEffect{operation=EnemyContentOperation.弱化,target=EnemyContentTarget.对手,amount=2,reductionPercent=30}};
  private static void EnemyRoutes(){
   string normalPath="Logs/unified-normal.xlsx",bossPath="Logs/unified-boss.xlsx";
   try{
    var normal=AssetDatabase.LoadAssetAtPath<NormalEnemyTable>("Assets/RainbowLi/Data/DefaultNormalEnemies.asset").rows.First(r=>r.configured).Copy();
    normal.skills[0].effects=Authored();normal.sequences[0].effects=Authored();var converted=EnemyEncounterAdapter.Normal(normal);
    Check(converted.Skills[0].Effects[0].ShieldBonusPercent==60&&converted.Skills[0].Effects[1].ReductionPercent==40,"Normal enemy skill adapter must preserve percentages.");
    Check(converted.Sequences[0].Effects[2].ReductionPercent==30,"Enemy sequences must preserve reduction.");
    Check(converted.Skills[0].Description.Contains("60%"),"Enemy skill description must show configured percentage.");
    var g=Game();g.Enemy.Encounter=new EnemyEncounterState{Definition=converted};Call(g,"ApplyEncounterEffects",new BattleAction{Enemy=true},converted.Skills[0].Effects,BattleEventSource.EnemySkill);
    Check(g.Enemy.Statuses.IronWallBonusPercent==60&&g.Enemy.Statuses.FortifyReductionPercent==40&&g.Player.Statuses.WeakenReductionPercent==30,"Enemy skills must settle all configured percentages on correct actors.");
    Check(CombatResolver.GainShield(g.Enemy,100)==160,"Enemy IronWall must use configured shield arithmetic.");g.Enemy.Shield=0;
    Check(CombatResolver.Damage(g.Player,g.Enemy,100).Damage==42,"Authored weakness and fortify must use independent configured reductions.");
    if(File.Exists(normalPath))File.Delete(normalPath);NormalEnemyWorkbook.Write(normalPath,new[]{normal});var normalBack=NormalEnemyWorkbook.Read(normalPath).Single();
    Check(normalBack.skills[0].effects[0].shieldBonusPercent==60&&normalBack.sequences[0].effects[2].reductionPercent==30,"Normal enemy Excel configuration must preserve percentages.");
    var boss=AssetDatabase.LoadAssetAtPath<BossContentTable>("Assets/RainbowLi/Data/DefaultBosses.asset").rows.First(r=>r.content.configured).Copy();boss.content.skills[0].effects=Authored();boss.content.sequences[0].effects=Authored();boss.phases[0].passives=new[]{new BossPassive{timing=BossTiming.回合开始,effects=Authored()}};
    var bossDefinition=EnemyEncounterAdapter.Boss(boss);Check(bossDefinition.Phases[0].Passives[0].Effects[0].ShieldBonusPercent==60,"Boss passive adapter must preserve percentages.");
    var bossGame=Game();bossGame.Enemy.Encounter=new EnemyEncounterState{Definition=bossDefinition};Call(bossGame,"RunContentPassives",EncounterTiming.RoundStart,null);
    Check(bossGame.Enemy.Statuses.IronWallBonusPercent==60&&bossGame.Enemy.Statuses.FortifyReductionPercent==40&&bossGame.Player.Statuses.WeakenReductionPercent==30,"Actual boss round-start passives must apply percentages.");
    var restored=ContainmentGame.RestoreSave(JsonUtility.FromJson<RunSave>(JsonUtility.ToJson(bossGame.CaptureSave())));
    Check(restored.Enemy.Encounter.Definition.Phases[0].Passives[0].Effects[0].ShieldBonusPercent==60,"Encounter save/load must preserve passive parameters.");
    if(File.Exists(bossPath))File.Delete(bossPath);BossContentWorkbook.Write(bossPath,new[]{boss});var bossBack=BossContentWorkbook.Read(bossPath).Single();
    Check(bossBack.phases[0].passives[0].effects[0].shieldBonusPercent==60&&bossBack.content.skills[0].effects[1].reductionPercent==40,"Boss Excel configuration must preserve skills and passive percentages.");
    Check(new EnemyAuthoredEffect().shieldBonusPercent==25&&JsonUtility.FromJson<EncounterEffect>("{}").ReductionPercent==25,"Legacy authored and encounter effects must keep defaults.");
    var issues=new List<string>();var invalid=Authored();invalid[1].reductionPercent=101;EnemyConfiguredContent.CheckEffects(invalid,"测试",issues);Check(issues.Any(s=>s.Contains("减幅")),"Authored invalid percentages must be rejected.");
   }finally{if(File.Exists(normalPath))File.Delete(normalPath);if(File.Exists(bossPath))File.Delete(bossPath);}
  }
 }
}
