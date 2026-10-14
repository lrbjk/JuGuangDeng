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
  public bool Stacked {get;}
  public bool CardDungeon {get;}
  public bool IndependentStacks {get;}
  public int StartLane {get;}
  public RouteGraph(IEnumerable<LevelNodeData> nodes,bool stacked=false,bool cardDungeon=false,int startLane=-1,bool independentStacks=false){IndependentStacks=independentStacks;CardDungeon=cardDungeon&&!independentStacks;Stacked=stacked||cardDungeon||independentStacks;Nodes=nodes.Select(n=>n.Copy()).ToList();StartLane=startLane<0?(Nodes.Count==0?0:Nodes.Max(n=>n.lane)/2):startLane;if(Stacked)foreach(var n in Nodes)n.next.Clear();Validate();}
  public LevelNodeData Find(int id)=>Nodes.Find(n=>n.id==id);
  public int Floors=>Nodes.Max(n=>n.floor)+1;
  public void Validate(){
   if(Nodes.Count==0||Nodes.Any(n=>n.floor<0||n.lane<0)||Nodes.Select(n=>n.id).Distinct().Count()!=Nodes.Count)throw new ArgumentException("Invalid node coordinates or IDs");
   int top=Nodes.Max(n=>n.floor);if(Nodes.Count(n=>n.kind==LevelKind.Boss)!=1||Nodes.Single(n=>n.kind==LevelKind.Boss).floor!=top||Nodes.Count(n=>n.floor==top)!=1)throw new ArgumentException("Final floor must have one boss");
   if(Stacked){if(Nodes.Any(n=>n.id<0)||Nodes.GroupBy(n=>new {n.floor,n.lane}).Any(g=>g.Count()>1))throw new ArgumentException("Duplicate stack positions or negative IDs");if(IndependentStacks&&(!Nodes.Where(n=>n.kind!=LevelKind.Boss).Select(n=>n.lane).Distinct().OrderBy(n=>n).SequenceEqual(new[]{0,1,2})))throw new ArgumentException("Independent stacks require exactly three lanes (0, 1, 2)");if(CardDungeon){if(StartLane<0||StartLane>Nodes.Max(n=>n.lane)||!Nodes.Any(n=>n.floor==0&&Math.Abs(n.lane-StartLane)<=1))throw new ArgumentException("No reachable starting card");foreach(var n in Nodes.Where(n=>n.kind!=LevelKind.Boss))if(!Nodes.Any(v=>v.floor==n.floor+1&&(v.kind==LevelKind.Boss||Math.Abs(v.lane-n.lane)<=1)))throw new ArgumentException("Dungeon dead end at "+n.id);}return;}
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
  public int ChosenLane {get;private set;}=-1;
  public int PlayerFloor {get;private set;}=-1;
  public int PlayerLane {get;private set;}
  public int AdventureLayer {get;private set;}=1;
  public bool BossUnlockByBattles {get;private set;}
  public int CompletedBattleCount=>Completed.Count(id=>{var n=Graph.Find(id);return n!=null&&(n.kind==LevelKind.Combat||n.kind==LevelKind.Elite);});
  public int BossRequiredBattles=>CombatResolver.Clamp(5L+AdventureLayer);
  public bool BossUnlocked=>BossUnlockByBattles&&CompletedBattleCount>=BossRequiredBattles;
  public bool CanStartBossBattle=>BossUnlocked&&!Finished&&!Failed&&(ActiveId<0||Graph.Find(ActiveId)?.kind==LevelKind.Boss);
  public void ConfigureBossUnlock(int adventureLayer=1){AdventureLayer=Math.Max(1,adventureLayer);BossUnlockByBattles=true;}
  public readonly HashSet<int> Completed=new HashSet<int>();
  public RouteRun(RouteGraph graph){Graph=graph;PlayerLane=graph.StartLane;}
  public static RouteRun Restore(RouteGraph graph,int current,int active,int[] completed,bool finished,bool failed,bool bossUnlockByBattles=false,int adventureLayer=1){
   var run=new RouteRun(graph);if(bossUnlockByBattles)run.ConfigureBossUnlock(adventureLayer);
   if(current!=-1&&graph.Find(current)==null||active!=-1&&graph.Find(active)==null)throw new ArgumentException("Invalid saved route node");
   run.CurrentId=current;run.Completed.UnionWith(completed??Array.Empty<int>());
   if(run.Completed.Any(id=>graph.Find(id)==null))throw new ArgumentException("Invalid completed node");
   if(graph.CardDungeon&&current>=0){var node=graph.Find(current);run.PlayerFloor=node.floor;run.PlayerLane=node.lane;}
   if(graph.Stacked&&!graph.CardDungeon&&!graph.IndependentStacks){var first=run.Completed.Select(graph.Find).Where(n=>n.kind!=LevelKind.Boss).OrderBy(n=>n.floor).FirstOrDefault();if(first!=null)run.ChosenLane=first.lane;}
   if(active>=0&&!run.Enter(active))throw new ArgumentException("Invalid active node");
   run.Finished=finished;run.Failed=failed;return run;
  }
  public bool IsAbandoned(int id){var n=Graph.Find(id);return n!=null&&!Graph.IndependentStacks&&(Graph.CardDungeon?n.floor<=PlayerFloor&&!Completed.Contains(id):Graph.Stacked&&ChosenLane>=0&&n.kind!=LevelKind.Boss&&n.lane!=ChosenLane);}
  public bool IsCovered(int id){var n=Graph.Find(id);if(n!=null&&n.kind==LevelKind.Boss&&BossUnlockByBattles)return !BossUnlocked;if(n==null||!Graph.Stacked)return false;if(Graph.CardDungeon)return n.floor>PlayerFloor+1;return Graph.Nodes.Any(v=>v.kind!=LevelKind.Boss&&!IsAbandoned(v.id)&&!Completed.Contains(v.id)&&(n.kind==LevelKind.Boss||v.lane==n.lane&&v.floor<n.floor));}
  public bool CanEnter(int id){var n=Graph.Find(id);if(n!=null&&n.kind==LevelKind.Boss&&BossUnlockByBattles)return !Finished&&!Failed&&ActiveId<0&&!Completed.Contains(id)&&BossUnlocked;return n!=null&&!Finished&&!Failed&&ActiveId<0&&!Completed.Contains(id)&&!IsAbandoned(id)&&(Graph.CardDungeon?n.floor==PlayerFloor+1&&(n.kind==LevelKind.Boss||Math.Abs(n.lane-PlayerLane)<=1):Graph.Stacked?!IsCovered(id):(CurrentId<0?n.floor==0:Graph.Find(CurrentId).next.Contains(id)));}
  public bool Enter(int id){if(!CanEnter(id))return false;ActiveId=id;if(Graph.Stacked&&!Graph.CardDungeon&&!Graph.IndependentStacks&&ChosenLane<0&&Graph.Find(id).kind!=LevelKind.Boss)ChosenLane=Graph.Find(id).lane;return true;}
  public bool Complete(){if(ActiveId<0||Failed)return false;CurrentId=ActiveId;Completed.Add(ActiveId);var n=Graph.Find(ActiveId);if(Graph.CardDungeon){PlayerFloor=n.floor;PlayerLane=n.lane;}Finished=n.kind==LevelKind.Boss;ActiveId=-1;ExtendIndependentStackIfBossLocked(n.lane);ExtendDungeonIfBossLocked();return true;}
  // Each lane is a queue; completed cards are removed only from their own queue.
  public LevelNodeData FrontNode(int lane)=>Graph.Nodes.Where(n=>n.kind!=LevelKind.Boss&&n.lane==lane&&!Completed.Contains(n.id)).OrderBy(n=>n.floor).FirstOrDefault();
  private void ExtendIndependentStackIfBossLocked(int lane){
   if(!Graph.IndependentStacks||!BossUnlockByBattles||BossUnlocked||Finished||Failed||FrontNode(lane)!=null)return;
   var boss=Graph.Nodes.First(n=>n.kind==LevelKind.Boss);
   int floor=Graph.Nodes.Where(n=>n.kind!=LevelKind.Boss&&n.lane==lane).Max(n=>n.floor)+1;
   boss.floor=Math.Max(boss.floor,floor+1);
   Graph.Nodes.Add(new LevelNodeData{id=Graph.Nodes.Max(n=>n.id)+1,floor=floor,lane=lane,kind=LevelKind.Combat});
  }
  private void ExtendDungeonIfBossLocked(){
   if(!Graph.CardDungeon||!BossUnlockByBattles||BossUnlocked||Finished||Failed)return;
   var boss=Graph.Nodes.First(n=>n.kind==LevelKind.Boss);if(PlayerFloor+1!=boss.floor)return;
   int floor=boss.floor,lanes=Graph.Nodes.Max(n=>n.lane)+1,id=Graph.Nodes.Max(n=>n.id)+1;boss.floor++;
   for(int lane=0;lane<lanes;lane++)Graph.Nodes.Add(new LevelNodeData{id=id++,floor=floor,lane=lane,kind=LevelKind.Combat});
  }
  public void Fail(){Failed=true;}
 }
}
