using UnityEngine;
namespace RainbowLi.UnityGame {
[CreateAssetMenu(menuName="RainbowLi/游戏配置")]
public sealed class GameDefinition : ScriptableObject {
 public PlayerDefinition player;
 public EnemyDefinition enemy;
 [HideInInspector] public int playerHealth=70;
 [HideInInspector] public int handSize=6;
 [HideInInspector] public int enemyHealth=36, enemyHealthGrowth=12, bossHealth=110;
 [HideInInspector] public int enemyAttack=5, enemyGuard=6, bossAttack=9;
 [HideInInspector] public CardDefinition[] initialDeck;
 [Tooltip("未配置卡牌效果池时使用的兼容卡牌池")] public CardDefinition[] cardPool;
 public CardEffectCatalog cardEffects;
 [HideInInspector] public SequenceDefinition[] playerRules;
 public SequenceDefinition[] arenaRules;
 [HideInInspector] public SequenceDefinition enemyRule;
 public GameSetup CreateSetup() {
  var s=new GameSetup { PlayerHealth=playerHealth, HandSize=handSize, EnemyHealth=enemyHealth, EnemyHealthGrowth=enemyHealthGrowth, BossHealth=bossHealth, EnemyAttack=enemyAttack, EnemyGuard=enemyGuard, BossAttack=bossAttack };
  if(initialDeck!=null) foreach(var c in initialDeck) if(c!=null) s.InitialDeck.Add(c.CreateCard());
  cardEffects?.Apply(s);
  if(cardPool!=null) foreach(var c in cardPool) if(c!=null) s.CardPool.Add(c.CreateCard());
  if(playerRules!=null) foreach(var r in playerRules) if(r!=null) s.PlayerRules.Add(r.CreateRule());
  if(arenaRules!=null) foreach(var r in arenaRules) if(r!=null) s.ArenaRules.Add(r.CreateRule());
  s.EnemyRule=enemyRule==null?null:enemyRule.CreateRule();player?.Apply(s);enemy?.Apply(s);return s;
 }
}}
