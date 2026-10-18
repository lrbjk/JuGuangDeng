namespace RainbowLi {
 public sealed partial class ContainmentGame {
  public bool CanClaimReward=>Phase==GamePhase.Reward&&Rewards.Count>0;
  private void CompleteRewardChoice(){
   if(rewardBatches.Count>0){rewardBatches.RemoveAt(0);ShowNextContentReward();return;}
   Rewards.Clear();Phase=GamePhase.Map;Message="奖励已领取，继续选择任务。";
  }
  public bool TakeEngravingReward(Card card){
   if(!CanClaimReward||card==null||!Deck.Contains(card)||card.Engraved)return false;
   var engraving=DrawEngraving(setup?.EngravingPoolName);
   card.Engraving=engraving;card.EngravingCode=engraving.Code;card.Engraved=true;
   if(engraving.Effect==EngravingEffect.FollowUpDamage)card.EngravingDamage=engraving.Value;
   CompleteRewardChoice();return true;
  }
  public bool TakeSequenceReward(){
   if(!CanClaimReward)return false;
   var effect=DrawSequenceEffect(setup?.RewardSequencePool);AddAvailableEffect(effect);CompleteRewardChoice();return true;
  }
 }
}
