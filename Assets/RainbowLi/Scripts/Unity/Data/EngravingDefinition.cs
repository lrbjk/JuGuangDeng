using UnityEngine;
namespace RainbowLi.UnityGame {
[CreateAssetMenu(menuName="RainbowLi/铭刻定义")]
public sealed class EngravingDefinition : ScriptableObject {
 [Min(0)] public int extraDamage = 5;
 [TextArea] public string description = "卡牌完成牌序时追加伤害";
}}
