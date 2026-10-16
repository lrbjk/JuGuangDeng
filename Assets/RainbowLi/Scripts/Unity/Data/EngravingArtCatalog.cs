using System;
using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/铭刻美术配置",fileName="EngravingArt")]
 public sealed class EngravingArtCatalog:ScriptableObject {
  [Serializable] public sealed class Entry {
   [Tooltip("策划识别名称，仅用于配置。")]
   public string name;
   [Tooltip("与铭刻效果表的 ID 对应；运行时优先使用 ID 查找。")]
   public string effectId;
   [Tooltip("旧卡牌没有 ID 时，按此铭刻行为匹配。")]
   public EngravingEffect effect;
   public Sprite frame;
  }
  public Entry[] entries=Array.Empty<Entry>();
  [Tooltip("兼容旧版只有铭刻标记的卡牌；原有含义为追击伤害。")]
  public EngravingEffect legacyEffect=EngravingEffect.FollowUpDamage;
  public Sprite Find(RuntimeEngraving engraving){
   var byId=engraving==null||string.IsNullOrWhiteSpace(engraving.Code)?null:entries.FirstOrDefault(e=>e!=null&&e.effectId==engraving.Code&&e.frame!=null);
   return byId?.frame??entries.FirstOrDefault(e=>e!=null&&e.effect==(engraving?.Effect??legacyEffect)&&e.frame!=null)?.frame;
  }
 }
}
