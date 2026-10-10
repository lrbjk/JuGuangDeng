using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi {
 public enum LevelKind { Combat, Elite, Rest, Shop, Event, Boss }
 [Serializable] public sealed class LevelNodeData {
  public int id,floor,lane;
  public LevelKind kind;
  public List<int> next=new List<int>();
  public LevelNodeData Copy()=>new LevelNodeData{id=id,floor=floor,lane=lane,kind=kind,next=new List<int>(next)};
 }
 public sealed class RouteGraph {
  public readonly List<LevelNodeData> Nodes;
  public RouteGraph(IEnumerable<LevelNodeData> nodes){Nodes=nodes.Select(n=>n.Copy()).ToList();Validate();}
  public LevelNodeData Find(int id)=>Nodes.Find(n=>n.id==id);
  public int Floors=>Nodes.Max(n=>n.floor)+1;
  public void Validate(){
   if(Nodes.Count==0||Nodes.Any(n=>n.floor<0||n.lane<0)||Nodes.Select(n=>n.id).Distinct().Count()!=Nodes.Count)throw new ArgumentException("Invalid node coordinates or IDs");
   int top=Nodes.Max(n=>n.floor);if(Nodes.Count(n=>n.kind==LevelKind.Boss)!=1||Nodes.Single(n=>n.kind==LevelKind.Boss).floor!=top||Nodes.Count(n=>n.floor==top)!=1)throw new ArgumentException("Final floor must have one boss");
   foreach(var n in Nodes){if(n.floor<top&&n.next.Count==0)throw new ArgumentException("Dead end at "+n.id);foreach(int id in n.next){var to=Find(id);if(to==null||to.floor!=n.floor+1)throw new ArgumentException("Connections must advance one floor");}if(n.floor==top&&n.next.Count>0)throw new ArgumentException("Boss is terminal");}
   var reachable=new HashSet<int>(Nodes.Where(n=>n.floor==0).Select(n=>n.id));
   foreach(var n in Nodes.OrderBy(n=>n.floor))if(reachable.Contains(n.id))foreach(int id in n.next)reachable.Add(id);
   if(reachable.Count!=Nodes.Count)throw new ArgumentException("Unreachable nodes in route");
  }
 }
 public static class RouteGenerator {
  public static RouteGraph Generate(int seed,int floors=8,int lanes=3)=>Generate(new RecoverableRandom(seed),floors,lanes);
  public static RouteGraph Generate(Random random,int floors=8,int lanes=3){
   if(floors<4||floors>16||lanes<2||lanes>5)throw new ArgumentOutOfRangeException("Route size");
   var nodes=new List<LevelNodeData>();
   for(int row=0;row<floors-1;row++)for(int lane=0;lane<lanes;lane++){
    int roll=random.Next(100);LevelKind kind=roll<50?LevelKind.Combat:roll<65?LevelKind.Rest:roll<80?LevelKind.Shop:roll<90?LevelKind.Event:LevelKind.Elite;
    if(row==0)kind=LevelKind.Combat;
    else if(row==1)kind=(LevelKind)(2+lane%3);
    else if(row==floors-2)kind=LevelKind.Rest;
    else if(row==floors/2&&lane==lanes/2)kind=LevelKind.Elite;
    nodes.Add(new LevelNodeData{id=row*lanes+lane,floor=row,lane=lane,kind=kind});
   }
   int bossId=(floors-1)*lanes+lanes/2;nodes.Add(new LevelNodeData{id=bossId,floor=floors-1,lane=lanes/2,kind=LevelKind.Boss});
   foreach(var n in nodes.Where(n=>n.floor<floors-1)){
    if(n.floor==floors-2)n.next.Add(bossId);
    else{n.next.Add((n.floor+1)*lanes+n.lane);int other=n.lane+(n.lane==0?1:n.lane==lanes-1?-1:random.Next(2)*2-1);n.next.Add((n.floor+1)*lanes+other);}
   }
   return new RouteGraph(nodes);
  }
 }
 public sealed class RouteRun {
  public RouteGraph Graph {get;}
  public int CurrentId {get;private set;}=-1;
  public int ActiveId {get;private set;}=-1;
  public bool Finished {get;private set;}
  public bool Failed {get;private set;}
  public readonly HashSet<int> Completed=new HashSet<int>();
  public RouteRun(RouteGraph graph){Graph=graph;}
  public static RouteRun Restore(RouteGraph graph,int current,int active,int[] completed,bool finished,bool failed){var run=new RouteRun(graph);if(current!=-1&&graph.Find(current)==null||active!=-1&&graph.Find(active)==null)throw new ArgumentException("Invalid saved route node");run.CurrentId=current;run.Completed.UnionWith(completed??Array.Empty<int>());if(run.Completed.Any(id=>graph.Find(id)==null))throw new ArgumentException("Invalid completed node");if(active>=0&&!run.CanEnter(active))throw new ArgumentException("Invalid active node");run.ActiveId=active;run.Finished=finished;run.Failed=failed;return run;}
  public bool CanEnter(int id){var n=Graph.Find(id);return n!=null&&!Finished&&!Failed&&ActiveId<0&&!Completed.Contains(id)&&(CurrentId<0?n.floor==0:Graph.Find(CurrentId).next.Contains(id));}
  public bool Enter(int id){if(!CanEnter(id))return false;ActiveId=id;return true;}
  public bool Complete(){if(ActiveId<0||Failed)return false;CurrentId=ActiveId;Completed.Add(ActiveId);Finished=Graph.Find(ActiveId).kind==LevelKind.Boss;ActiveId=-1;return true;}
  public void Fail(){Failed=true;}
 }
}
