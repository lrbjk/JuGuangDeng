using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class EffectPoolWeightWorkbook {
  public static readonly string[] Headers={"编号","名称","权重"};
  public static EffectPoolWeightRow[] Read(string path,EffectPoolKind kind){var rows=Read(path);var issues=EffectPoolWeightTable.CheckKind(rows,kind);if(issues.Length>0)throw new FormatException(string.Join("\n",issues));return rows;}
  static string Cell(string[] row,int at)=>at<row.Length?row[at]??"":"";
  public static EffectPoolWeightRow[] Read(string path){
   var data=CardTableWorkbook.Read(path,3);if(data.Length==0||!data[0].Select(c=>c??"").SequenceEqual(Headers))throw new FormatException("表头必须为：编号、名称、权重。");
   var rows=new List<EffectPoolWeightRow>();for(int i=1;i<data.Length;i++){var line=data[i];if(line.All(string.IsNullOrWhiteSpace))continue;float weight;if(!float.TryParse(Cell(line,2),NumberStyles.Float,CultureInfo.InvariantCulture,out weight))throw new FormatException("第"+(i+1)+"行：权重不是有效数值。");rows.Add(new EffectPoolWeightRow{id=Cell(line,0).Trim(),name=Cell(line,1).Trim(),weight=weight});}
   var issues=EffectPoolWeightTable.Check(rows.ToArray());if(issues.Length>0)throw new FormatException(string.Join("\n",issues));return rows.ToArray();
  }
  public static void Write(string path,EffectPoolWeightRow[] rows){
   var errors=EffectPoolWeightTable.Check(rows);if(errors.Length>0)throw new FormatException(string.Join("\n",errors));string temp=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)),Guid.NewGuid()+".xlsx");try{CardTableWorkbook.WriteRows(temp,Headers,rows.Select(r=>new[]{r.id,r.name,r.weight.ToString(CultureInfo.InvariantCulture)}));File.Copy(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
  }
 }
}
