using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 [CustomEditor(typeof(PlayerModule))]
 public sealed class PlayerModuleEditor : UnityEditor.Editor {
  public override void OnInspectorGUI(){DrawDefaultInspector();var p=(PlayerModule)target;if(!Application.isPlaying||p.session==null||p.State==null)return;var s=p.State;EditorGUILayout.LabelField("状态",s.Statuses.Summary);EditorGUILayout.LabelField("生命 / 上限",s.Health+" / "+s.MaxHealth);EditorGUILayout.LabelField("护盾 / 增伤",s.Shield+" / "+s.Power);EditorGUILayout.LabelField("牌组 / 手牌 / 金币",s.Deck.Count+" / "+s.Hand.Count+" / "+s.Coins);Repaint();}
 }
 [CustomEditor(typeof(EnemyModule))]
 public sealed class EnemyModuleEditor : UnityEditor.Editor {
  public override void OnInspectorGUI(){DrawDefaultInspector();var e=(EnemyModule)target;if(!Application.isPlaying||e.session==null||e.State==null)return;var s=e.State;EditorGUILayout.LabelField("状态",s.Statuses.Summary);EditorGUILayout.LabelField("生命 / 上限",s.Health+" / "+s.MaxHealth);EditorGUILayout.LabelField("护盾 / 增伤",s.Shield+" / "+s.Power);EditorGUILayout.LabelField("Boss",s.Boss.ToString());foreach(var rule in s.ActiveRules)EditorGUILayout.LabelField("已激活敌方牌序",rule.Pattern+" / "+rule.Description);Repaint();}
 }
}
