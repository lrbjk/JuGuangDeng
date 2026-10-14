using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi
{
    public sealed partial class ContainmentGame
    {
        private Card FromSpec(CardEffectSpec spec,int number)=>new Card {Number=number,Effect=spec.Effect,Spec=spec.Copy(),EffectAmount=random.Next(spec.Min,spec.Max+1),AttackBase=BaseAttack,BaseBonus=0,UpgradeBonus=spec.Effect==CardEffect.DamageCap||spec.Effect==CardEffect.Reciprocity||spec.Effect==CardEffect.Disorder?0:2};
        private void GenerateInitialDeck(){
            if(setup!=null&&!setup.RandomStartingDeck&&setup.InitialDeck.Count>0)return;
            if(setup!=null&&setup.CardEffectGroups.Count>0){
                var groups=setup.CardEffectGroups.Where(g=>g.Primary.StartingEnabled&&ValidWeight(g.Primary.Weight)).ToArray();
                if(groups.Length==0)throw new ArgumentException("起始牌效果组池为空。");
                for(int round=0;round<3;round++){var weights=groups.Select(g=>(double)g.Primary.Weight).ToArray();for(int number=1;number<=6;number++){
                    if(setup.CardEffectPools.Count>0)Deck.Add(DrawCardGroup(true).CreateCard(number,random,BaseAttack));
                    else {int at=WeightedChoice(weights);Deck.Add(groups[at].CreateCard(number,random,BaseAttack));weights[at]*=Math.Max(.01,Math.Min(1,setup.RepeatWeightMultiplier));}
                }}return;
            }
            if(setup!=null&&setup.CardEffects.Count>0){
                if(setup.CardEffectPools.Count>0){
                    for(int group=0;group<3;group++)for(int number=1;number<=6;number++)Deck.Add(FromSpec(DrawCardEffect(true),number));
                    return;
                }
                var pool=setup.CardEffects.Where(e=>e.StartingEnabled&&e.Weight>0&&!float.IsNaN(e.Weight)&&!float.IsInfinity(e.Weight)).ToArray();
                if(pool.Length==0)throw new ArgumentException("起始牌效果池至少需要启用一种效果");
                for(int group=0;group<3;group++){var weights=pool.Select(e=>(double)e.Weight).ToArray();for(int number=1;number<=6;number++){int at=WeightedChoice(weights);Deck.Add(FromSpec(pool[at],number));weights[at]*=Math.Max(.01,Math.Min(1,setup.RepeatWeightMultiplier));}}
                return;
            }
            for(int group=0;group<3;group++){
                var weights=new double[]{1,1,1};if(setup!=null&&setup.CardPool.Count>0)for(int e=0;e<3;e++)if(!setup.CardPool.Any(c=>(int)c.Effect==e))weights[e]=0;
                for(int number=1;number<=6;number++){var card=GenerateCard(number);if(setup==null||setup.RandomStartingDeck){int at=WeightedChoice(weights);card=GenerateStartingCard(number,(CardEffect)at);weights[at]*=setup==null?.5:Math.Max(.01,Math.Min(1,setup.RepeatWeightMultiplier));}Deck.Add(card);}
            }
        }
        private void DrawRoundHand()
        {
            for(int drawn=0;drawn<(setup==null?6:setup.HandSize);drawn++)
            {
                if (drawPile.Count == 0)
                { drawPile.AddRange(discard); discard.Clear(); Shuffle(drawPile); }
                if (drawPile.Count == 0) break;
                var card = drawPile[drawPile.Count - 1]; drawPile.RemoveAt(drawPile.Count - 1); if(Hand.Count<HandLimit)Hand.Add(card);else discard.Add(card);
            }
        }

        public Card GenerateCard(int number = 0)
        {
            if(setup!=null&&setup.CardEffectGroups.Count>0)return DrawCardGroup(false).CreateCard(number==0?random.Next(1,7):number,random,BaseAttack);
            if(setup!=null&&setup.CardEffects.Count>0)return FromSpec(DrawCardEffect(false),number==0?random.Next(1,7):number);
            if (setup != null && setup.CardPool.Count > 0)
            {
                var candidates = setup.CardPool.Where(c => number == 0 || c.Number == number).ToList();
                if (candidates.Count > 0) {var c=candidates[random.Next(candidates.Count)].CopyAsNewInstance();c.AttackBase=BaseAttack;return RollCard(c);}
            }
            return new Card { AttackBase=BaseAttack, Number = number == 0 ? random.Next(1, 7) : number, Effect = (CardEffect)random.Next(0, 3) };
        }

        private CardEffectGroup DrawCardGroup(bool starting){
            string pool=DrawPool(setup.CardEffectPools);var groups=setup.CardEffectGroups.Where(g=>(!starting||g.Primary.StartingEnabled)&&ValidWeight(g.Primary.Weight)&&g.Primary.InPool(pool)).ToArray();
            if(groups.Length==0)throw new InvalidOperationException("Empty card effect group pool: "+pool);
            return groups[WeightedChoice(groups.Select(g=>(double)g.Primary.Weight).ToArray())];
        }
        private CardEffectSpec DrawCardEffect(bool starting){
            var candidates=setup.CardEffects.Where(e=>(!starting||e.StartingEnabled)&&ValidWeight(e.Weight)).ToArray();
            string pool=DrawPool(setup.CardEffectPools);
            candidates=candidates.Where(e=>e.InPool(pool)).ToArray();
            if(candidates.Length==0)throw new InvalidOperationException("Empty card effect pool: "+pool);
            return candidates[WeightedChoice(candidates.Select(e=>(double)e.Weight).ToArray())];
        }
        private static bool ValidWeight(float weight)=>weight>0&&!float.IsNaN(weight)&&!float.IsInfinity(weight);
        private string DrawPool(List<EffectPoolWeight> pools){
            if(pools.Count==0)return null;
            var candidates=pools.Where(p=>p!=null&&ValidWeight(p.Weight)).ToArray();
            if(candidates.Length==0)throw new InvalidOperationException("No positive effect pool weights");
            return candidates[WeightedChoice(candidates.Select(p=>(double)p.Weight).ToArray())].Name;
        }
        private int WeightedChoice(double[] weights){double total=weights.Sum();if(weights.Length==0||total<=0||double.IsNaN(total)||double.IsInfinity(total)||weights.Any(w=>w<0||double.IsNaN(w)||double.IsInfinity(w)))throw new InvalidOperationException("Invalid draw weights");double pick=random.NextDouble()*total;for(int i=0;i<weights.Length;i++){pick-=weights[i];if(pick<0)return i;}return weights.Length-1;}

        private Card GenerateStartingCard(int number,CardEffect effect){var c=setup==null?null:setup.CardPool.FirstOrDefault(t=>t.Number==number&&t.Effect==effect);if(c==null&&setup!=null)c=setup.CardPool.FirstOrDefault(t=>t.Effect==effect);var result=c==null?new Card {Number=number,Effect=effect}:c.CopyAsNewInstance();result.Number=number;result.AttackBase=BaseAttack;return result;}

        private void Shuffle(List<Card> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            { int j = random.Next(i + 1); Card t = cards[i]; cards[i] = cards[j]; cards[j] = t; }
        }

        public bool AddCard(Card card)
        {
            if (Phase != GamePhase.Planning || PlayerActionCount >= PlayerActionLimit || !Hand.Contains(card) || Timeline.Any(a => a.Card == card)) return false;
            Timeline.Add(new BattleAction { Card = card, Number = card.Number, Effect = card.Effect });
            return true;
        }

        public bool InsertCard(Card card, int index)
        {
            if (index < 0 || index > Timeline.Count || !AddCard(card)) return false;
            var action = Timeline[Timeline.Count - 1];
            Timeline.RemoveAt(Timeline.Count - 1); Timeline.Insert(index, action);
            return true;
        }

        public void MoveAction(int index, int offset)
        {
            int target = index + offset;
            if (Phase != GamePhase.Planning || index < 0 || index >= Timeline.Count || target < 0 || target >= Timeline.Count) return;
            var action = Timeline[index]; Timeline.RemoveAt(index); Timeline.Insert(target, action);
        }

        public void RemoveAction(int index)
        {
            if (Phase == GamePhase.Planning && index >= 0 && index < Timeline.Count && Timeline[index].Card != null) Timeline.RemoveAt(index);
        }
    }
}
