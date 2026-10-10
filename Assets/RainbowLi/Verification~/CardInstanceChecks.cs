using System;
using System.Linq;
using RainbowLi;

public static class CardInstanceChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }

    private static CardEffectInstance Effect(CardEffect effect, int x, bool negative = false)
    {
        return new CardEffectInstance {
            BaseX = x,
            Spec = new CardEffectSpec { Code = effect.ToString(), Name = effect.ToString(),
                Effect = effect, Polarity = negative ? EffectPolarity.Negative : EffectPolarity.Positive,
                Lifetime = CombatStatuses.IsDirect(effect) ? EffectLifetime.Direct : EffectLifetime.Temporary }
        };
    }

    private static ContainmentGame Battle(Card card, bool enemy = false)
    {
        var game = new ContainmentGame(42, new GameSetup {
            PlayerHealth = 300, EnemyHealth = 300, EnemyAttack = 0, GuardInterval = 0
        });
        game.Deck[0] = card;
        game.StartBattle();
        game.PlayerRules.Clear();
        foreach (var rule in game.ArenaRules) rule.Numbers = new[] { 0, 0, 0 };
        game.EnemyRule.Numbers = new[] { 0, 0, 0 };
        game.Hand.Clear(); game.Hand.Add(card);
        Check(game.InsertCard(card, 0), "insert compound card");
        game.Timeline[0].Enemy = enemy;
        return game;
    }

    public static void Main()
    {
        var template = new Card { Number = 3, Engraved = true, EngravingCode = "pending",
            PermanentGrowth = 7, BattleCharge = 4, Upgrade = 2 };
        template.Effects.Add(Effect(CardEffect.Guard, 5));
        var first = template.CopyAsNewInstance();
        var second = template.CopyAsNewInstance();
        Check(first.InstanceId != second.InstanceId && first.InstanceId != template.InstanceId, "new cards have distinct IDs");
        Check(first.BattleCharge == 0 && first.PermanentGrowth == 7 && first.Upgrade == 2 && first.EngravingCode == "pending", "physical copy retains permanent state, clears charge");
        first.PermanentGrowth++; first.BattleCharge++; first.Effects[0].BaseX++; first.Effects[0].Spec.Name = "changed";
        Check(second.PermanentGrowth == 7 && second.BattleCharge == 0 && second.Effects[0].BaseX == 5 && template.Effects[0].Spec.Name != "changed", "physical copies deeply isolated");
        var snapshot = first.CopyForSnapshot();
        Check(snapshot.InstanceId == first.InstanceId && snapshot.BattleCharge == first.BattleCharge, "snapshot preserves identity and charge");
        snapshot.Effects[0].Spec.Code = "snapshot"; snapshot.PermanentGrowth++;
        Check(first.Effects[0].Spec.Code != "snapshot" && first.PermanentGrowth == 8, "snapshot mutations isolated");

        var setup = new GameSetup();
        setup.InitialDeck.AddRange(Enumerable.Repeat(template, 6));
        var game = new ContainmentGame(21, setup);
        Check(game.Deck.Select(c => c.InstanceId).Distinct().Count() == 6, "repeated template creates six physical IDs");
        Check(game.Deck.All(c => c.InstanceId != template.InstanceId && c.BattleCharge == 0), "starter does not reuse template identity or charge");
        var pool = new GameSetup(); pool.CardPool.Add(template);
        game = new ContainmentGame(21, pool);
        var generated = game.GenerateCard(3); var generatedAgain = game.GenerateCard(3);
        Check(generated.InstanceId != generatedAgain.InstanceId && generated.InstanceId != template.InstanceId, "pool generation gets fresh IDs");
        Check(generated.Effects[0] != template.Effects[0], "pool effects deeply copied");

        var card = new Card { Number = 3 };
        card.Effects.Add(Effect(CardEffect.Strength, 2));
        var attack = Effect(CardEffect.Strike, 4); attack.Multiplier = 2;
        card.Effects.Add(attack);
        game = Battle(card);
        card.BattleCharge = 9; card.PermanentGrowth = 11;
        var battleSnapshot = game.CreateBattleSnapshot();
        var snapshotCard = battleSnapshot.Deck[0];
        Check(ReferenceEquals(snapshotCard, battleSnapshot.Hand[0]), "snapshot deck and hand preserve card aliases");
        Check(snapshotCard.InstanceId == card.InstanceId && snapshotCard.BattleCharge == 9 && !ReferenceEquals(snapshotCard, card), "battle snapshot preserves isolated state");
        battleSnapshot.Step();
        Check(ReferenceEquals(battleSnapshot.LastResolvedAction.Card, snapshotCard), "snapshot queue shares deck card");
        Check(game.EnemyHealth == 300 && game.Player.Statuses[CardEffect.Strength] == 0, "snapshot execution does not mutate live combatants");
        var forecast = game.Forecast(); var again = game.Forecast();
        Check(forecast.EnemyHealth == 290 && again.EnemyHealth == 290, "strength precedes doubled attack, multiplier applied once");
        Check(card.BattleCharge == 9 && card.PermanentGrowth == 11 && card.Effects[1].BaseX == 4, "repeated preview preserves instance state");
        Check(game.Confirm(), "compound battle confirms"); game.Step();
        Check(game.EnemyHealth == 290 && game.Player.Statuses[CardEffect.Strength] == 2 && game.NumberStream.Count == 1, "compound effects execute as one card behavior and one number");
        while (game.Phase == GamePhase.Resolving && game.Round == 1) game.Step();
        Check(card.BattleCharge == 9 && card.PermanentGrowth == 11, "new round retains encounter charge and growth");

        card = new Card { Number = 4 };
        card.Effects.Add(Effect(CardEffect.Guard, 3)); card.Effects.Add(Effect(CardEffect.Strike, 4));
        game = Battle(card); game.Player.Statuses.Add(CardEffect.Bleed, 3);
        game.Player.Statuses.Add(CardEffect.Reciprocity, 1);
        forecast = game.Forecast(); game.Confirm(); game.Step();
        Check(game.Health == 297 && game.Player.Statuses[CardEffect.Bleed] == 2 && game.NumberStream.Count == 1, "bleed occurs once per compound behavior");
        game.Step();
        Check(game.LastResolvedAction.IsRepeat && game.EnemyHealth == 292 && game.Shield == 6 && game.Health == 295 && game.NumberStream.Count == 2, "reciprocity repeats whole compound behavior exactly once");
        Check(forecast.Health == game.Health && forecast.EnemyHealth == game.EnemyHealth, "compound bleed and replay match forecast");

        card = new Card { Number = 3 };
        card.Effects.Add(Effect(CardEffect.Poison, 4, true)); card.Effects.Add(Effect(CardEffect.Guard, 6));
        game = Battle(card, true); game.Confirm(); game.Step();
        Check(game.Player.Statuses[CardEffect.Poison] == 4 && game.EnemyShield == 6 && game.Player.Shield == 0, "enemy compound polarity targets correct actor");

        card = new Card { Number = 2 };
        card.Effects.Add(Effect(CardEffect.Strike, 1000)); card.Effects.Add(Effect(CardEffect.Guard, 8));
        game = Battle(card); forecast = game.Forecast(); game.Confirm(); game.Step();
        Check(game.Phase == GamePhase.Reward && game.Shield == 0 && forecast.Victory && forecast.Shield == 0, "fatal first effect stops subsequent effects in live and preview");
        card.PermanentGrowth = 13; card.BattleCharge = 6;
        var id = card.InstanceId;
        game.TakeReward(0); game.StartBattle();
        Check(card.BattleCharge == 0 && card.PermanentGrowth == 13 && card.InstanceId == id, "next encounter resets only battle state");

        var upgradeEffect = Effect(CardEffect.Guard, 3); upgradeEffect.Multiplier = 2; upgradeEffect.UpgradeBonus = 1;
        card = new Card { Upgrade = 2 }; card.Effects.Add(upgradeEffect);
        Check(card.CanUpgrade && card.Value == 8, "compound upgrade applies after base multiplier");
        upgradeEffect.Spec.UsesNumber = true; upgradeEffect.Spec.Effect = CardEffect.InverseStrike;
        Check(upgradeEffect.ResolveValue(9, 0) == 2, "inverse minimum is applied before multiplier");
        upgradeEffect.Multiplier = 0;
        bool rejected = false; try { upgradeEffect.ResolveValue(3, 0); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "invalid multiplier rejected");
        Console.WriteLine("PASS: " + checks + " card instance and compound effect assertions");
    }
}
