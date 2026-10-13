using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class EffectTableWorkbook {
  public static string[] Headers(bool sequence)=>new[]{"效果编号","效果名称","具体效果","基础取值范围（存在X时）"}.Concat(sequence?new[]{"卡效所属池","备注"}:new[]{"备注"}).Concat(new[]{"启用","权重","程序效果标识","X倍率","固定参数"}).ToArray();
  static string Cell(string[] r,int i,string fallback="")=>i<r.Length&&!string.IsNullOrEmpty(r[i])?r[i]:fallback;
  static int Int(string s,int fallback){if(string.IsNullOrWhiteSpace(s))return fallback;if(int.TryParse(s,NumberStyles.Integer,CultureInfo.InvariantCulture,out int n))return n;throw new FormatException("不是整数："+s);}
  static bool Bool(string s,bool fallback){if(string.IsNullOrWhiteSpace(s))return fallback;if(s=="1"||s=="是"||s.Equals("true",StringComparison.OrdinalIgnoreCase))return true;if(s=="0"||s=="否"||s.Equals("false",StringComparison.OrdinalIgnoreCase))return false;throw new FormatException("启用请填 1/0 或 是/否。");}
  public static EffectTableRow[] Parse(IEnumerable<string[]> cells,bool sequence,IEnumerable<EffectTableRow> known){
   cells=CanonicalRows(cells,sequence);
   var lookup=(known??Array.Empty<EffectTableRow>()).Where(r=>r!=null).GroupBy(r=>r.id).ToDictionary(g=>g.Key,g=>g.First());var result=new List<EffectTableRow>();int line=0,baseCols=sequence?6:5;
   foreach(var c in cells){line++;if(c.All(string.IsNullOrWhiteSpace))continue;
    if(Cell(c,0)=="效果编号"){var header=Headers(sequence);for(int i=0;i<baseCols;i++)if(Cell(c,i)!=header[i])throw new FormatException("表头第 "+(i+1)+" 列应为："+header[i]);continue;}
    try{
     if(c.Length<baseCols)throw new FormatException("至少需要原表的 "+baseCols+" 列。");
     string id=Cell(c,0).Trim();lookup.TryGetValue(id,out var previous);var r=previous?.Copy()??new EffectTableRow();
     r.id=id;r.name=Cell(c,1);r.description=Cell(c,2);r.range=Cell(c,3);r.pools=sequence?Cell(c,4):"";r.notes=Cell(c,sequence?5:4);
     r.enabled=Bool(Cell(c,baseCols),previous?.enabled??true);r.weight=float.Parse(Cell(c,baseCols+1,(previous?.weight??10).ToString(CultureInfo.InvariantCulture)),CultureInfo.InvariantCulture);
     r.behavior=Cell(c,baseCols+2,previous?.behavior);if(string.IsNullOrWhiteSpace(r.behavior))throw new FormatException("新编号必须填写程序效果标识，请复制已有行或使用导出模板。");
     r.multiplier=Int(Cell(c,baseCols+3),previous?.multiplier??1);r.fixedValue=Int(Cell(c,baseCols+4),previous?.fixedValue??0);result.Add(r);
    }catch(Exception e){throw new FormatException("Excel 第 "+line+" 行："+e.Message,e);}
   }
   return result.ToArray();
  }
  static string[][] CanonicalRows(IEnumerable<string[]> source,bool sequence){
   var rows=source.ToArray();var header=rows.FirstOrDefault(r=>Cell(r,0)=="效果编号");int weightColumn=sequence?5:4;
   if(header==null||Cell(header,weightColumn)!="权重")return rows;
   return rows.Select(r=>{if(Cell(r,0)=="效果编号")return Headers(sequence);if(r.All(string.IsNullOrWhiteSpace))return r;int baseCols=sequence?6:5;var c=new string[baseCols+5];for(int i=0;i<4;i++)c[i]=Cell(r,i);if(sequence)c[4]=Cell(r,4);c[baseCols-1]=Cell(r,weightColumn+1);c[baseCols+1]=Cell(r,weightColumn);return c;}).ToArray();
  }
  public static string[] Cells(EffectTableRow r,bool sequence)=>new[]{r.id,r.name,r.description,r.range}.Concat(sequence?new[]{r.pools,r.notes}:new[]{r.notes}).Concat(new[]{r.enabled?"1":"0",r.weight.ToString(CultureInfo.InvariantCulture),r.behavior,r.multiplier.ToString(CultureInfo.InvariantCulture),r.fixedValue.ToString(CultureInfo.InvariantCulture)}).ToArray();
  public static void Write(string path,EffectTableDefinition table){table.ValidateTable();CardTableWorkbook.WriteRows(path,Headers(table.IsSequence),table.rows.Select(r=>Cells(r,table.IsSequence)));}
 }
}
