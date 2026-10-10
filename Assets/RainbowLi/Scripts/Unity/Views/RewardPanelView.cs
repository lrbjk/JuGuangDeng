using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class RewardPanelView:MonoBehaviour {
 public GameView view;
 public void Initialize(){for(int i=0;i<view.rewardButtons.Length;i++){int index=i;view.rewardButtons[i].onClick.AddListener(()=>view.progression.Reward(index));}}
 public void Refresh(ContainmentGame g){for(int i=0;i<view.rewards.Length;i++){bool exists=i<g.Rewards.Count;view.rewards[i].gameObject.SetActive(exists);view.rewardButtons[i].gameObject.SetActive(exists);if(!exists)continue;var c=g.Rewards[i];view.rewards[i].Bind(c.Number,c.Effect,c.Description,false,c.Engraved,false,false,null);view.rewards[i].SetDetails(view.tooltip,c.Name+" · 数字 "+c.Number,c.RulesText);}}
}
}
