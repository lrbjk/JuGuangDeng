using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi
{
    public sealed partial class ContainmentGame
    {
        public void OpenRouteEvent(){if(Phase==GamePhase.Map){Phase=GamePhase.Event;Message="选择一个事件奖励";}}
        public void ResolveRouteEvent(bool heal,int amount){if(Phase!=GamePhase.Event)return;BeginSettlement(BattleEventTiming.Service);if(heal)SettleHealing(false,false,amount,"RouteHealing",BattleEventSource.Service);else Coins+=Math.Max(0,amount);Phase=GamePhase.Map;Message="事件已完成，选择下一层路线。";}

        private void FinishBattle()
        {
            pending.Clear(); Coins += Boss ? 50 : Elite ? EliteGold : 25; Victories++; RestAvailable = true;
            if (Boss) { Phase = GamePhase.Won; Message = "核心实体已收容，原型通关！"; return; }
            Rewards.Clear(); for (int i = 0; i < (setup?.RewardCount??3); i++) Rewards.Add(GenerateCard());
            if(setup!=null&&setup.SequencePool.Count>0)AddAvailableEffect(DrawSequenceEffect(setup.RewardSequencePool));
            if (random.NextDouble() < 0.2)
            {
                var candidates = Deck.Where(c => !c.Engraved).ToList();
                if (candidates.Count > 0) { var card=candidates[random.Next(candidates.Count)];card.Engraving=DrawEngraving(setup?.EngravingPoolName);card.EngravingCode=card.Engraving.Code;card.Engraved=true; Log("稀有战利品：随机一张牌获得铭刻。"); }
            }
            Phase = GamePhase.Reward; Message = "收容成功，获得金币。选择一张生成卡牌加入牌组。";
        }

        public void TakeReward(int index)
        {
            if (Phase != GamePhase.Reward || index < 0 || index >= Rewards.Count) return;
            Deck.Add(Rewards[index]); Rewards.Clear(); Phase = GamePhase.Map; Message = "奖励已加入牌组，继续选择任务。";
        }

        public void OpenService(bool shop)
        {
            if (Phase != GamePhase.Map || !shop && !RestAvailable) return;
            Phase = shop ? GamePhase.Shop : GamePhase.Rest;
            ServiceUsed = false;
        }

        public void LeaveService()
        { if (Phase == GamePhase.Shop || Phase == GamePhase.Rest) Phase = GamePhase.Map; }

        public void RestHeal()
        {
            if (Phase != GamePhase.Rest || ServiceUsed) return;
            BeginSettlement(BattleEventTiming.Service);SettleHealing(false,false,setup?.RestHealing??24,"RestHealing",BattleEventSource.Service); ServiceUsed = true; RestAvailable = false; Message = "恢复 24 生命。";
        }

        public void RestGenerate()
        {
            if (Phase != GamePhase.Rest || ServiceUsed) return;
            Deck.Add(GenerateCard()); ServiceUsed = true; RestAvailable = false; Message = "随机生成一张新卡牌。";
        }

        public void RestRemove(Card card)
        {
            if (Phase != GamePhase.Rest || ServiceUsed || !Deck.Contains(card)) return;
            if(Deck.Count<=6){Message="牌组至少保留 6 张，无法删除。";return;}
            Deck.Remove(card); ServiceUsed = true; RestAvailable = false; Message = "指定卡牌已删除。";
        }

        public void RestRebuild(int index)
        {
            if (Phase != GamePhase.Rest || ServiceUsed || index < 0 || index >= PlayerRules.Count) return;
            PlayerRules[index].Numbers = RandomPattern(); ServiceUsed = true; RestAvailable = false; Message = "已重构牌序数字。";
        }

        public int CardPurchasePrice => setup?.CardPrice ?? 15;
        public bool BuyCard(int number)
        {
            if (Phase != GamePhase.Shop || Coins < CardPurchasePrice || number < 1 || number > 6) return false;
            Coins -= CardPurchasePrice; Deck.Add(GenerateCard(number)); Message = "已生成指定数字卡牌。"; return true;
        }



        public bool BuyRule(int index)
        {
            if (Phase != GamePhase.Shop || Coins < (setup?.RulePrice??18) || index < 0 || index >= PlayerRules.Count) return false;
            var rule = PlayerRules[index];
            if(setup!=null&&setup.SequencePool.Count>0){var effect=DrawSequenceEffect(setup.ShopSequencePool);Coins-=setup.RulePrice;SetRuleEffect(rule,effect);AddAvailableEffect(effect);return true;}
            Coins-=setup?.RulePrice??18;rule.Code=null;rule.BaseX=0;rule.Multiplier=1;rule.FixedValue=0;
            rule.Effect = (RuleEffect)(((int)rule.Effect + 1) % 3);
            rule.Value = rule.Effect == RuleEffect.Power ? 2 : 10;
            rule.InsertsTwo = rule.Effect == RuleEffect.Power;
            AddAvailableEffect(new SequenceEffect {Effect=rule.Effect,Value=rule.Value,InsertsTwo=rule.InsertsTwo,Requirement=rule.Requirement});Message = "牌序效果已替换并加入可选效果池。"; return true;
        }
    }
}
