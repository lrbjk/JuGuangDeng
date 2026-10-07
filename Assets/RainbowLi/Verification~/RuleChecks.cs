using System;
using System.Linq;
using RainbowLi;

public static class RuleChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }
    private static ContainmentGame Battle()
    {
        var g = new ContainmentGame(42); g.StartBattle();
        foreach (var r in g.ArenaRules) r.Numbers = new[] { 9, 9, 9 };
        g.EnemyRule.Numbers = new[] { 9, 9, 9 };
        return g;
    }
    private static Card Card(int n, CardEffect effect = CardEffect.Guard)
    { return new Card { Number = n, Effect = effect }; }
    private static void Arrange(ContainmentGame g, params Card[] cards)
    {
        g.Hand.Clear(); g.Hand.AddRange(cards);
        foreach (var c in cards) g.AddCard(c);
        // Keep mandatory enemy actions, move all selected cards before them.
        for (int i = 0; i < cards.Length; i++)
        {
            int at = g.Timeline.FindIndex(a => a.Card == cards[i]);
            while (at > i) { g.MoveAction(at, -1); at--; }
        }
    }
    public static void Main()
    {
        var g = new ContainmentGame(42);
        Check(g.Deck.Count == 18 && Enumerable.Range(1, 6).All(n => g.Deck.Count(c => c.Number == n) == 3), "18-card initial deck");
        g.StartBattle(true); Check(g.Phase == GamePhase.Map, "boss gated");
        g.StartBattle(); Check(g.Hand.Count == 6 && g.Timeline.Count == 2, "draw and intents");
        for (int i = 0; i < 4; i++) Check(g.AddCard(g.Hand[i]), "add up to four");
        Check(!g.AddCard(g.Hand[4]) && !g.AddCard(g.Hand[0]), "limit and duplicate");
        g.RemoveAction(0); Check(g.Timeline.Count == 6, "cannot remove enemy");
        g.Confirm(); int steps = 0;
        while (g.Phase == GamePhase.Resolving && steps++ < 500) g.Step();
        Check(g.Phase == GamePhase.Planning && g.Round == 2 && g.Hand.Count == 6, "next round redraw");

        g = Battle(); g.PlayerRules.Clear();
        Arrange(g, Card(6)); g.Confirm(); g.Step();
        Check(g.Shield == 9, "guard precedes attack");
        g.Step(); Check(g.Health == 70 && g.Shield == 4, "shield absorbs first attack");
        g.Step(); Check(g.Health == 69, "remaining damage reaches health");

        g = Battle(); Arrange(g, Card(1), Card(2), Card(3)); g.Confirm();
        g.Step(); g.Step(); g.Step();
        Check(g.PlayerRules[0].Triggers == 1 && g.EnemyHealth == g.EnemyMaxHealth, "effect inserted after third number");
        g.Step(); Check(g.EnemyHealth == g.EnemyMaxHealth - 10, "sequence damage applied");

        g = Battle(); g.PlayerRules.Clear();
        var chain = ContainmentGame.Rule("chain", new[] { 2, 2, 2 }, RuleEffect.Power, 1); chain.InsertsTwo = true;
        g.PlayerRules.Add(chain); Arrange(g, Card(2), Card(2), Card(2)); g.Confirm();
        steps = 0; while (g.Phase == GamePhase.Resolving && steps++ < 500) g.Step();
        Check(chain.Triggers == 0 && g.Round == 2, "99-chain terminates and resets next round");
        Check(g.History.Any(s => s.Contains("99/99")), "chain reaches exactly 99 triggers");

        g = Battle();
        g.ArenaRules[0].Numbers = new[] { 1, 2, 3 };
        g.EnemyRule.Numbers = new[] { 1, 2, 3 };
        Arrange(g, Card(1), Card(2), Card(3)); g.Confirm(); g.Step(); g.Step(); g.Step();
        g.Step(); Check(g.LastAction.StartsWith("场地"), "arena priority");
        g.Step(); Check(g.LastAction.StartsWith("基础"), "player priority");
        g.Step(); Check(g.LastAction.StartsWith("实体"), "enemy priority");

        g = Battle(); g.PlayerRules.Clear();
        g.ArenaRules[0].Numbers = new[] { 2, 2, 2 };
        g.ArenaRules[0].Effect = RuleEffect.Power;
        Arrange(g, Card(2), Card(2), Card(2), Card(2)); g.Confirm();
        steps = 0; while (g.Phase == GamePhase.Resolving && steps++ < 100) g.Step();
        Check(g.ArenaIndex == 1 && g.ArenaTriggerCount == 0, "arena rotates only at next round");

        g = new ContainmentGame(42); g.OpenService(false); g.Health = 1; g.RestHeal();
        g.RestGenerate(); Check(g.Health == 25 && g.Deck.Count == 18, "one rest action");
        g.LeaveService(); g.OpenService(false); Check(g.Phase == GamePhase.Map, "no unlimited rest reentry");
        g.OpenService(true); Check(!g.BuyCard(3), "cannot spend missing coins");
        g.Coins = 100; var card = g.Deck[0];
        Check(g.BuyUpgrade(card, true) && !g.BuyUpgrade(card, true) && g.Coins == 80, "one engraving per card");
        Check(g.BuyUpgrade(card, false) && card.Upgrade == 1 && g.Coins == 68, "upgrade cost");
        Check(g.BuyCard(4) && g.Deck.Last().Number == 4 && g.Coins == 53, "directed generation");

        g = Battle(); g.EnemyHealth = 1;
        Arrange(g, Card(1, CardEffect.Strike)); g.Confirm(); g.Step();
        Check(g.Phase == GamePhase.Reward && g.Coins == 25 && g.Rewards.Count == 3, "victory reward");
        g.TakeReward(0); Check(g.Phase == GamePhase.Map && g.Deck.Count == 19, "reward joins deck");
        g.StartBattle(); g.EnemyHealth = 1; Arrange(g, Card(1, CardEffect.Strike)); g.Confirm(); g.Step(); g.TakeReward(0);
        g.StartBattle(true); Check(g.Boss && g.Timeline.Count == 3, "boss unlock and intents");
        g.EnemyHealth = 1; Arrange(g, Card(1, CardEffect.Strike)); g.Confirm(); g.Step();
        Check(g.Phase == GamePhase.Won, "boss win");

        g = Battle(); g.Health = 1; g.Hand[0].Effect = CardEffect.Guard; g.AddCard(g.Hand[0]); g.Confirm(); g.Step();
        Check(g.Phase == GamePhase.Lost, "defeat stops timeline");
        g = Battle(); g.PlayerRules.Clear();
        var inserted = Card(2); g.Hand.Clear(); g.Hand.Add(inserted);
        Check(g.InsertCard(inserted, 0) && g.Timeline[0].Card == inserted && g.Timeline[1].Enemy, "insert before enemy");
        Check(!g.InsertCard(inserted, 0) && !g.InsertCard(Card(3), 99), "insert validates membership, duplicates and index");
        int oldHealth = g.Health, oldEnemyHealth = g.EnemyHealth, oldLog = g.History.Count;
        var predicted = g.Forecast();
        Check(g.Phase == GamePhase.Planning && g.Health == oldHealth && g.EnemyHealth == oldEnemyHealth && g.History.Count == oldLog, "forecast leaves live state intact");
        var repeated = g.Forecast();
        Check(predicted.Health == repeated.Health && predicted.EnemyHealth == repeated.EnemyHealth, "forecast is stable");
        g.Confirm(); for (int i = 0; i < 3; i++) g.Step();
        Check(g.Health == predicted.Health && g.Shield == predicted.Shield && g.EnemyHealth == predicted.EnemyHealth, "forecast matches shield and actual damage");

        g = Battle(); g.PlayerRules.Clear();
        var previewChain = ContainmentGame.Rule("preview chain", new[] { 2, 2, 2 }, RuleEffect.Power, 1); previewChain.InsertsTwo = true;
        g.PlayerRules.Add(previewChain); Arrange(g, Card(2), Card(2), Card(2));
        predicted = g.Forecast();
        Check(predicted.RuleHits[previewChain.Name] == 99 && previewChain.Triggers == 0 && g.NumberStream.Count == 0, "forecast supports 99-chain without mutation");

        g = Battle(); g.EnemyHealth = 1; Arrange(g, Card(1, CardEffect.Strike));
        predicted = g.Forecast();
        Check(predicted.Victory && g.Victories == 0 && g.Coins == 0 && g.Deck.All(c => !c.Engraved), "forecast victory does not award or engrave live cards");

        Console.WriteLine("PASS: " + checks + " gameplay assertions");
    }
}
