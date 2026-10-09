using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
[CustomEditor(typeof(GameSession))]
public sealed class GameSessionEditor : UnityEditor.Editor {
 public override void OnInspectorGUI(){
  DrawDefaultInspector();var s=(GameSession)target;
  if(!Application.isPlaying||s.Game==null){EditorGUILayout.HelpBox("配置静态数据请编辑 GameDefinition。UI 布局在 GameCanvas 下编辑，卡牌样式编辑 PokerCard Prefab。",MessageType.Info);return;}
  var g=s.Game;EditorGUILayout.Space();EditorGUILayout.LabelField("实时状态",EditorStyles.boldLabel);
  EditorGUILayout.LabelField("阶段 / 回合",g.Phase+" / "+g.Round);
  EditorGUILayout.LabelField("玩家生命 / 护盾 / 增伤",g.Health+" / "+g.Shield+" / "+g.Power);
  EditorGUILayout.LabelField("敌人生命 / 护盾 / 增伤",g.EnemyHealth+" / "+g.EnemyShield+" / "+g.EnemyPower);
  EditorGUILayout.LabelField("牌组 / 手牌 / 金币",g.Deck.Count+" / "+g.Hand.Count+" / "+g.Coins);
  EditorGUILayout.HelpBox(g.LastAction,MessageType.None);
  if(GUILayout.Button("重新开始当前游戏"))s.NewGame();
  Repaint();
 }
}}
