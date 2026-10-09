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

        foreach (bool enemyFinish in new[] { false, true })
        foreach (RuleEffect effect in new[] { RuleEffect.Shield, RuleEffect.Damage, RuleEffect.Power, RuleEffect.Insert })
        {
            g = Battle(); g.PlayerRules.Clear();
            var arena = g.ArenaRules[0]; arena.Numbers = new[] { 1, 2, 3 }; arena.Effect = effect; arena.Value = 8;
            g.Timeline.Clear(); g.Hand.Clear();
            for (int n = 1; n <= 3; n++)
            {
                bool enemy = enemyFinish ? n != 1 : n != 3;
                var c = enemy ? null : Card(n);
                if (c != null) g.Hand.Add(c);
                g.Timeline.Add(new BattleAction { Enemy = enemy, Number = n, Card = c, Effect = CardEffect.Guard, Value = 0 });
            }
            var preview = g.Forecast();
            g.Confirm(); g.Step(); g.Step(); g.Step();
            int shieldBefore = g.Shield, enemyShieldBefore = g.EnemyShield, hpBefore = g.Health, enemyHpBefore = g.EnemyHealth;
            g.Step();
            Check(g.LastResolvedAction.Enemy == enemyFinish, "arena belongs to final card for " + effect);
            if (effect == RuleEffect.Shield)
                Check(enemyFinish ? g.EnemyShield == enemyShieldBefore + 8 && g.Shield == shieldBefore : g.Shield == shieldBefore + 8 && g.EnemyShield == enemyShieldBefore, "arena shield recipient");
            if (effect == RuleEffect.Damage)
                Check(enemyFinish ? g.Health == hpBefore - Math.Max(0, 8 - shieldBefore) && g.EnemyHealth == enemyHpBefore : g.EnemyHealth == enemyHpBefore - 8 && g.Health == hpBefore, "arena damage target");
            if (effect == RuleEffect.Power)
                Check(enemyFinish ? g.EnemyPower == 8 && g.Power == 0 : g.Power == 8 && g.EnemyPower == 0, "power isolated by side");
            if (effect == RuleEffect.Insert)
            {
                g.Step();
                Check(g.LastResolvedAction.IsNumberOnly && g.LastResolvedAction.Enemy == enemyFinish, "inserted number inherits trigger owner");
            }
            Check(g.Health == preview.Health && g.Shield == preview.Shield && g.EnemyHealth == preview.EnemyHealth && g.EnemyShield == preview.EnemyShield, "arena forecast matches actual");
        }
        g = Battle(); g.PlayerRules.Clear();
        g.Timeline.Clear(); var strike = Card(4, CardEffect.Strike); g.Hand.Clear(); g.Hand.Add(strike);
        g.Timeline.Add(new BattleAction { Enemy = true, Number = 6, Effect = CardEffect.Guard, Value = 10 });
        g.Timeline.Add(new BattleAction { Card = strike, Number = 4, Effect = CardEffect.Strike });
        g.Confirm(); g.Step(); Check(g.EnemyShield == 10, "enemy guard adds shield");
        g.Step(); Check(g.EnemyShield == 3 && g.EnemyHealth == g.EnemyMaxHealth, "enemy shield absorbs player attack");

        var authored = new GameSetup { PlayerHealth = 90, EnemyHealth = 50, HandSize = 4 };
        var authoredCard = new Card { Number = 1, Effect = CardEffect.Strike, BaseBonus = 8, UpgradeBonus = 4, EngravingDamage = 11 };
        authored.InitialDeck.AddRange(Enumerable.Range(0,18).Select(_ => authoredCard));
        authored.PlayerRules.Add(ContainmentGame.Rule("authored",new[]{1,1,1},RuleEffect.Shield,7));
        g = new ContainmentGame(42, authored);
        Check(g.Health==90&&g.HealthLimit==90&&g.Deck.Count==18,"authored health and starter deck");
        Check(g.Deck[0].Value==9&&g.Deck[0].UpgradeBonus==4&&g.Deck[0].EngravingDamage==11,"authored card and engraving values");
        g.Deck[0].Upgrade++;
        Check(authoredCard.Upgrade==0&&g.Deck[1].Upgrade==0,"runtime card instances isolated");
        g.PlayerRules[0].Numbers[0]=6;
        Check(authored.PlayerRules[0].Numbers[0]==1,"authored sequences isolated");
        g.StartBattle();Check(g.EnemyMaxHealth==50&&g.Hand.Count==4,"authored battle and hand size");
        g.EnemyShield=3;Arrange(g,g.Hand[0]);var authoredForecast=g.Forecast();g.Confirm();
        for(int n=0;n<3&&g.Phase==GamePhase.Resolving;n++)g.Step();
        Check(g.Health==authoredForecast.Health&&g.EnemyHealth==authoredForecast.EnemyHealth,"configured forecast preserves authored card values");

        g=Battle();
        Check(object.ReferenceEquals(g.Deck,g.Player.Deck)&&object.ReferenceEquals(g.Hand,g.Player.Hand),"player owns deck and hand");
        Check(object.ReferenceEquals(g.PlayerRules,g.Player.Rules)&&object.ReferenceEquals(g.EnemyRule,g.Enemy.Rule),"actor sequence ownership");
        g.Player.Shield=10;g.Enemy.Shield=4;g.Player.ReceiveDamage(13);
        Check(g.Player.Health==67&&g.Player.Shield==0&&g.Enemy.Shield==4,"player damage independent of enemy");
        g.Enemy.ReceiveDamage(7);
        Check(g.Enemy.Health==g.Enemy.MaxHealth-3&&g.Enemy.Shield==0&&g.Player.Health==67,"enemy damage independent of player");
        g.Player.Power=8;g.Enemy.Power=3;g.Player.ResetRound();
        Check(g.Power==0&&g.EnemyPower==3,"reset one actor does not reset the other");
        var enemyProfile=new GameSetup{EnemyAttack=8,EnemyGuard=11,GuardSlot=0,GuardInterval=1,EnemyNumbers=new[]{5,6}};
        g=new ContainmentGame(42,enemyProfile);g.StartBattle();
        Check(g.Timeline[0].Enemy&&g.Timeline[0].Effect==CardEffect.Guard&&g.Timeline[0].Value==11&&g.Timeline[0].Number==5,"enemy action profile guard");
        Check(g.Timeline[1].Effect==CardEffect.Strike&&g.Timeline[1].Value==8&&g.Timeline[1].Number==6,"enemy action profile strike");
        var playerProfile=new GameSetup{PlayerActions=1,StartingCoins=23};g=new ContainmentGame(42,playerProfile);g.StartBattle();
        Check(g.Coins==23&&g.PlayerActionLimit==1&&g.AddCard(g.Hand[0])&&!g.AddCard(g.Hand[1]),"player authored resources and action limit");

        Console.WriteLine("PASS: " + checks + " gameplay assertions");
    }
}
