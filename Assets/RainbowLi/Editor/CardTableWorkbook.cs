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
  public static readonly string[] Headers={"效果编号","效果名称","效果类型","效果类型","具体效果","基础取值范围（存在X时）","备注","启用","数字","权重","起始牌","特殊技能","触发条件","阈值","特殊权重比","结算效果","卡效所属池"};
  public static readonly string[] Lifetimes={"其他类型效果","临时性效果","持续性效果"};
  public static readonly string[] Polarities={"正面效果","负面效果","特殊效果"};
  static readonly XNamespace S="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
  static string Cell(string[] cells,int index,string fallback="") => index<cells.Length?cells[index]??fallback:fallback;
  static int Int(string s,int fallback){if(string.IsNullOrWhiteSpace(s))return fallback;if(double.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out var d)&&d==Math.Truncate(d)&&d>=int.MinValue&&d<=int.MaxValue)return (int)d;throw new FormatException("不是整数："+s);}
  static float Float(string s,float fallback)=>string.IsNullOrWhiteSpace(s)?fallback:float.Parse(s,CultureInfo.InvariantCulture);
  static bool Bool(string s,bool fallback){if(string.IsNullOrWhiteSpace(s))return fallback;if(s=="1"||s=="是"||s.Equals("true",StringComparison.OrdinalIgnoreCase))return true;if(s=="0"||s=="否"||s.Equals("false",StringComparison.OrdinalIgnoreCase))return false;throw new FormatException("开关须为 1/0 或 是/否："+s);}
  public static void SetRange(CardTableRow row,string text){CardTableRange.Parse(text,out row.usesNumber,out row.min,out row.max);row.range=row.usesNumber?"为卡牌对应数字":text.Trim();}
  static T Field<T>(string name,Func<T> read){try{return read();}catch(FormatException e){throw new FormatException(name+"："+e.Message,e);}catch(OverflowException e){throw new FormatException(name+"：数字太大。",e);}}
  // The source design sheet has nine columns; editor exports retain the legacy
  // configuration columns and append membership so old exports still import.
  public static string[][] CanonicalRows(IEnumerable<string[]> source){
   var rows=source.ToArray();var header=rows.FirstOrDefault(r=>Cell(r,0)=="效果编号");
   if(header==null||Cell(header,6)!="卡效所属池")return rows;
   if(Cell(header,4)!="具体效果"||Cell(header,7)!="权重"||Cell(header,8)!="备注")throw new FormatException("卡效设计表缺少具体效果、权重或备注列。");
   return rows.Select(r=>{if(Cell(r,0)=="效果编号")return Headers;if(r.All(string.IsNullOrWhiteSpace))return r;var c=new string[Headers.Length];for(int i=0;i<6;i++)c[i]=Cell(r,i);c[6]=Cell(r,8);c[9]=Cell(r,7);c[16]=Cell(r,6);return c;}).ToArray();
  }
  public static CardTableRow[] Parse(IEnumerable<string[]> source,IEnumerable<CardTableRow> known){
   var lookup=(known??Array.Empty<CardTableRow>()).Where(r=>r!=null).GroupBy(r=>r.id).ToDictionary(g=>g.Key,g=>g.First());
   var result=new List<CardTableRow>();int line=0;
   foreach(var cells in CanonicalRows(source)){line++;if(cells.All(string.IsNullOrWhiteSpace)||Cell(cells,0)=="效果编号")continue;
    try{
     if(cells.Length<7)throw new FormatException("至少需要原表的七列。");
     var id=Cell(cells,0).Trim();lookup.TryGetValue(id,out var previous);var row=previous?.Copy()??new CardTableRow();row.id=id;row.name=Cell(cells,1);row.description=Cell(cells,4);row.notes=Cell(cells,6);
     row.pools=Cell(cells,16,previous?.pools);
     int life=Array.IndexOf(Lifetimes,Cell(cells,2)),polarity=Array.IndexOf(Polarities,Cell(cells,3));if(life<0||polarity<0)throw new FormatException("效果类型须使用原表中的类型名称。");row.lifetime=(EffectLifetime)life;row.polarity=(EffectPolarity)polarity;SetRange(row,Cell(cells,5));
     row.enabled=Field("启用",()=>Bool(Cell(cells,7),previous?.enabled??true));row.number=Field("数字",()=>Int(Cell(cells,8),previous?.number??1));row.weight=Field("权重",()=>Float(Cell(cells,9),previous?.weight??10));row.starting=Field("起始牌",()=>Bool(Cell(cells,10),previous?.starting??true));row.special=Field("特殊技能",()=>Bool(Cell(cells,11),previous?.special??false));
     string condition=Cell(cells,12);row.condition=string.IsNullOrWhiteSpace(condition)?previous?.condition??ActivationCondition.RoundAtLeast:condition=="生命百分比"||condition=="HealthPercentAtMost"?ActivationCondition.HealthPercentAtMost:condition=="回合"||condition=="RoundAtLeast"?ActivationCondition.RoundAtLeast:throw new FormatException("触发条件须为 回合 或 生命百分比。");
     row.threshold=Field("阈值",()=>Int(Cell(cells,13),previous?.threshold??3));row.specialWeightRatio=Field("特殊权重比",()=>Float(Cell(cells,14),previous?.specialWeightRatio??.5f));
     string kind=Cell(cells,15);if(!string.IsNullOrWhiteSpace(kind)){if(!Enum.TryParse(kind,out CardEffect effect)||!Enum.IsDefined(typeof(CardEffect),effect))throw new FormatException("未知结算效果："+kind);row.effect=effect;}else if(previous==null)throw new FormatException("新编号需要填写结算效果列；请复制已有行或使用导出的模板。");
     result.Add(row);
    }catch(Exception e){throw new FormatException("第 "+line+" 行："+e.Message,e);}
   }
   return result.ToArray();
  }
  public static string[] Cells(CardTableRow r)=>new[]{r.id,r.name,Lifetimes[(int)r.lifetime],Polarities[(int)r.polarity],r.description,r.range,r.notes,r.enabled?"1":"0",r.number.ToString(),r.weight.ToString(CultureInfo.InvariantCulture),r.starting?"1":"0",r.special?"1":"0",r.condition==ActivationCondition.RoundAtLeast?"回合":"生命百分比",r.threshold.ToString(),r.specialWeightRatio.ToString(CultureInfo.InvariantCulture),r.effect.ToString(),r.pools};
  static XDocument Document(ZipArchive zip,string name){var entry=zip.GetEntry(name)??throw new FormatException("缺少 XLSX 内容："+name);using(var stream=entry.Open())using(var reader=XmlReader.Create(stream,new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=20000000}))return XDocument.Load(reader);}
  static int Column(string address){int n=0;foreach(char c in address){if(!char.IsLetter(c))break;n=n*26+char.ToUpperInvariant(c)-'A'+1;}return n-1;}
  public static string[][] Read(string path,int columnCount=17)=>ReadSheet(path,null,columnCount);
  public static string[][] ReadSheet(string path,string sheetName,int columnCount=17){
   using(var zip=ZipFile.OpenRead(path)){
    var book=Document(zip,"xl/workbook.xml");var sheets=book.Descendants(S+"sheet").ToArray();var sheet=sheetName==null?sheets.FirstOrDefault(e=>(string)e.Attribute("name")=="Sheet1")??sheets.FirstOrDefault():sheets.FirstOrDefault(e=>(string)e.Attribute("name")==sheetName);
    if(sheet==null){if(sheetName!=null)return null;throw new FormatException("工作簿没有工作表。");}
    XNamespace relationships="http://schemas.openxmlformats.org/officeDocument/2006/relationships";var id=(string)sheet.Attribute(relationships+"id");var rel=Document(zip,"xl/_rels/workbook.xml.rels").Root.Elements().First(e=>(string)e.Attribute("Id")==id);if((string)rel.Attribute("TargetMode")=="External")throw new FormatException("不能读取外部工作表。");
    var uri=new Uri(new Uri("http://local/xl/workbook.xml"),(string)rel.Attribute("Target"));var sheetPath=Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
    var shared=zip.GetEntry("xl/sharedStrings.xml")==null?Array.Empty<string>():Document(zip,"xl/sharedStrings.xml").Descendants(S+"si").Select(e=>string.Concat(e.Descendants(S+"t").Select(t=>t.Value))).ToArray();
    var result=new List<string[]>();foreach(var row in Document(zip,sheetPath).Descendants(S+"row")){
     int rowNumber=Int((string)row.Attribute("r"),result.Count+1);if(rowNumber<1||rowNumber>50000||rowNumber<=result.Count)throw new FormatException("工作表行号无效或超过 50000 行。");
     while(result.Count<rowNumber)result.Add(new string[columnCount]);var cells=result[rowNumber-1];
     foreach(var cell in row.Elements(S+"c")){string address=(string)cell.Attribute("r")??"";int col=Column(address);if(col<0||col>=cells.Length)continue;if(cell.Element(S+"f")!=null)throw new FormatException((string)sheet.Attribute("name")+" / "+address+"：公式请粘贴为值后导入。");string value=(string)cell.Element(S+"v")??"";string type=(string)cell.Attribute("t");cells[col]=type=="s"?shared[Int(value,0)]:type=="inlineStr"?string.Concat(cell.Descendants(S+"t").Select(e=>e.Value)):value;}
    }return result.ToArray();
   }
  }
  static void WriteEntry(ZipArchive zip,string name,XDocument doc){using(var stream=zip.CreateEntry(name).Open())doc.Save(stream);}
  static string Address(int c){string a="";for(c++;c>0;c=(c-1)/26)a=(char)('A'+(c-1)%26)+a;return a;}
  public static readonly string[] CompoundHeaders={"卡牌编号","执行序号","结算效果","持续方式","性质","效果描述","基础取值范围","启用"};
  public static void Write(string path,IEnumerable<CardTableRow> rows){var data=rows.ToArray();WriteSheets(path,new[]{"Sheet1","复合效果"},new[]{Headers,CompoundHeaders},new[]{data.Select(Cells).ToArray(),data.SelectMany(r=>(r.additionalEffects??Array.Empty<CardTableEffect>()).Select((e,i)=>new[]{r.id,(i+2).ToString(),e.effect.ToString(),Lifetimes[(int)e.lifetime],Polarities[(int)e.polarity],e.description,e.range,e.enabled?"1":"0"})).ToArray()});}
  public static void WriteRows(string path,string[] headers,IEnumerable<string[]> rows)=>WriteSheets(path,new[]{"Sheet1"},new[]{headers},new[]{rows.ToArray()});
  public static void WriteSheets(string path,string[] names,string[][] headers,string[][][] rows){
   using(var zip=ZipFile.Open(path,ZipArchiveMode.Create)){
    XNamespace types="http://schemas.openxmlformats.org/package/2006/content-types",rels="http://schemas.openxmlformats.org/package/2006/relationships",office="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    var content=new XElement(types+"Types",new XElement(types+"Default",new XAttribute("Extension","rels"),new XAttribute("ContentType","application/vnd.openxmlformats-package.relationships+xml")),new XElement(types+"Default",new XAttribute("Extension","xml"),new XAttribute("ContentType","application/xml")),new XElement(types+"Override",new XAttribute("PartName","/xl/workbook.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")));
    var sheets=new XElement(S+"sheets");var links=new XElement(rels+"Relationships");
    for(int n=0;n<names.Length;n++){
     string name="sheet"+(n+1)+".xml",id="rId"+(n+1);content.Add(new XElement(types+"Override",new XAttribute("PartName","/xl/worksheets/"+name),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));
     sheets.Add(new XElement(S+"sheet",new XAttribute("name",names[n]),new XAttribute("sheetId",n+1),new XAttribute(office+"id",id)));links.Add(new XElement(rels+"Relationship",new XAttribute("Id",id),new XAttribute("Type",office.NamespaceName+"/worksheet"),new XAttribute("Target","worksheets/"+name)));
     var data=new XElement(S+"sheetData");var all=new[]{headers[n]}.Concat(rows[n]).ToArray();for(int i=0;i<all.Length;i++){var row=new XElement(S+"row",new XAttribute("r",i+1));for(int c=0;c<all[i].Length;c++)row.Add(new XElement(S+"c",new XAttribute("r",Address(c)+(i+1)),new XAttribute("t","inlineStr"),new XElement(S+"is",new XElement(S+"t",new XAttribute(XNamespace.Xml+"space","preserve"),all[i][c]??""))));data.Add(row);}
     WriteEntry(zip,"xl/worksheets/"+name,new XDocument(new XElement(S+"worksheet",new XElement(S+"sheetViews",new XElement(S+"sheetView",new XAttribute("workbookViewId",0),new XElement(S+"pane",new XAttribute("ySplit",1),new XAttribute("topLeftCell","A2"),new XAttribute("state","frozen")))),data)));
    }
    WriteEntry(zip,"[Content_Types].xml",new XDocument(content));WriteEntry(zip,"_rels/.rels",new XDocument(new XElement(rels+"Relationships",new XElement(rels+"Relationship",new XAttribute("Id","rId1"),new XAttribute("Type",office.NamespaceName+"/officeDocument"),new XAttribute("Target","xl/workbook.xml")))));
    WriteEntry(zip,"xl/workbook.xml",new XDocument(new XElement(S+"workbook",new XAttribute(XNamespace.Xmlns+"r",office),sheets)));WriteEntry(zip,"xl/_rels/workbook.xml.rels",new XDocument(links));
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
