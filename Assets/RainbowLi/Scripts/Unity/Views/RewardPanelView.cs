using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class RewardPanelView:MonoBehaviour {
 public GameView view;
 public bool illustrated;
 public DeckOperationView deck;
 public UnityEngine.UI.Button openDeck;
 bool openedForReward;
 public void Initialize(){for(int i=0;i<view.rewardButtons.Length;i++){int index=i;view.rewardButtons[i].onClick.AddListener(()=>view.progression.Reward(index));}}
 public void Refresh(ContainmentGame g){if(g.Phase!=GamePhase.Reward)openedForReward=false;for(int i=0;i<view.rewards.Length;i++){view.rewards[i].gameObject.SetActive(false);view.rewardButtons[i].gameObject.SetActive(false);}if(!illustrated&&g.Phase==GamePhase.Reward&&view.rewardPanel.activeInHierarchy&&!openedForReward){openedForReward=true;deck.Open(DeckOperation.Reward);}}
}
}
