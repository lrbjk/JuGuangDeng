using UnityEngine;
using System.Linq;
namespace RainbowLi.UnityGame {
 public sealed class LevelModule : MonoBehaviour {
  public GameSession session;
  public LevelDefinition definition;
  public RouteRun Run {get;private set;}
  private ContainmentGame observed;
  private void OnEnable(){session.Changed+=Track;}
  private void OnDisable(){session.Changed-=Track;}
  private void Start(){Track();session.Notify();}
  private void Track(){
   if(session.Game==null)return;
   if(observed!=session.Game){observed=session.Game;int seed=session.seed==0?System.Environment.TickCount:session.seed;if(definition==null)session.Game.ConfigureGeneratedRoute(independentStacks:true);else if(definition.randomizeEachRun&&definition.seed==0)session.Game.ConfigureGeneratedRoute(definition.floors,definition.lanes,definition.independentStacks);else session.Game.ConfigureRoute(definition.CreateGraph(seed));Run=session.Game.Route;if(!Run.BossUnlockByBattles)Run.ConfigureBossUnlock(definition==null?1:definition.adventureLayer);}
   if(session.Game.Phase==GamePhase.Lost)Run.Fail();
   if(session.Game.Phase==GamePhase.Won&&Run.ActiveId>=0)Run.Complete();
  }
  public void SelectBoss(){Track();var boss=Run?.Graph.Nodes.FirstOrDefault(n=>n.kind==LevelKind.Boss);if(boss!=null)SelectNode(boss.id);}
  public void SelectNode(int id){
   Track();var g=session.Game;if(g.Phase!=GamePhase.Map||!Run.CanEnter(id))return;
   var node=Run.Graph.Find(id);
   if(!Run.Enter(id))return;
   if(node.kind==LevelKind.Rest){g.RestAvailable=true;g.OpenService(false);}
   else if(node.kind==LevelKind.Shop)g.OpenService(true);
   else if(node.kind==LevelKind.Event)g.OpenRouteEvent();
   else{g.StartBattle(node.kind==LevelKind.Boss,node.kind==LevelKind.Elite,true);if(node.kind==LevelKind.Elite){g.EnemyMaxHealth=g.EnemyHealth=Mathf.CeilToInt(g.EnemyHealth*(definition==null?1.5f:definition.eliteHealthMultiplier));g.EliteAttackBonus=definition==null?3:definition.eliteAttackBonus;g.EliteGold=definition==null?40:definition.eliteGold;foreach(var a in g.Timeline)if(a.Effect==CardEffect.Strike)a.Value+=g.EliteAttackBonus;}}
   session.Notify();
  }
  public void CompleteNode(){if(session.Game.Phase==GamePhase.Map&&Run!=null&&Run.ActiveId>=0){Run.Complete();session.Notify();}}
  public void Save(){session.SaveGame();}
  public void Continue(){session.ContinueGame();}
  public void EventGold(){ResolveEvent(false);}
  public void EventHeal(){ResolveEvent(true);}
  private void ResolveEvent(bool heal){if(session.Game.Phase!=GamePhase.Event||Run==null||Run.ActiveId<0)return;session.Game.ResolveRouteEvent(heal,definition==null?(heal?16:18):(heal?definition.eventHealing:definition.eventGold));CompleteNode();}
 }
}
