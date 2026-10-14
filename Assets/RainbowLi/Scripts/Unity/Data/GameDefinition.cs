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
 [Header("策划效果池权重表（优先于兼容内联权重）")]
 public EffectPoolWeightTable cardPoolWeights,sequencePoolWeights;
 public EffectPoolWeightRow[] CreateCardPoolWeightSnapshot()=>cardPoolWeights==null?System.Array.Empty<EffectPoolWeightRow>():cardPoolWeights.GetSnapshot(EffectPoolKind.Card);
 public EffectPoolWeightRow[] CreateSequencePoolWeightSnapshot()=>sequencePoolWeights==null?System.Array.Empty<EffectPoolWeightRow>():sequencePoolWeights.GetSnapshot(EffectPoolKind.Sequence);
 public EffectPoolWeight[] cardEffectPools=System.Array.Empty<EffectPoolWeight>(),sequenceEffectPools=System.Array.Empty<EffectPoolWeight>();
 [Min(1)] public int rewardCount=3;[Min(0)] public int cardPrice=15,upgradePrice=12,engravingPrice=20,rulePrice=18,restHealing=24;
 public SequenceEffectTable sequenceEffects; public EngravingEffectTable engravingEffects;
 public bool randomPlayerSequenceEffects,randomEnemySequenceEffects;
 public string playerSequencePool,enemySequencePool,rewardSequencePool,shopSequencePool,engravingPool;
 [HideInInspector] public SequenceDefinition[] playerRules;
 public SequenceDefinition[] arenaRules;
 [HideInInspector] public SequenceDefinition enemyRule;
 static void ValidatePools(GameSetup setup){
  foreach(var pool in setup.CardEffectPools){if(pool.Weight<=0)continue;bool eligible=false,starting=false;
   if(setup.CardEffectGroups.Count>0){foreach(var group in setup.CardEffectGroups){var e=group.Primary;if(e.Weight>0&&e.InPool(pool.Name)){eligible=true;if(e.StartingEnabled)starting=true;}}}
   else foreach(var e in setup.CardEffects)if(e.Weight>0&&e.InPool(pool.Name)){eligible=true;if(e.StartingEnabled)starting=true;}
   if(!eligible||setup.RandomStartingDeck&&!starting)throw new System.InvalidOperationException("卡牌效果池「"+pool.Name+"」没有可生成"+(setup.RandomStartingDeck?"起始牌":"卡牌")+"的效果，请检查池归属、启用和权重。");
  }
  foreach(var pool in setup.SequenceEffectPools)if(pool.Weight>0&&!setup.SequencePool.Exists(e=>e.Enabled&&e.Weight>0&&e.InPool(pool.Name)))throw new System.InvalidOperationException("牌序效果池「"+pool.Name+"」没有可抽取效果。");
 }
 public GameSetup CreateSetup() {
  var s=new GameSetup { PlayerHealth=playerHealth, HandSize=handSize, EnemyHealth=enemyHealth, EnemyHealthGrowth=enemyHealthGrowth, BossHealth=bossHealth, EnemyAttack=enemyAttack, EnemyGuard=enemyGuard, BossAttack=bossAttack };
  if(initialDeck!=null) foreach(var c in initialDeck) if(c!=null) s.InitialDeck.Add(c.CreateCard());
  s.RewardCount=rewardCount;s.CardPrice=cardPrice;s.UpgradePrice=upgradePrice;s.EngravingPrice=engravingPrice;s.RulePrice=rulePrice;s.RestHealing=restHealing;
  cardEffects?.Apply(s);
  if(cardPoolWeights!=null)foreach(var row in CreateCardPoolWeightSnapshot())s.CardEffectPools.Add(new EffectPoolWeight{Id=row.id,Name=row.name,Weight=row.weight});
  else if(cardEffectPools!=null)foreach(var pool in cardEffectPools)if(pool!=null)s.CardEffectPools.Add(pool.Copy());
  if(sequencePoolWeights!=null)foreach(var row in CreateSequencePoolWeightSnapshot())s.SequenceEffectPools.Add(new EffectPoolWeight{Id=row.id,Name=row.name,Weight=row.weight});
  else if(sequenceEffectPools!=null)foreach(var pool in sequenceEffectPools)if(pool!=null)s.SequenceEffectPools.Add(pool.Copy());
  if(sequenceEffects!=null){sequenceEffects.ValidateTable();foreach(var row in sequenceEffects.rows)if(sequenceEffects.IsRuntimeSupported(row))s.SequencePool.Add(RuntimeTableAdapter.Sequence(row));}
  if(engravingEffects!=null){engravingEffects.ValidateTable();foreach(var row in engravingEffects.rows)if(engravingEffects.IsRuntimeSupported(row))s.EngravingPool.Add(RuntimeTableAdapter.Engraving(row));}
  s.RandomPlayerSequenceEffects=randomPlayerSequenceEffects;s.RandomEnemySequenceEffects=randomEnemySequenceEffects;s.PlayerSequencePool=playerSequencePool;s.EnemySequencePool=enemySequencePool;s.RewardSequencePool=rewardSequencePool;s.ShopSequencePool=shopSequencePool;s.EngravingPoolName=engravingPool;
  if(cardPool!=null) foreach(var c in cardPool) if(c!=null) s.CardPool.Add(c.CreateCard());
  if(playerRules!=null) foreach(var r in playerRules) if(r!=null) s.PlayerRules.Add(r.CreateRule());
  if(arenaRules!=null) foreach(var r in arenaRules) if(r!=null) s.ArenaRules.Add(r.CreateRule());
  s.EnemyRule=enemyRule==null?null:enemyRule.CreateRule();player?.Apply(s);enemy?.Apply(s);ValidatePools(s);return s;
 }
}}
