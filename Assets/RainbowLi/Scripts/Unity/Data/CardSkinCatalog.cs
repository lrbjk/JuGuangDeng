using System;
using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/卡牌美术配置",fileName="CardSkins")]
 public sealed class CardSkinCatalog:ScriptableObject {
  [Serializable] public sealed class Entry {
   [Range(1,3)] public int suit=1;
   [Range(1,6)] public int number=1;
   [Tooltip("红色敌人牌；不勾选为蓝色玩家牌。")]
   public bool enemy;
   public Sprite sprite;
  }
  [Header("效果类别对应花色")]
  [Range(1,3)] public int attackSuit=1,guardSuit=2,effectSuit=3;
  [Tooltip("包含3种花色 × 6点数 × 玩家/敌人两方，共36张。")]
  public Entry[] entries=Array.Empty<Entry>();
  public Material fallbackMaterial;
  public int SuitFor(CardEffect effect)=>effect==CardEffect.Strike||effect==CardEffect.IncreasedStrike||effect==CardEffect.InverseStrike?attackSuit:effect==CardEffect.Guard||effect==CardEffect.Fortify||effect==CardEffect.IronWall||effect==CardEffect.Parry||effect==CardEffect.Barrier||effect==CardEffect.DamageCap||effect==CardEffect.Invulnerable?guardSuit:effectSuit;
  public Sprite Find(int number,CardEffect effect,bool enemy){int suit=SuitFor(effect);return entries.FirstOrDefault(e=>e!=null&&e.suit==suit&&e.number==number&&e.enemy==enemy)?.sprite;}
 }
}
