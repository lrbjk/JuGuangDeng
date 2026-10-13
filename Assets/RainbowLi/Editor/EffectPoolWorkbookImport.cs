using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using RainbowLi.UnityGame;
using UnityEditor;
using UnityEngine;

namespace RainbowLi.EditorTools {
 // All five sheets are parsed and validated before any asset is changed.
 public static class EffectPoolWorkbookImport {
  const string Root="Assets/RainbowLi/Data/";
  static T Load<T>(string name) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(Root+name)??throw new InvalidOperationException("找不到配置："+name);

  [MenuItem("RainbowLi/配表/导入五张效果与池权重表")]
  public static void Import(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("请在非运行状态导入配表。");
   var player=Load<PlayerCardTable>("DefaultPlayerCards.asset");
   var sequences=Load<SequenceEffectTable>("DefaultSequenceEffects.asset");
   var engravings=Load<EngravingEffectTable>("DefaultEngravingEffects.asset");
   var game=Load<GameDefinition>("DefaultGame.asset");
   var catalog=Load<CardEffectCatalog>("DefaultCardEffects.asset");
   var cards=CardTableWorkbook.Parse(CardTableWorkbook.Read(Root+"卡牌效果.xlsx"),player.rows);
   var sequenceKnown=sequences.rows.Concat(new[]{
    new EffectTableRow {id="牌序效果_24",behavior="IronWall"},
    new EffectTableRow {id="牌序效果_25",behavior="Regeneration"}
   }).GroupBy(r=>r.id).Select(g=>g.First()).ToArray();
   var rules=EffectTableWorkbook.Parse(CardTableWorkbook.Read(Root+"牌序效果.xlsx"),true,sequenceKnown);
   var marks=EffectTableWorkbook.Parse(CardTableWorkbook.Read(Root+"铭刻效果.xlsx"),false,engravings.rows);
   var cardPools=ReadPoolWeights(Root+"卡牌效果池权重表.xlsx");
   var sequencePools=ReadPoolWeights(Root+"卡序效果池权重表.xlsx");
   Validate(cards,rules,marks,cardPools,sequencePools);
   var definitions=catalog.effects.Where(d=>d!=null).ToDictionary(d=>d.effectId);
   foreach(var row in cards)if(!definitions.ContainsKey(row.id))throw new InvalidOperationException("卡效目录缺少定义："+row.id);

   Undo.RecordObjects(new UnityEngine.Object[]{player,sequences,engravings,game}.Concat(definitions.Values).ToArray(),"导入五张效果配表");
   player.rows=cards;sequences.rows=rules;engravings.rows=marks;
   game.cardEffectPools=cardPools;game.sequenceEffectPools=sequencePools;
   game.cardEffects=catalog;game.sequenceEffects=sequences;game.engravingEffects=engravings;
   // Empty selectors enable the first-stage pool lottery in every random entry.
   game.playerSequencePool=game.enemySequencePool=game.rewardSequencePool=game.shopSequencePool="";
   game.engravingPool="";
   if(game.player!=null){Undo.RecordObject(game.player,"绑定导入卡效表");game.player.cardTable=player;EditorUtility.SetDirty(game.player);}
   foreach(var row in cards){
    var d=definitions[row.id];d.displayName=row.name;d.description=row.description;d.sourceRange=row.range;d.pools=row.pools;
    d.effect=row.effect;d.lifetime=row.lifetime;d.polarity=row.polarity;d.usesCardNumber=row.usesNumber;
    d.includeInStartingDeck=row.starting;d.minValue=row.min;d.maxValue=row.max;d.weight=row.weight;EditorUtility.SetDirty(d);
   }
   foreach(var asset in new UnityEngine.Object[]{player,sequences,engravings,game})EditorUtility.SetDirty(asset);
   AssetDatabase.SaveAssets();
   Debug.Log("五表导入完成：卡效 "+cards.Length+"，卡效池 "+cardPools.Length+"，牌序 "+rules.Length+"，牌序池 "+sequencePools.Length+"，铭刻 "+marks.Length+"。重新开始游戏后使用新配置。");
  }

