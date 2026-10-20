using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/玩家定义")]
 public sealed class PlayerDefinition : ScriptableObject {
  [Min(1)] public int maxHealth=70;
  [Min(0)] public int attack=5;
  [Range(1,9)] public int handSize=6;
  [Range(6,9)] public int handLimit=9;
  public bool randomStartingDeck=true;
  [Range(.1f,1)] public float repeatWeightMultiplier=.5f;
  [Range(1,4)] public int maxActions=4;
  [Min(0)] public int startingCoins;
  public CardDefinition[] startingDeck;
  public PlayerCardTable cardTable;
  public SequenceDefinition[] sequences;
  [Tooltip("额外已拥有的牌序效果；基础牌序效果会自动加入可选池")] public SequenceEffect[] availableSequenceEffects;
  public void Apply(GameSetup setup){
   setup.AvailablePlayerEffects.Clear();if(availableSequenceEffects!=null)foreach(var e in availableSequenceEffects)if(e!=null)setup.AvailablePlayerEffects.Add(new SequenceEffect {Effect=e.Effect,ShieldBonusPercent=e.ShieldBonusPercent,Value=e.Value,InsertsTwo=e.InsertsTwo,Requirement=e.Requirement,Code=e.Code,BaseX=e.BaseX,Multiplier=e.Multiplier,FixedValue=e.FixedValue,MinX=e.MinX,MaxX=e.MaxX,RollX=e.RollX});setup.PlayerAttack=attack;setup.HandLimit=handLimit;setup.RandomStartingDeck=randomStartingDeck;setup.RepeatWeightMultiplier=repeatWeightMultiplier;setup.PlayerHealth=maxHealth;setup.HandSize=handSize;setup.PlayerActions=maxActions;setup.StartingCoins=startingCoins;
   if(startingDeck!=null&&startingDeck.Length>0){setup.InitialDeck.Clear();foreach(var c in startingDeck)if(c!=null)setup.InitialDeck.Add(c.CreateCard());}
   if(sequences!=null&&sequences.Length>0){setup.PlayerRules.Clear();foreach(var r in sequences)if(r!=null)setup.PlayerRules.Add(r.CreateRule());}
   cardTable?.Apply(setup);
  }
 }
}
