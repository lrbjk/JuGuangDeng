using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RainbowLi;
using RainbowLi.UnityGame;
using RainbowLi.EditorTools;
using UnityEditor;
using UnityEngine;

public static class EffectPoolImportChecks {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static Dictionary<string,double> Expected(IEnumerable<EffectPoolEntry> source,IEnumerable<EffectPoolWeight> weights){
  var entries=source.ToArray();var pools=weights.ToArray();var result=entries.ToDictionary(e=>e.Code,e=>0d);
  if(pools.Length==0){double total=entries.Sum(e=>(double)e.Weight);foreach(var e in entries)result[e.Code]=e.Weight/total;return result;}
  double poolTotal=pools.Sum(p=>(double)p.Weight);
  foreach(var pool in pools){var candidates=entries.Where(e=>e.InPool(pool.Name)).ToArray();double total=candidates.Sum(e=>(double)e.Weight);foreach(var e in candidates)result[e.Code]+=pool.Weight/poolTotal*e.Weight/total;}
  Check(Math.Abs(result.Values.Sum()-1)<1e-9,"expected probabilities sum to one");return result;
 }
 static void CheckDistribution(Dictionary<string,int> counts,Dictionary<string,double> expected,int n,string label){
  foreach(var item in expected)Check(Math.Abs(counts[item.Key]/(double)n-item.Value)<.012,label+" probability mismatch "+item.Key);
 }
 public static void Run(){try{
  EffectPoolWorkbookImport.Import();
  AssetDatabase.Refresh();
  var definition=AssetDatabase.LoadAssetAtPath<GameDefinition>("Assets/RainbowLi/Data/DefaultGame.asset");var s=definition.CreateSetup();
  Check(s.CardEffects.Count==26&&s.SequencePool.Count==25&&s.EngravingPool.Count==19,"imported effect counts");
  Check(s.CardEffectPools.Count==7&&s.SequenceEffectPools.Count==6,"imported pool counts");
  Check(s.CardEffectPools.Select(p=>p.Weight).SequenceEqual(new float[]{80,50,40,40,20,40,40}),"card pool weights");
  Check(s.SequenceEffectPools.Select(p=>p.Weight).SequenceEqual(new float[]{80,50,40,40,20,30}),"sequence pool weights");
  Check(s.CardEffects.First(c=>c.Code=="卡效_01").Weight==50&&s.CardEffects.First(c=>c.Code=="卡效_20").Weight==35,"card row weights by id, including reordered burn row");
  Check(s.SequencePool.Single(c=>c.Code=="牌序效果_24").Sequence==RuleEffect.IronWall&&s.SequencePool.Single(c=>c.Code=="牌序效果_25").Sequence==RuleEffect.Regeneration,"new sequence dispatch");
  Check(s.SequencePool.Single(c=>c.Code=="牌序效果_16").Multiplier==2,"doubled effect preserves runtime multiplier");
  Check(string.IsNullOrEmpty(s.RewardSequencePool)&&string.IsNullOrEmpty(s.ShopSequencePool)&&string.IsNullOrEmpty(s.EngravingPoolName),"entry selectors use weighted pools and engraving mixed pool");
  var raw=CardTableWorkbook.Read("Assets/RainbowLi/Data/卡牌效果.xlsx");var roundTrip=CardTableWorkbook.Parse(raw,definition.player.cardTable.rows);
  Check(roundTrip.Select(r=>string.Join("|",CardTableWorkbook.Cells(r))).SequenceEqual(definition.player.cardTable.rows.Select(r=>string.Join("|",CardTableWorkbook.Cells(r)))),"new card layout reimport preserves membership and weight");
  foreach(var pair in new[]{Tuple.Create("牌序效果.xlsx",(EffectTableDefinition)definition.sequenceEffects),Tuple.Create("铭刻效果.xlsx",(EffectTableDefinition)definition.engravingEffects)}){
   var parsed=EffectTableWorkbook.Parse(CardTableWorkbook.Read("Assets/RainbowLi/Data/"+pair.Item1),pair.Item2.IsSequence,pair.Item2.rows);
   Check(parsed.Select(r=>string.Join("|",EffectTableWorkbook.Cells(r,pair.Item2.IsSequence))).SequenceEqual(pair.Item2.rows.Select(r=>string.Join("|",EffectTableWorkbook.Cells(r,pair.Item2.IsSequence)))),"effect layout reimport "+pair.Item1);
  }
  // Editor export rows and the legacy seven-column sheet still round-trip.
  var exportRows=new[]{CardTableWorkbook.Headers}.Concat(definition.player.cardTable.rows.Select(CardTableWorkbook.Cells));
  Check(CardTableWorkbook.Parse(exportRows,roundTrip).All(r=>r.pools!=null&&r.weight>0),"editor export retains membership");
  var legacy=roundTrip.Select(r=>new[]{r.id,r.name,CardTableWorkbook.Lifetimes[(int)r.lifetime],CardTableWorkbook.Polarities[(int)r.polarity],r.description,r.range,r.notes}).ToArray();
  Check(CardTableWorkbook.Parse(legacy,roundTrip).Select(r=>r.weight).SequenceEqual(roundTrip.Select(r=>r.weight)),"legacy imports preserve existing weights");
  var preview=new CardTableImportPlan(definition.player.cardTable,raw,false,roundTrip);Check(preview.Errors.Count==0&&preview.Changes.All(c=>c.Kind=="不变"),"new-layout import preview");
  var game=new ContainmentGame(20261013,s);const int n=40000;
  var cardCounts=s.CardEffects.ToDictionary(e=>e.Code,e=>0);var ruleCounts=s.SequencePool.ToDictionary(e=>e.Code,e=>0);var markCounts=s.EngravingPool.ToDictionary(e=>e.Code,e=>0);
  for(int i=0;i<n;i++){cardCounts[game.GenerateCard().Spec.Code]++;ruleCounts[game.DrawSequenceEffect().Code]++;markCounts[game.DrawEngraving().Code]++;}
  var cardEntries=s.CardEffects.Select(e=>new EffectPoolEntry {Code=e.Code,Pools=e.Pools,Weight=e.Weight});
  CheckDistribution(cardCounts,Expected(cardEntries,s.CardEffectPools),n,"cards");CheckDistribution(ruleCounts,Expected(s.SequencePool,s.SequenceEffectPools),n,"sequences");CheckDistribution(markCounts,Expected(s.EngravingPool,Array.Empty<EffectPoolWeight>()),n,"engravings");
  var save=JsonUtility.FromJson<RunSave>(JsonUtility.ToJson(game.CaptureSave()));var restored=ContainmentGame.RestoreSave(save,s);
  for(int i=0;i<100;i++){Check(game.GenerateCard().Spec.Code==restored.GenerateCard().Spec.Code,"native JSON card RNG continuation");Check(game.DrawSequenceEffect().Code==restored.DrawSequenceEffect().Code,"native JSON sequence RNG continuation");Check(game.DrawEngraving().Code==restored.DrawEngraving().Code,"native JSON engraving RNG continuation");}
  EffectPoolRuntimeChecks.Main();
  File.WriteAllText("effect-pool-result.txt","PASS: Unity XLSX import, 26 card effects / 7 pools, 25 sequence effects / 6 pools, 19 engravings, source and legacy layout, preview, 120000 actual-table draws, native JSON continuation, 90000 fixture draws.");
  EditorApplication.Exit(0);
 }catch(Exception error){Debug.LogException(error);File.WriteAllText("effect-pool-result.txt",error.ToString());EditorApplication.Exit(1);}}
}
