using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class NormalEnemyWorkbook {
  static string Cell(string[][] grid,int r,int c)=>r<grid.Length&&c<grid[r].Length?(grid[r][c]??""):"";
  static int Integer(string value)=>int.Parse(value,NumberStyles.Integer,CultureInfo.InvariantCulture);
  static float Number(string value)=>float.Parse(value,NumberStyles.Float,CultureInfo.InvariantCulture);
  public static NormalEnemyRow[] PreserveUnchangedConfigurations(NormalEnemyRow[] imported,NormalEnemyRow[] existing){
   return imported.Select(r=>{var prior=(existing??Array.Empty<NormalEnemyRow>()).SingleOrDefault(p=>p!=null&&p.sheetName==r.sheetName);var result=!r.configured&&prior!=null&&prior.configured&&Grid(prior).SelectMany(c=>c.Select(v=>v??"")).SequenceEqual(Grid(r).SelectMany(c=>c.Select(v=>v??"")))?prior.Copy():r;if(prior!=null)result.portrait=prior.portrait;else result.portrait=null;return result;}).ToArray();
  }
  static string ExportConfiguration(NormalEnemyRow row){var copy=row.Copy();copy.portrait=null;return UnityEngine.JsonUtility.ToJson(copy);}
  public static NormalEnemyRow[] Read(string path){
   const string configSheet="_数值配置";
   string[] sheets;
   using(var zip=ZipFile.OpenRead(path))using(var stream=(zip.GetEntry("xl/workbook.xml")??throw new FormatException("缺少工作簿定义")).Open())using(var reader=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=20000000})){
    XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    sheets=XDocument.Load(reader).Descendants(ns+"sheet").Select(s=>(string)s.Attribute("name")).ToArray();
   }
   var result=new List<NormalEnemyRow>();
   foreach(string name in sheets.Where(s=>s!=configSheet)){try{
    var grid=CardTableWorkbook.ReadSheet(path,name,12);
    if(Cell(grid,3,0)!="血量"||Cell(grid,3,2)!="回合技能使用数"||Cell(grid,4,0)!="基础攻击值")throw new FormatException("布局不符，请使用普通敌人原表布局。");
    var row=new NormalEnemyRow{id=name,sheetName=name,title=Cell(grid,0,0),health=Integer(Cell(grid,3,1)),actions=Cell(grid,3,3),healthThresholdPercent=Integer(Cell(grid,3,5).Trim().TrimEnd('%','％')),sequenceCount=Integer(Cell(grid,3,7)),attack=Cell(grid,4,1),skillCount=Integer(Cell(grid,4,3)),initialEffects=Cell(grid,4,5)};
    var skills=new List<EnemySkillContent>();var sequences=new List<EnemySequenceContent>();var rewards=new List<EnemyRewardContent>();
    for(int i=0;i<grid.Length;i++){
     string label=Cell(grid,i,0);
     if((label.StartsWith("基础技能")||label.StartsWith("特殊技能"))&&label!="基础技能组"&&label!="特殊技能组")skills.Add(new EnemySkillContent{slot=label,special=label.StartsWith("特殊"),name=Cell(grid,i,1),description=Cell(grid,i,3),numberRange=Cell(grid,i,6),conditionLabel=Cell(grid,i,7),condition=Cell(grid,i,8),weight=Number(Cell(grid,i,10)),notes=Cell(grid,i,11)});
     else if(label.StartsWith("牌序效果")&&label!="牌序效果")sequences.Add(new EnemySequenceContent{slot=label,name=Cell(grid,i,1),description=Cell(grid,i,3),numbers=Cell(grid,i,6)});
     else if(label.StartsWith("奖励")&&label!="奖励内容")rewards.Add(new EnemyRewardContent{slot=label,name=Cell(grid,i,1),amount=Cell(grid,i,3),probability=Number(Cell(grid,i,5)),notes=Cell(grid,i,6)});
    }
    row.skills=skills.ToArray();row.sequences=sequences.ToArray();row.rewards=rewards.ToArray();result.Add(row);
   }catch(Exception e){throw new FormatException("工作表「"+name+"」："+e.Message,e);}}
   if(sheets.Contains(configSheet)){
    var configs=CardTableWorkbook.ReadSheet(path,configSheet,3);var seen=new HashSet<string>();
    foreach(var line in configs.Skip(1)){
     if(string.IsNullOrWhiteSpace(line[0]))continue;
     if(line[1]!="1"||!seen.Add(line[0]))throw new FormatException("数值配置版本错误或工作表重复。");
     var authored=UnityEngine.JsonUtility.FromJson<NormalEnemyRow>(line[2]);int at=result.FindIndex(r=>r.sheetName==line[0]);
     if(authored==null||at<0||authored.sheetName!=line[0]||!Grid(authored).SelectMany(r=>r.Select(c=>c??"")).SequenceEqual(Grid(result[at]).SelectMany(r=>r.Select(c=>c??""))))throw new FormatException(line[0]+"：数值配置与原表不一致，请在Unity同步修改效果配置后重新导出。");
     if(authored.configured){var issues=EnemyConfiguredContent.Check(authored);if(issues.Length>0)throw new FormatException(string.Join("\n",issues));}
     result[at]=authored;
    }
    if(seen.Count!=result.Count)throw new FormatException("数值配置未覆盖所有敌人。");
   }
   var rows=result.ToArray();var errors=NormalEnemyTable.Check(rows);if(errors.Length>0)throw new FormatException(string.Join("\n",errors));return rows;
  }
  public static string[][] Grid(NormalEnemyRow r){
   var grid=new List<string[]>();Action<string[]> add=c=>{var row=new string[12];Array.Copy(c,row,c.Length);grid.Add(row);};
   add(new[]{r.title});add(Array.Empty<string>());add(new[]{"基础数值"});
   add(new[]{"血量",r.health.ToString(),"回合技能使用数",r.actions,"血量匝值",r.healthThresholdPercent+"%","牌序效果数",r.sequenceCount.ToString()});
   add(new[]{"基础攻击值",r.attack,"技能总数",r.skillCount.ToString(),"初始效果",r.initialEffects});add(Array.Empty<string>());add(new[]{"基础技能组"});
   foreach(var s in r.skills.Where(s=>!s.special))add(new[]{s.slot,s.name,"技能效果",s.description,"","数字范围",s.numberRange,s.conditionLabel,s.condition,"权重",s.weight.ToString(CultureInfo.InvariantCulture),s.notes});
   add(new[]{"特殊技能组"});foreach(var s in r.skills.Where(s=>s.special))add(new[]{s.slot,s.name,"技能效果",s.description,"","数字范围",s.numberRange,s.conditionLabel,s.condition,"权重",s.weight.ToString(CultureInfo.InvariantCulture),s.notes});
   add(Array.Empty<string>());add(Array.Empty<string>());add(new[]{"牌序效果"});foreach(var s in r.sequences)add(new[]{s.slot,s.name,"具体效果",s.description,"","数字排列",s.numbers});
   add(Array.Empty<string>());add(Array.Empty<string>());add(new[]{"奖励内容"});foreach(var s in r.rewards)add(new[]{s.slot,s.name,"数量",s.amount,"出现概率",s.probability.ToString(CultureInfo.InvariantCulture),s.notes});
   return grid.ToArray();
  }
  public static void Write(string path,NormalEnemyRow[] rows){
   var errors=NormalEnemyTable.Check(rows);if(errors.Length>0)throw new FormatException(string.Join("\n",errors));
   var grids=rows.Select(Grid).ToArray();string temp=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)),Guid.NewGuid()+".xlsx");
   if(rows.Any(r=>r.sheetName=="_数值配置"))throw new FormatException("_数值配置是保留工作表名称。");
   foreach(var r in rows.Where(r=>r.configured)){var issues=EnemyConfiguredContent.Check(r);if(issues.Length>0)throw new FormatException(string.Join("\n",issues));}
   try{CardTableWorkbook.WriteSheets(temp,rows.Select(r=>r.sheetName).Concat(new[]{"_数值配置"}).ToArray(),grids.Select(g=>g[0]).Concat(new[]{new[]{"工作表名","版本","配置JSON（通过Unity编辑器编辑）"}}).ToArray(),grids.Select(g=>g.Skip(1).ToArray()).Concat(new[]{rows.Select(r=>new[]{r.sheetName,"1",ExportConfiguration(r)}).ToArray()}).ToArray());File.Copy(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
  }
 }
}
