using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class CardSpecificValueVerification {
  private static int checks;
  private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
  private static CardTableRow Row()=>new CardTableRow{id="fixed_test",name="测试卡",description="伤害 X",range="2-8",effect=CardEffect.Strike};
  public static void Run(){
   checks=0;string path=Path.Combine("Logs","card-specific-value.xlsx");
   var player=ScriptableObject.CreateInstance<PlayerCardTable>();var enemy=ScriptableObject.CreateInstance<EnemyCardTable>();
   var definition=ScriptableObject.CreateInstance<CardDefinition>();
   try{
    var row=Row();var original=row.EffectSpecs()[0];Check(original.Min==2&&original.Max==8&&!original.UsesNumber,"Old range must remain unchanged by default.");
    row.range="为卡牌对应数字";Check(row.EffectSpecs()[0].UsesNumber,"Old number-dependent effects must remain unchanged.");
    row.fixedValue=10;var fixedSpec=row.EffectSpecs()[0];Check(!fixedSpec.UsesNumber&&fixedSpec.Min==10&&fixedSpec.Max==10,"Specific value must override number-dependent values.");
    row.range="";Check(row.EffectSpecs()[0].Min==10&&CardTableValidation.Check(new[]{row},false).Length==0,"Fixed-only rows must not require a range.");
    row.additionalEffects=new[]{new CardTableEffect{effect=CardEffect.Guard,range="3-5"}};
    Check(row.EffectSpecs()[1].Min==3&&row.EffectSpecs()[1].Max==5,"Specific value must not overwrite additional effects.");
    player.rows=new[]{row};enemy.rows=new[]{row.Copy()};var setup=new GameSetup{RandomStartingDeck=true};player.Apply(setup);enemy.Apply(setup);
    Check(setup.CardEffectGroups[0].Primary.Min==10&&setup.EnemySkills[0].Spec.Min==10,"Player pool and enemy skills must use fixed values.");
    var random=new System.Random(42);for(int number=1;number<=6;number++){
     var group=new CardEffectGroup{Specs=row.EffectSpecs()};var card=group.CreateCard(number,random);
     Check(card.Effects[0].ResolveValue(number,0)==10,"Rolled card must retain the fixed effect value.");
    }
    definition.cardTable=player;definition.rowId=row.id;
    Check(definition.CreateCard().Effects[0].BaseX==10,"Table-bound cards must use fixed values.");
    var serialized=JsonUtility.ToJson(row);Check(JsonUtility.FromJson<CardTableRow>(serialized).fixedValue==10&&row.Copy().fixedValue==10,"Copy and serialization must preserve specific values.");
    if(File.Exists(path))File.Delete(path);CardTableWorkbook.Write(path,new[]{row});
    var cells=CardTableWorkbook.Read(path);Check(cells[0][17]=="具体数值"&&cells[1][17]=="10","XLSX must export the new column.");
    var imported=CardTableWorkbook.Parse(cells,Array.Empty<CardTableRow>()).Single();
    Check(imported.fixedValue==10&&imported.EffectSpecs()[0].Min==10,"XLSX round trip must preserve fixed-only rows.");
    row.range="2-8";var oldCells=CardTableWorkbook.Cells(row).Take(17).ToArray();
    Check(CardTableWorkbook.Parse(new[]{oldCells},new[]{row}).Single().fixedValue==10,"Old imports must preserve existing specific values.");
    Check(CardTableWorkbook.Parse(new[]{oldCells},Array.Empty<CardTableRow>()).Single().fixedValue==0,"Old new rows must default to range mode.");
    var reset=CardTableWorkbook.Cells(row);reset[17]="0";
    var resetRow=CardTableWorkbook.Parse(new[]{reset},new[]{row}).Single();Check(resetRow.fixedValue==0&&resetRow.EffectSpecs()[0].Max==8,"Explicit zero must restore range mode.");
    var designHeader=new[]{"效果编号","效果名称","效果类型","效果类型","具体效果","基础取值范围（存在X时）","卡效所属池","权重","备注","具体数值"};
    var design=new[]{row.id,row.name,CardTableWorkbook.Lifetimes[0],CardTableWorkbook.Polarities[0],row.description,row.range,"测试池","10","","12"};
    Check(CardTableWorkbook.Parse(new[]{designHeader,design},new[]{row}).Single().fixedValue==12,"Design sheets must support the new column.");
    var change=CardTableWorkbook.Cells(row);change[17]="25";var plan=new CardTableImportPlan(player,new[]{change},false,Array.Empty<CardTableRow>());
    Check(plan.Errors.Count==0&&plan.Changes.Single().Details.Contains("具体数值"),"Import preview must describe fixed-value changes.");
    plan.Apply();Check(player.rows[0].fixedValue==25,"Applying import preview must preserve fixed values.");
    foreach(int bad in new[]{-1,10001}){row.fixedValue=bad;Check(CardTableValidation.Check(new[]{row},false).Any(e=>e.Field=="具体数值"),"Invalid fixed values must be rejected.");}
    foreach(string asset in new[]{"DefaultPlayerCards","DefaultEnemyCards"}){
     var table=AssetDatabase.LoadAssetAtPath<CardTableDefinition>("Assets/RainbowLi/Data/"+asset+".asset");
     Check(table!=null&&table.rows.All(r=>r.fixedValue==0),"Existing assets must remain in range mode.");table.ValidateTable(table is EnemyCardTable);
    }
    Debug.Log($"CARD SPECIFIC VALUE PASS: {checks} checks; runtime, compatibility, XLSX, preview, serialization and validation.");
   }finally{
    if(File.Exists(path))File.Delete(path);
    UnityEngine.Object.DestroyImmediate(player);UnityEngine.Object.DestroyImmediate(enemy);UnityEngine.Object.DestroyImmediate(definition);
   }
  }
 }
}
