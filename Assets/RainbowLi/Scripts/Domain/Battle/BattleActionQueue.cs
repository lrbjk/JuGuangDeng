using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi {
 // Every insertion, including reciprocal behaviors and status numbers, shares one budget.
 public sealed class BattleActionQueue : IReadOnlyList<BattleAction> {
  private readonly List<BattleAction> actions=new List<BattleAction>();
  private int budget=4096;
  public int Budget {get=>budget;set {budget=Math.Max(0,value);if(actions.Count>budget){actions.RemoveRange(budget,actions.Count-budget);Truncated=true;}}}
  public bool Truncated {get;private set;}
  public int Count=>actions.Count;public BattleAction this[int index]=>actions[index];
  public void Add(BattleAction action)=>Insert(Count,action);
  public void AddRange(IEnumerable<BattleAction> added)=>InsertRange(Count,added);
  public void Insert(int index,BattleAction action)=>InsertRange(index,new[]{action});
  public void InsertRange(int index,IEnumerable<BattleAction> added){var list=added.Take(Math.Max(0,Budget-Count)+1).ToList();int room=Math.Max(0,Budget-Count);if(list.Count>room){list.RemoveAt(list.Count-1);Truncated=true;}actions.InsertRange(index,list);}
  public void RemoveAt(int index)=>actions.RemoveAt(index);
  public void Clear(){actions.Clear();Truncated=false;}
  public IEnumerator<BattleAction> GetEnumerator()=>actions.GetEnumerator();IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
 }
}
