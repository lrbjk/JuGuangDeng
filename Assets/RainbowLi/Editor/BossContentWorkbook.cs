using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class BossContentWorkbook {
  const string ConfigSheet="_数值配置";
  static string Cell(string[][] g,int r,int c)=>r<g.Length&&c<g[r].Length?g[r][c]??"":"";
  static int Int(string v)=>int.Parse(v,CultureInfo.InvariantCulture);
  public static BossContentRow[] Read(string path){
   string[] sheets;using(var zip=ZipFile.OpenRead(path))using(var stream=(zip.GetEntry("xl/workbook.xml")??throw new FormatException("缺少工作簿")).Open())using(var reader=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=20000000})){XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";sheets=XDocument.Load(reader).Descendants(ns+"sheet").Select(s=>(string)s.Attribute("name")).ToArray();}
   var rows=new List<BossContentRow>();
   foreach(string name in sheets.Where(s=>s!=ConfigSheet)){try{
    var g=CardTableWorkbook.ReadSheet(path,name,12);if(Cell(g,3,0)!="血量"||Cell(g,3,8)!="出没关卡层数")throw new FormatException("请使用BOSS表格原格式。");
    var r=new BossContentRow{spawnLayers=Cell(g,3,9),thresholdText=Cell(g,3,5)};var c=r.content;c.id=c.sheetName=name;c.title=Cell(g,0,0);c.health=Int(Cell(g,3,1));c.actions=Cell(g,3,3);c.healthThresholdPercent=Int(Regex.Match(r.thresholdText,@"\d+").Value);c.sequenceCount=Int(Cell(g,3,7));c.attack=Cell(g,4,1);c.skillCount=Int(Cell(g,4,3));c.initialEffects=Cell(g,4,5);
    var phases=new List<BossPhase>{new BossPhase{index=0,label="初始效果",description=c.initialEffects}};
    for(int line=5;line<7;line++){string label=Cell(g,line,4);if(label.StartsWith("一阶段")||label.StartsWith("二阶段")){int phase=label.StartsWith("一")?1:2;var percents=Regex.Matches(r.thresholdText,@"(\d+)%");phases.Add(new BossPhase{index=phase,label=label,description=Cell(g,line,5),thresholdPercent=phase<=percents.Count?Int(percents[phase-1].Groups[1].Value):0});}}
    r.phases=phases.ToArray();var skills=new List<EnemySkillContent>();var sequences=new List<EnemySequenceContent>();var rewards=new List<EnemyRewardContent>();var rules=new List<BossSkillRule>();var sequenceRules=new List<BossSequenceRule>();
    for(int line=0;line<g.Length;line++){string label=Cell(g,line,0);
     if((label.StartsWith("基础技能")||label.StartsWith("特殊技能"))&&!label.EndsWith("组")){string weight=Cell(g,line,10);skills.Add(new EnemySkillContent{slot=label,name=Cell(g,line,1),description=Cell(g,line,3),numberRange=Cell(g,line,6),conditionLabel=Cell(g,line,7),condition=Cell(g,line,8),special=label.StartsWith("特殊"),weight=weight=="固定触发"?0:float.Parse(weight,CultureInfo.InvariantCulture),notes=Cell(g,line,11)});rules.Add(new BossSkillRule{slot=label,fixedTrigger=weight=="固定触发"});}
     else if(label.StartsWith("牌序效果")&&label!="牌序效果"){string numbers=Cell(g,line,6);sequences.Add(new EnemySequenceContent{slot=label,name=Cell(g,line,1),description=Cell(g,line,3),numbers=numbers});var rule=new BossSequenceRule{slot=label};if(numbers.StartsWith("构成所需")){rule.mode=BossSequenceMode.必须包含指定数字;rule.requiredNumber=Int(Regex.Match(numbers,@"\d+").Value);}else if(numbers.StartsWith("构成牌序")){rule.mode=BossSequenceMode.固定排列;rule.fixedNumbers=Regex.Match(numbers,@"\d+").Value.Select(ch=>ch-'0').ToArray();}sequenceRules.Add(rule);}
     else if(label.StartsWith("奖励")&&label!="奖励内容")rewards.Add(new EnemyRewardContent{slot=label,name=Cell(g,line,1),amount=Cell(g,line,3),probability=float.Parse(Cell(g,line,5),CultureInfo.InvariantCulture),notes=Cell(g,line,6)});
    }
    c.skills=skills.ToArray();c.sequences=sequences.ToArray();c.rewards=rewards.ToArray();r.skillRules=rules.ToArray();r.sequenceRules=sequenceRules.ToArray();rows.Add(r);
   }catch(Exception e){throw new FormatException("工作表「"+name+"」："+e.Message,e);}}
   if(sheets.Contains(ConfigSheet)){var data=CardTableWorkbook.ReadSheet(path,ConfigSheet,3);var seen=new HashSet<string>();foreach(var line in data.Skip(1)){if(string.IsNullOrWhiteSpace(line[0]))continue;if(line[1]!="1"||!seen.Add(line[0]))throw new FormatException("BOSS数值配置版本无效或重复。");var authored=JsonUtility.FromJson<BossContentRow>(line[2]);int at=rows.FindIndex(r=>r.content.sheetName==line[0]);if(authored==null||at<0||!SameGrid(authored,rows[at]))throw new FormatException(line[0]+"：数值页与原表不一致，请在Unity同步修改。");rows[at]=authored;}if(seen.Count!=rows.Count)throw new FormatException("BOSS数值页未覆盖全部工作表。");}
   var result=rows.ToArray();var errors=BossContentTable.Check(result);if(errors.Length>0)throw new FormatException(string.Join("\n",errors));if(result.Any(r=>r.content.configured)){errors=BossContentTable.Check(result.Where(r=>r.content.configured).ToArray(),true);if(errors.Length>0)throw new FormatException(string.Join("\n",errors));}return result;
  }
  public static bool SameGrid(BossContentRow a,BossContentRow b)=>Grid(a).SelectMany(r=>r.Select(c=>c??"")).SequenceEqual(Grid(b).SelectMany(r=>r.Select(c=>c??"")));
  public static BossContentRow[] PreserveUnchanged(BossContentRow[] imported,BossContentRow[] existing)=>imported.Select(r=>{var prior=(existing??Array.Empty<BossContentRow>()).SingleOrDefault(p=>p?.content.sheetName==r.content.sheetName);return !r.content.configured&&prior!=null&&prior.content.configured&&SameGrid(r,prior)?prior.Copy():r;}).ToArray();
  public static string[][] Grid(BossContentRow r){
   var c=r.content;var grid=new List<string[]>();Action<string[]> add=data=>{var line=new string[12];Array.Copy(data,line,data.Length);grid.Add(line);};Action blank=()=>add(Array.Empty<string>());
   add(new[]{c.title});blank();add(new[]{"基础数值"});add(new[]{"血量",c.health.ToString(),"回合技能使用数",c.actions,"血量匝值",r.thresholdText,"牌序效果数",c.sequenceCount.ToString(),"出没关卡层数",r.spawnLayers});add(new[]{"基础攻击值",c.attack,"技能总数",c.skillCount.ToString(),"初始效果",c.initialEffects});
   foreach(var phase in r.phases.Where(p=>p.index>0))add(new[]{"","","","",phase.label,phase.description});if(r.phases.Length==1)blank();add(new[]{"基础技能组"});
   Action<EnemySkillContent> skill=s=>{var rule=r.skillRules.SingleOrDefault(p=>p.slot==s.slot);add(new[]{s.slot,s.name,"技能效果",s.description,"","数字范围",s.numberRange,s.conditionLabel,s.condition,"权重",rule!=null&&rule.fixedTrigger?"固定触发":s.weight.ToString(CultureInfo.InvariantCulture),s.notes});};
   foreach(var s in c.skills.Where(p=>!p.special))skill(s);blank();if(r.phases.Length==1)blank();add(new[]{"特殊技能组"});foreach(var s in c.skills.Where(p=>p.special))skill(s);blank();if(r.phases.Length==1)blank();add(new[]{"牌序效果"});foreach(var s in c.sequences)add(new[]{s.slot,s.name,"具体效果",s.description,"","数字排列",s.numbers});blank();if(r.phases.Length==1)blank();add(new[]{"奖励内容"});foreach(var s in c.rewards)add(new[]{s.slot,s.name,"数量",s.amount,"出现概率",s.probability.ToString(CultureInfo.InvariantCulture),s.notes});return grid.ToArray();
  }
  public static void Write(string path,BossContentRow[] rows){
   var issues=BossContentTable.Check(rows);if(issues.Length>0)throw new FormatException(string.Join("\n",issues));if(rows.Any(r=>r.content.sheetName==ConfigSheet))throw new FormatException("工作表名保留给数值配置。");foreach(var r in rows.Where(r=>r.content.configured)){issues=BossContentTable.Check(new[]{r},true);if(issues.Length>0)throw new FormatException(string.Join("\n",issues));}
   var grids=rows.Select(Grid).ToArray();string temp=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)),Guid.NewGuid()+".xlsx");try{CardTableWorkbook.WriteSheets(temp,rows.Select(r=>r.content.sheetName).Concat(new[]{ConfigSheet}).ToArray(),grids.Select(g=>g[0]).Concat(new[]{new[]{"工作表名","版本","配置JSON（由Unity编辑）"}}).ToArray(),grids.Select(g=>g.Skip(1).ToArray()).Concat(new[]{rows.Select(r=>new[]{r.content.sheetName,"1",JsonUtility.ToJson(r)}).ToArray()}).ToArray());File.Copy(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
  }
 }
}
