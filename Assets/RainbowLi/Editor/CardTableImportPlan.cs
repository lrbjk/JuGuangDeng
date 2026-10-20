using System;
using System.Collections.Generic;
using System.Linq;
using RainbowLi.UnityGame;
using UnityEditor;
using UnityEngine;
namespace RainbowLi.EditorTools {
 public sealed class CardTableDiff {
  public string Id,Kind,Details;public CardTableRow Before,After;public bool Selected;
 }
 // Preview holds copies; it never changes the asset until Apply succeeds.
 public sealed class CardTableImportPlan {
  public readonly CardTableDefinition Target;
  public readonly List<string> Errors=new List<string>();
  public readonly List<CardTableDiff> Changes=new List<CardTableDiff>();
  public readonly bool Replace;
  public event Action Applied;
  readonly string snapshot;readonly CardTableRow[] before;readonly bool enemy;
  public CardTableImportPlan(CardTableDefinition target,string[][] cells,bool replace,IEnumerable<CardTableRow> known,string[][] compound=null){
   cells=CardTableWorkbook.CanonicalRows(cells);
   Target=target;Replace=replace;enemy=target is EnemyCardTable;snapshot=EditorJsonUtility.ToJson(target);before=(target.rows??Array.Empty<CardTableRow>()).Select(r=>r?.Copy()).ToArray();
   var imported=new List<CardTableRow>();var lines=new List<int>();var catalog=before.Concat(known??Array.Empty<CardTableRow>()).Where(r=>r!=null).ToArray();
   for(int i=0;i<cells.Length;i++){
    var row=cells[i];if(row==null||row.All(string.IsNullOrWhiteSpace)||row.FirstOrDefault()=="效果编号")continue;
    try{imported.Add(CardTableWorkbook.Parse(new[]{row},catalog).Single());lines.Add(i+1);}catch(FormatException e){Errors.Add("主表 · 第 "+(i+1)+" 行："+e.Message.Replace("第 1 行：",""));}
   }
   if(imported.Count==0)Errors.Add("主表：没有可导入的数据行。");
   if(compound!=null)ReadCompound(imported,compound);
   foreach(var issue in CardTableValidation.Check(imported.ToArray(),enemy)){
    // Merge may inherit its required enabled/start/basic pool from rows outside the import.
    if(issue.Row==0)continue;
    Errors.Add("主表 · "+(issue.Row>0?"第 "+lines[issue.Row-1]+" 行 · ":"")+issue.Field+"："+System.Text.RegularExpressions.Regex.Replace(issue.Message,@"^与第 (\d+) 行重复",m=>"与第 "+lines[int.Parse(m.Groups[1].Value)-1]+" 行重复"));
   }
   var unique=imported.GroupBy(r=>r.id??"",StringComparer.Ordinal).Select(g=>g.First()).ToArray();
   var existing=before.Where(r=>r!=null&&!string.IsNullOrWhiteSpace(r.id)).GroupBy(r=>r.id).ToDictionary(g=>g.Key,g=>g.First());
   foreach(var row in unique){existing.TryGetValue(row.id??"",out var old);bool changed=old==null||JsonUtility.ToJson(old)!=JsonUtility.ToJson(row);Changes.Add(new CardTableDiff{Id=row.id,Kind=old==null?"新增":changed?"修改":"不变",Before=old?.Copy(),After=row.Copy(),Selected=changed,Details=old==null?"新增完整行":Describe(old,row)});}
   if(replace)foreach(var old in before.Where(r=>r!=null&&!unique.Any(n=>n.id==r.id)))Changes.Add(new CardTableDiff{Id=old.id,Kind="删除",Before=old.Copy(),Selected=false,Details="源表缺少此行；默认保留，勾选才删除。"});
  }
  void ReadCompound(List<CardTableRow> rows,string[][] cells){
   var previousExtras=rows.GroupBy(row=>row.id).ToDictionary(group=>group.Key,group=>group.First().additionalEffects??Array.Empty<CardTableEffect>());foreach(var row in rows)row.additionalEffects=Array.Empty<CardTableEffect>();
   var groups=new Dictionary<string,SortedDictionary<int,CardTableEffect>>(StringComparer.Ordinal);
   for(int i=0;i<cells.Length;i++){
    var c=cells[i];if(c==null||c.All(string.IsNullOrWhiteSpace)||c.FirstOrDefault()=="卡牌编号")continue;
    try{
     if(c.Length<8)throw new FormatException("需要复合效果表的全部 8 列。");string id=(c[0]??"").Trim();if(!rows.Any(r=>r.id==id))throw new FormatException("卡牌编号在导入主表中不存在："+id);
     if(!int.TryParse(c[1],out int order)||order<2||order>100)throw new FormatException("执行序号须为 2–100，主效果固定为 1。");
     if(!Enum.TryParse(c[2],out CardEffect effect)||!Enum.IsDefined(typeof(CardEffect),effect))throw new FormatException("结算效果未知："+c[2]);
     int life=Array.IndexOf(CardTableWorkbook.Lifetimes,c[3]),polarity=Array.IndexOf(CardTableWorkbook.Polarities,c[4]);if(life<0||polarity<0)throw new FormatException("持续方式或性质未知。");
     var e=new CardTableEffect{effect=effect,lifetime=(EffectLifetime)life,polarity=(EffectPolarity)polarity,description=c[5],range=c[6]};if(previousExtras.TryGetValue(id,out var prior)&&order-2<prior.Length&&prior[order-2]!=null&&prior[order-2].effect==effect){e.shieldBonusPercent=prior[order-2].shieldBonusPercent;e.reductionPercent=prior[order-2].reductionPercent;}if(c.Length>8&&!string.IsNullOrWhiteSpace(c[8])){if(!int.TryParse(c[8],out e.shieldBonusPercent))throw new FormatException("护盾增幅(%)须为整数。");}if(c.Length>9&&!string.IsNullOrWhiteSpace(c[9])){if(!int.TryParse(c[9],out e.reductionPercent))throw new FormatException("减幅(%)须为整数。");}if(e.effect==CardEffect.IronWall&&(e.shieldBonusPercent<0||e.shieldBonusPercent>10000)||((e.effect==CardEffect.Weaken||e.effect==CardEffect.Fortify)&&(e.reductionPercent<0||e.reductionPercent>100)))throw new FormatException("百分比超出范围。");
     string enabled=c[7]??"";if(enabled=="0"||enabled=="否"||enabled.Equals("false",StringComparison.OrdinalIgnoreCase))e.enabled=false;else if(enabled!="1"&&enabled!="是"&&!enabled.Equals("true",StringComparison.OrdinalIgnoreCase))throw new FormatException("启用须为 1/0 或 是/否。");
     CardTableRange.Parse(e.range,out _,out int min,out int max);if(min<1||max<min||max>10000)throw new FormatException("基础取值范围须为 1–10000，且上限不小于下限。");
     if(!groups.TryGetValue(id,out var group))groups[id]=group=new SortedDictionary<int,CardTableEffect>();if(group.ContainsKey(order))throw new FormatException("同卡牌执行序号重复："+order);group[order]=e;
    }catch(FormatException e){Errors.Add("复合效果 · 第 "+(i+1)+" 行："+e.Message);}
   }
   foreach(var entry in groups){if(!entry.Value.Keys.SequenceEqual(Enumerable.Range(2,entry.Value.Count)))Errors.Add("复合效果 · "+entry.Key+"：执行序号应从 2 连续编号，不能跳号。");foreach(var row in rows.Where(r=>r.id==entry.Key))row.additionalEffects=entry.Value.Values.Select(e=>e.Copy()).ToArray();}
  }
  public CardTableRow[] Result(){
   var result=before.Select(r=>r?.Copy()).ToList();
   foreach(var change in Changes.Where(c=>c.Selected)){
    int index=result.FindIndex(r=>r!=null&&r.id==change.Id);
    if(change.After==null){if(index>=0)result.RemoveAt(index);}else if(index>=0)result[index]=change.After.Copy();else result.Add(change.After.Copy());
   }
   return result.ToArray();
  }
  public void Apply(){
   if(Target==null||EditorJsonUtility.ToJson(Target)!=snapshot)throw new InvalidOperationException("预览后配表发生变化，请重新预览再应用。");
   if(Errors.Count>0)throw new InvalidOperationException(string.Join("\n",Errors));
   var result=Result();var issues=CardTableValidation.Check(result,enemy);if(issues.Length>0)throw new InvalidOperationException(string.Join("\n",issues.Select(e=>e.ToString())));
   foreach(var row in result)row.NormalizeRange();Undo.RegisterCompleteObjectUndo(Target,"应用卡牌导入预览");Target.rows=result;EditorUtility.SetDirty(Target);Applied?.Invoke();
  }
  static string Describe(CardTableRow a,CardTableRow b){
   string[] left=CardTableWorkbook.Cells(a),right=CardTableWorkbook.Cells(b);var details=new List<string>();
   for(int i=0;i<left.Length;i++)if(left[i]!=right[i])details.Add((i==2?"持续方式":i==3?"性质":CardTableWorkbook.Headers[i])+"："+left[i]+" → "+right[i]);
   if(string.Join("|",(a.additionalEffects??Array.Empty<CardTableEffect>()).Select(e=>e==null?"null":JsonUtility.ToJson(e)))!=string.Join("|",(b.additionalEffects??Array.Empty<CardTableEffect>()).Select(e=>e==null?"null":JsonUtility.ToJson(e))))details.Add("追加效果配置发生变化");
   return details.Count==0?"无差异":string.Join("\n",details);
  }
 }
 public static class CardTableBulkEdit {
  public static void Apply(CardTableDefinition table,IEnumerable<int> indices,Action<CardTableRow> edit){
   var result=table.rows.Select(r=>r?.Copy()).ToArray();var selected=indices.Distinct().ToArray();if(selected.Length==0)throw new InvalidOperationException("先勾选需要修改的行。");
   foreach(int i in selected){if(i<0||i>=result.Length||result[i]==null)throw new InvalidOperationException("选择行已失效，请重新选择。");edit(result[i]);}
   var issues=CardTableValidation.Check(result,table is EnemyCardTable);if(issues.Length>0)throw new InvalidOperationException(string.Join("\n",issues.Select(e=>e.ToString())));
   foreach(var row in result)row.NormalizeRange();Undo.RegisterCompleteObjectUndo(table,"批量编辑卡牌配表");table.rows=result;EditorUtility.SetDirty(table);
  }
 }
}
