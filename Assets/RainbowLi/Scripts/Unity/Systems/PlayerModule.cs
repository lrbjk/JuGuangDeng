using UnityEngine;
namespace RainbowLi.UnityGame {
 public sealed class PlayerModule : MonoBehaviour {
  public GameSession session;
  public PlayerDefinition definition;
  public PlayerState State => session.Game==null?null:session.Game.Player;
  public void Apply(GameSetup setup){if(definition!=null)definition.Apply(setup);}
 }
}
