using UnityEngine;
namespace RainbowLi.UnityGame {
[CreateAssetMenu(menuName="RainbowLi/铭刻定义")]
public sealed class EngravingDefinition : ScriptableObject {
 [Min(0)] public int extraDamage = 5;
 [TextArea] public string description = "卡牌完成牌序时追加伤害";
 public EngravingEffectTable effectTable;
 public string effectId;
 [Tooltip("0 使用配表范围下限；非零值必须在该行范围内")][Min(0)] public int effectX;
 public RuntimeEngraving CreateEffect(){if(effectTable==null||string.IsNullOrWhiteSpace(effectId))return new RuntimeEngraving {Code="LegacyEngraving",Effect=EngravingEffect.FollowUpDamage,BaseX=extraDamage};var row=effectTable.Find(effectId);row.TryRange(out int min,out _);return effectTable.CreateEffect(effectId,effectX);}
 public int ResolveExtraDamage(){if(effectTable==null||string.IsNullOrWhiteSpace(effectId))return extraDamage;var row=effectTable.Find(effectId);row.TryRange(out int min,out _);var e=CreateEffect();return e.Effect==EngravingEffect.FollowUpDamage?e.Value:0;}
}}
