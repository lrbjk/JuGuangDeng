using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 // Small Open XML reader/writer: no Excel installation or runtime dependency required.
 public static class CardTableWorkbook {
  public static readonly string[] Headers={"效果编号","效果名称","效果类型","效果类型","对应效果池","基础取值范围（存在X时）","备注","启用","数字","权重","起始牌","特殊技能","触发条件","阈值","特殊权重比","结算效果"};
  public static readonly string[] Lifetimes={"其他类型效果","临时性效果","持续性效果"};
  public static readonly string[] Polarities={"正面效果","负面效果","特殊效果"};
  static readonly XNamespace S="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
  static string Cell(string[] cells,int index,string fallback="") => index<cells.Length?cells[index]??fallback:fallback;
  static int Int(string s,int fallback){if(string.IsNullOrWhiteSpace(s))return fallback;if(double.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out var d)&&d==Math.Truncate(d)&&d>=int.MinValue&&d<=int.MaxValue)return (int)d;throw new FormatException("不是整数："+s);}
  static float Float(string s,float fallback)=>string.IsNullOrWhiteSpace(s)?fallback:float.Parse(s,CultureInfo.InvariantCulture);
  static bool Bool(string s,bool fallback){if(string.IsNullOrWhiteSpace(s))return fallback;if(s=="1"||s=="是"||s.Equals("true",StringComparison.OrdinalIgnoreCase))return true;if(s=="0"||s=="否"||s.Equals("false",StringComparison.OrdinalIgnoreCase))return false;throw new FormatException("开关须为 1/0 或 是/否："+s);}
  public static void SetRange(CardTableRow row,string text){
   var normalized=(text??"").Trim().Replace('－','-').Replace('–','-').Replace('—','-').Replace('~','-').Replace('～','-');
   if(normalized.Contains("卡牌对应数字")||normalized=="数字"){row.usesNumber=true;row.min=1;row.max=6;row.range="为卡牌对应数字";return;}
   var m=Regex.Match(normalized,@"^(\d+)\s*(?:-\s*(\d+))?$");if(!m.Success)throw new FormatException("基础取值范围须为 1-2、1 或 为卡牌对应数字。");
   row.usesNumber=false;row.min=int.Parse(m.Groups[1].Value);row.max=m.Groups[2].Success?int.Parse(m.Groups[2].Value):row.min;row.range=text.Trim();
  }
  public static CardTableRow[] Parse(IEnumerable<string[]> source,IEnumerable<CardTableRow> known){
   var lookup=(known??Array.Empty<CardTableRow>()).Where(r=>r!=null).GroupBy(r=>r.id).ToDictionary(g=>g.Key,g=>g.First());
   var result=new List<CardTableRow>();int line=0;
   foreach(var cells in source){line++;if(cells.All(string.IsNullOrWhiteSpace)||Cell(cells,0)=="效果编号")continue;
    try{
     if(cells.Length<7)throw new FormatException("至少需要原表的七列。");
     var id=Cell(cells,0).Trim();lookup.TryGetValue(id,out var previous);var row=previous?.Copy()??new CardTableRow();row.id=id;row.name=Cell(cells,1);row.description=Cell(cells,4);row.notes=Cell(cells,6);
     int life=Array.IndexOf(Lifetimes,Cell(cells,2)),polarity=Array.IndexOf(Polarities,Cell(cells,3));if(life<0||polarity<0)throw new FormatException("效果类型须使用原表中的类型名称。");row.lifetime=(EffectLifetime)life;row.polarity=(EffectPolarity)polarity;SetRange(row,Cell(cells,5));
     row.enabled=Bool(Cell(cells,7),previous?.enabled??true);row.number=Int(Cell(cells,8),previous?.number??1);row.weight=Float(Cell(cells,9),previous?.weight??10);row.starting=Bool(Cell(cells,10),previous?.starting??true);row.special=Bool(Cell(cells,11),previous?.special??false);
     string condition=Cell(cells,12);row.condition=string.IsNullOrWhiteSpace(condition)?previous?.condition??ActivationCondition.RoundAtLeast:condition=="生命百分比"||condition=="HealthPercentAtMost"?ActivationCondition.HealthPercentAtMost:condition=="回合"||condition=="RoundAtLeast"?ActivationCondition.RoundAtLeast:throw new FormatException("触发条件须为 回合 或 生命百分比。");
     row.threshold=Int(Cell(cells,13),previous?.threshold??3);row.specialWeightRatio=Float(Cell(cells,14),previous?.specialWeightRatio??.5f);
     string kind=Cell(cells,15);if(!string.IsNullOrWhiteSpace(kind)){if(!Enum.TryParse(kind,out CardEffect effect)||!Enum.IsDefined(typeof(CardEffect),effect))throw new FormatException("未知结算效果："+kind);row.effect=effect;}else if(previous==null)throw new FormatException("新编号需要填写结算效果列；请复制已有行或使用导出的模板。");
     result.Add(row);
    }catch(Exception e){throw new FormatException("第 "+line+" 行："+e.Message,e);}
   }
   return result.ToArray();
  }
  public static string[] Cells(CardTableRow r)=>new[]{r.id,r.name,Lifetimes[(int)r.lifetime],Polarities[(int)r.polarity],r.description,r.range,r.notes,r.enabled?"1":"0",r.number.ToString(),r.weight.ToString(CultureInfo.InvariantCulture),r.starting?"1":"0",r.special?"1":"0",r.condition==ActivationCondition.RoundAtLeast?"回合":"生命百分比",r.threshold.ToString(),r.specialWeightRatio.ToString(CultureInfo.InvariantCulture),r.effect.ToString()};
  static XDocument Document(ZipArchive zip,string name){var entry=zip.GetEntry(name)??throw new FormatException("缺少 XLSX 内容："+name);using(var stream=entry.Open())using(var reader=XmlReader.Create(stream,new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=20000000}))return XDocument.Load(reader);}
  static int Column(string address){int n=0;foreach(char c in address){if(!char.IsLetter(c))break;n=n*26+char.ToUpperInvariant(c)-'A'+1;}return n-1;}
  public static string[][] Read(string path,int columnCount=16){
   using(var zip=ZipFile.OpenRead(path)){
    var book=Document(zip,"xl/workbook.xml");var sheets=book.Descendants(S+"sheet").ToArray();var sheet=sheets.FirstOrDefault(e=>(string)e.Attribute("name")=="Sheet1")??sheets.FirstOrDefault()??throw new FormatException("工作簿没有工作表。");
    XNamespace relationships="http://schemas.openxmlformats.org/officeDocument/2006/relationships";var id=(string)sheet.Attribute(relationships+"id");var rel=Document(zip,"xl/_rels/workbook.xml.rels").Root.Elements().First(e=>(string)e.Attribute("Id")==id);if((string)rel.Attribute("TargetMode")=="External")throw new FormatException("不能读取外部工作表。");
    var location=(string)rel.Attribute("Target");var uri=new Uri(new Uri("http://local/xl/workbook.xml"),location);var sheetPath=Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
    var shared=zip.GetEntry("xl/sharedStrings.xml")==null?Array.Empty<string>():Document(zip,"xl/sharedStrings.xml").Descendants(S+"si").Select(e=>string.Concat(e.Descendants(S+"t").Select(t=>t.Value))).ToArray();
    return Document(zip,sheetPath).Descendants(S+"row").Select(row=>{
     var cells=new string[columnCount];foreach(var cell in row.Elements(S+"c")){int col=Column((string)cell.Attribute("r")??"");if(col<0||col>=cells.Length)continue;if(cell.Element(S+"f")!=null)throw new FormatException("配表不支持公式，请粘贴为值后导入。");string value=(string)cell.Element(S+"v")??"";string type=(string)cell.Attribute("t");cells[col]=type=="s"?shared[int.Parse(value)]:type=="inlineStr"?string.Concat(cell.Descendants(S+"t").Select(e=>e.Value)):value;}return cells;
    }).Where(r=>r.Any(c=>!string.IsNullOrWhiteSpace(c))).ToArray();
   }
  }
  static void WriteEntry(ZipArchive zip,string name,XDocument doc){using(var stream=zip.CreateEntry(name).Open())doc.Save(stream);}
  static string Address(int c){string a="";for(c++;c>0;c=(c-1)/26)a=(char)('A'+(c-1)%26)+a;return a;}
  public static void Write(string path,IEnumerable<CardTableRow> rows){WriteRows(path,Headers,rows.Select(Cells));}
  public static void WriteRows(string path,string[] headers,IEnumerable<string[]> rows){
   using(var zip=ZipFile.Open(path,ZipArchiveMode.Create)){
    XNamespace types="http://schemas.openxmlformats.org/package/2006/content-types",rels="http://schemas.openxmlformats.org/package/2006/relationships",office="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    WriteEntry(zip,"[Content_Types].xml",new XDocument(new XElement(types+"Types",new XElement(types+"Default",new XAttribute("Extension","rels"),new XAttribute("ContentType","application/vnd.openxmlformats-package.relationships+xml")),new XElement(types+"Default",new XAttribute("Extension","xml"),new XAttribute("ContentType","application/xml")),new XElement(types+"Override",new XAttribute("PartName","/xl/workbook.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),new XElement(types+"Override",new XAttribute("PartName","/xl/worksheets/sheet1.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")))));
    WriteEntry(zip,"_rels/.rels",new XDocument(new XElement(rels+"Relationships",new XElement(rels+"Relationship",new XAttribute("Id","rId1"),new XAttribute("Type",office.NamespaceName+"/officeDocument"),new XAttribute("Target","xl/workbook.xml")))));
    WriteEntry(zip,"xl/workbook.xml",new XDocument(new XElement(S+"workbook",new XAttribute(XNamespace.Xmlns+"r",office),new XElement(S+"sheets",new XElement(S+"sheet",new XAttribute("name","Sheet1"),new XAttribute("sheetId",1),new XAttribute(office+"id","rId1"))))));
    WriteEntry(zip,"xl/_rels/workbook.xml.rels",new XDocument(new XElement(rels+"Relationships",new XElement(rels+"Relationship",new XAttribute("Id","rId1"),new XAttribute("Type",office.NamespaceName+"/worksheet"),new XAttribute("Target","worksheets/sheet1.xml")))));
    var data=new XElement(S+"sheetData");var all=new[]{headers}.Concat(rows).ToArray();for(int i=0;i<all.Length;i++){var row=new XElement(S+"row",new XAttribute("r",i+1));for(int c=0;c<all[i].Length;c++)row.Add(new XElement(S+"c",new XAttribute("r",Address(c)+(i+1)),new XAttribute("t","inlineStr"),new XElement(S+"is",new XElement(S+"t",new XAttribute(XNamespace.Xml+"space","preserve"),all[i][c]??""))));data.Add(row);}
    WriteEntry(zip,"xl/worksheets/sheet1.xml",new XDocument(new XElement(S+"worksheet",new XElement(S+"sheetViews",new XElement(S+"sheetView",new XAttribute("workbookViewId",0),new XElement(S+"pane",new XAttribute("ySplit",1),new XAttribute("topLeftCell","A2"),new XAttribute("state","frozen")))),data)));
   }
  }
  // Excel clipboard fields can contain quoted tabs/newlines and doubled quotes.
  public static string[][] ClipboardRows(string text){
   var rows=new List<string[]>();var cells=new List<string>();var field=new StringBuilder();bool quoted=false;
   for(int i=0;i<text.Length;i++){char c=text[i];if(c=='"'){if(quoted&&i+1<text.Length&&text[i+1]=='"'){field.Append('"');i++;}else if(quoted||field.Length==0)quoted=!quoted;else field.Append(c);}else if(!quoted&&(c=='\t'||c=='\r'||c=='\n')){cells.Add(field.ToString());field.Clear();if(c!='\t'){rows.Add(cells.ToArray());cells.Clear();if(c=='\r'&&i+1<text.Length&&text[i+1]=='\n')i++;}}else field.Append(c);}
   if(quoted)throw new FormatException("粘贴内容引号未闭合。");if(field.Length>0||cells.Count>0){cells.Add(field.ToString());rows.Add(cells.ToArray());}return rows.ToArray();
  }
 }
}
