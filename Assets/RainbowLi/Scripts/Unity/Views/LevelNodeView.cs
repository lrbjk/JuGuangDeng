using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace RainbowLi.UnityGame {
 public sealed class LevelNodeView : MonoBehaviour {
  public int nodeId,authoredFloor,authoredLane;
  public Button button;
  public Text label;
  public Image face;
  private UnityAction listener;
  public void Bind(LevelNodeData node,RouteRun run,Action click){
   if(listener!=null)button.onClick.RemoveListener(listener);listener=()=>click();button.onClick.AddListener(listener);
   bool visited=run.Completed.Contains(node.id),available=run.CanEnter(node.id);
   button.interactable=available;
   string name=node.kind==LevelKind.Combat?"战斗":node.kind==LevelKind.Elite?"精英":node.kind==LevelKind.Rest?"休息":node.kind==LevelKind.Shop?"商店":node.kind==LevelKind.Event?"事件":"BOSS";
   label.text=name+"\n"+(visited?"已完成":available?"可选择":"未解锁");
   face.color=visited?new Color(.42f,.37f,.22f):available?new Color(.15f,.48f,.36f):new Color(.19f,.24f,.23f);
  }
 }
}