  public static EffectPoolWeight[] ReadPoolWeights(string path){
   var rows=CardTableWorkbook.Read(path,3);var result=new List<EffectPoolWeight>();
   var ids=new HashSet<string>();var names=new HashSet<string>();
   for(int i=0;i<rows.Length;i++){
    var row=rows[i];if(row.All(string.IsNullOrWhiteSpace))continue;
    if(row[0]=="编号"){if(row[1]!="名称"||row[2]!="权重")throw new FormatException(path+"：权重表头须为编号、名称、权重。");continue;}
    string id=(row[0]??"").Trim(),name=(row[1]??"").Trim();
    if(id==""||name==""||!ids.Add(id)||!names.Add(EffectPoolNames.Normalize(name)))throw new FormatException(path+" 第 "+(i+1)+" 行：池编号或名称为空/重复。");
    if(!float.TryParse(row[2],NumberStyles.Float,CultureInfo.InvariantCulture,out float weight)||weight<0||float.IsNaN(weight)||float.IsInfinity(weight))throw new FormatException(path+" 第 "+(i+1)+" 行：池权重须为非负有限数。");
    result.Add(new EffectPoolWeight {Id=id,Name=name,Weight=weight});
   }
   if(!result.Any(p=>p.Weight>0))throw new FormatException(path+"：至少需要一个正权重池。");
   return result.ToArray();
  }

  public static void Validate(CardTableRow[] cards,EffectTableRow[] rules,EffectTableRow[] marks,EffectPoolWeight[] cardPools,EffectPoolWeight[] sequencePools){
   var errors=CardTableValidation.Check(cards,false);if(errors.Length>0)throw new InvalidOperationException(string.Join("\n",errors.Select(e=>e.ToString())));
   var sequenceProbe=ScriptableObject.CreateInstance<SequenceEffectTable>();var engravingProbe=ScriptableObject.CreateInstance<EngravingEffectTable>();
   try {sequenceProbe.rows=rules;engravingProbe.rows=marks;sequenceProbe.ValidateTable();engravingProbe.ValidateTable();}
   finally {UnityEngine.Object.DestroyImmediate(sequenceProbe);UnityEngine.Object.DestroyImmediate(engravingProbe);}
   CheckMembership(cards.Where(r=>r.enabled).Select(r=>new KeyValuePair<string,string>(r.id,r.pools)),cardPools,"卡效");
   CheckMembership(rules.Where(r=>r.enabled).Select(r=>new KeyValuePair<string,string>(r.id,r.pools)),sequencePools,"牌序");
   foreach(var pool in cardPools.Where(p=>p.Weight>0))if(!cards.Any(r=>r.enabled&&r.starting&&EffectPoolNames.Contains(r.pools,pool.Name)))throw new InvalidOperationException(pool.Name+" 没有启用的起始牌效果。");
  }
  static void CheckMembership(IEnumerable<KeyValuePair<string,string>> source,EffectPoolWeight[] pools,string label){
   var rows=source.ToArray();var names=new HashSet<string>(pools.Select(p=>EffectPoolNames.Normalize(p.Name)));
   foreach(var row in rows){
    var memberships=(row.Value??"").Split(new[]{',','，','、'},StringSplitOptions.RemoveEmptyEntries).Select(EffectPoolNames.Normalize).ToArray();
    if(memberships.Length==0||memberships.Any(p=>!names.Contains(p)))throw new InvalidOperationException(label+" "+row.Key+" 的所属池在权重表中不存在。");
    if(!pools.Any(p=>p.Weight>0&&EffectPoolNames.Contains(row.Value,p.Name)))throw new InvalidOperationException(label+" "+row.Key+" 不属于任何正权重池。");
   }
   foreach(var pool in pools.Where(p=>p.Weight>0))if(!rows.Any(r=>EffectPoolNames.Contains(r.Value,pool.Name)))throw new InvalidOperationException(label+"池 "+pool.Name+" 没有启用效果。");
  }
 }
}
