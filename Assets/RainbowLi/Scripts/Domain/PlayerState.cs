using System.Collections.Generic;
namespace RainbowLi {
 public sealed class PlayerState : CombatantState {
  public readonly List<Card> Deck=new List<Card>(),Hand=new List<Card>();
  public readonly List<SequenceRule> Rules=new List<SequenceRule>();
  public readonly List<SequenceEffect> AvailableEffects=new List<SequenceEffect>();
  public int Coins;
  public PlayerState(int health){Health=MaxHealth=health;}
 }
}
