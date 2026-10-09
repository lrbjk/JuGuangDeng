using UnityEngine;
namespace RainbowLi.UnityGame {
 public sealed class EnemyModule : MonoBehaviour {
  public GameSession session;
  public EnemyDefinition definition;
  public EnemyState State => session.Game==null?null:session.Game.Enemy;
  public void Apply(GameSetup setup){if(definition!=null)definition.Apply(setup);}
 }
}
