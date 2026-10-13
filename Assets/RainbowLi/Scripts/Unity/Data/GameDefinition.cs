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
 public EffectPoolWeight[] cardEffectPools=System.Array.Empty<EffectPoolWeight>(),sequenceEffectPools=System.Array.Empty<EffectPoolWeight>();
 [Min(1)] public int rewardCount=3;[Min(0)] public int cardPrice=15,upgradePrice=12,engravingPrice=20,rulePrice=18,restHealing=24;
 public SequenceEffectTable sequenceEffects; public EngravingEffectTable engravingEffects;
 public bool randomPlayerSequenceEffects,randomEnemySequenceEffects;
 public string playerSequencePool,enemySequencePool,rewardSequencePool,shopSequencePool,engravingPool;
 [HideInInspector] public SequenceDefinition[] playerRules;
 public SequenceDefinition[] arenaRules;
 [HideInInspector] public SequenceDefinition enemyRule;
 public GameSetup CreateSetup() {
  var s=new GameSetup { PlayerHealth=playerHealth, HandSize=handSize, EnemyHealth=enemyHealth, EnemyHealthGrowth=enemyHealthGrowth, BossHealth=bossHealth, EnemyAttack=enemyAttack, EnemyGuard=enemyGuard, BossAttack=bossAttack };
  if(initialDeck!=null) foreach(var c in initialDeck) if(c!=null) s.InitialDeck.Add(c.CreateCard());
  s.RewardCount=rewardCount;s.CardPrice=cardPrice;s.UpgradePrice=upgradePrice;s.EngravingPrice=engravingPrice;s.RulePrice=rulePrice;s.RestHealing=restHealing;
  cardEffects?.Apply(s);
  if(cardEffectPools!=null)foreach(var pool in cardEffectPools)if(pool!=null)s.CardEffectPools.Add(pool.Copy());
  if(sequenceEffectPools!=null)foreach(var pool in sequenceEffectPools)if(pool!=null)s.SequenceEffectPools.Add(pool.Copy());
  if(sequenceEffects!=null){sequenceEffects.ValidateTable();foreach(var row in sequenceEffects.rows)if(sequenceEffects.IsRuntimeSupported(row))s.SequencePool.Add(RuntimeTableAdapter.Sequence(row));}
  if(engravingEffects!=null){engravingEffects.ValidateTable();foreach(var row in engravingEffects.rows)if(engravingEffects.IsRuntimeSupported(row))s.EngravingPool.Add(RuntimeTableAdapter.Engraving(row));}
  s.RandomPlayerSequenceEffects=randomPlayerSequenceEffects;s.RandomEnemySequenceEffects=randomEnemySequenceEffects;s.PlayerSequencePool=playerSequencePool;s.EnemySequencePool=enemySequencePool;s.RewardSequencePool=rewardSequencePool;s.ShopSequencePool=shopSequencePool;s.EngravingPoolName=engravingPool;
  if(cardPool!=null) foreach(var c in cardPool) if(c!=null) s.CardPool.Add(c.CreateCard());
  if(playerRules!=null) foreach(var r in playerRules) if(r!=null) s.PlayerRules.Add(r.CreateRule());
  if(arenaRules!=null) foreach(var r in arenaRules) if(r!=null) s.ArenaRules.Add(r.CreateRule());
  s.EnemyRule=enemyRule==null?null:enemyRule.CreateRule();player?.Apply(s);enemy?.Apply(s);return s;
 }
}}
