using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi
{
    public sealed partial class ContainmentGame
    {
        public void StartBattle(bool boss = false, bool elite=false, bool routeBoss=false)
        {
            if (Phase != GamePhase.Map || boss && !routeBoss && Victories < 2) return;
            Boss = boss;Player.Statuses.Clear();battleRandomState=(uint)random.Next(1,int.MaxValue);
            EnemyMaxHealth = setup == null ? (boss ? 110 : 36 + Victories * 12) : (boss ? setup.BossHealth : setup.EnemyHealth + Victories * setup.EnemyHealthGrowth);
            Enemy.StartEncounter(EnemyMaxHealth,boss,elite);
            if(setup!=null)Enemy.ConditionalRules.AddRange(setup.EnemyConditionalRules.Select(c=>new ConditionalSequence {Condition=c.Condition,Threshold=c.Threshold,Rule=CloneRule(c.Rule)}));
            Round = 0; ArenaIndex = 0; ArenaTriggerCount = 0; rotateArena = false;
            ArenaRules.Clear();
            for (int i = 0; i < 3; i++)
                ArenaRules.Add(Rule("场地 / " + (i + 1), RandomPattern(), (RuleEffect)random.Next(0, 3), 4 + i * 2));
            if (setup != null && setup.ArenaRules.Count > 0)
            { ArenaRules.Clear(); ArenaRules.AddRange(setup.ArenaRules.Select(CloneRule)); }
            EnemyRule = Rule("实体 / 共振", boss ? new[] { 6, 6, 6 } : new[] { 6, 5, 4 }, RuleEffect.Damage, boss ? 12 : 6);
            if (setup != null && setup.EnemyRule != null) EnemyRule = CloneRule(setup.EnemyRule);
            drawPile.Clear(); discard.Clear(); Hand.Clear(); pending.Clear();
            drawPile.AddRange(Deck); Shuffle(drawPile);
            History.Clear(); Log("收容开始：" + EnemyName);
            BeginRound();
        }

        private void BeginRound()
        {
            Round++; Player.ResetRound(); Enemy.ResetRound();endTicksQueued=false;processedActions=0;
            StartStatusEffects(Player,false);if(CheckBattleEnd())return;StartStatusEffects(Enemy,true);if(CheckBattleEnd())return;
            if (rotateArena) { ArenaIndex = (ArenaIndex + 1) % 3; rotateArena = false; ArenaTriggerCount = 0; }
            foreach (var rule in PlayerRules.Concat(ArenaRules)) rule.Triggers = 0;
            foreach(var r in Enemy.ActiveRules)r.Triggers=0;
            NumberStream.Clear(); Timeline.Clear(); pending.Clear();
            LastResolvedAction = null; LastAction = "等待行动";
            DrawRoundHand();
            Timeline.AddRange(Enemy.GenerateActions(random,Round,setup));
            requiredEnemyActions.Clear();requiredEnemyActions.AddRange(Timeline);
            Phase = GamePhase.Planning;
            Message = "点击手牌追加行动，再用箭头调整敌我顺序。玩家最多 "+PlayerActionLimit+" 张；实体行动必须全部执行。";
            Log("第 " + Round + " 回合：抽取 " + Hand.Count + " 张，护盾与强化重置。");
        }

        public bool Confirm()
        {
            if (!CanConfirm) {Message="请保留并安排全部敌方技能，再开始执行。";return false;}
            pending.AddRange(Timeline); Phase = GamePhase.Resolving; Message = "行动依次执行，牌序效果立即插入。";
            return true;
        }

        public bool Step()
        {
            if(Phase!=GamePhase.Resolving)return false;
            if(pending.Count==0){
                if(QueueEndTicks())return Step();
                Player.Statuses.EndRound();Enemy.Statuses.EndRound();discard.AddRange(Hand);Hand.Clear();BeginRound();return false;
            }
            var action=pending[0];pending.RemoveAt(0);LastResolvedAction=action;LastAction=action.Label;Log(LastAction);
            if(!action.IsStatusTick&&++processedActions>4096){pending.Clear();Message="本回合连锁达到 4096 次，结束剩余连锁。";Log(Message);return false;}
            if(action.IsStatusTick){ApplyStatusTick(action);ActivateEnemyConditions();CheckBattleEnd();return true;}
            bool behavior=!action.IsNumberOnly;
            if(behavior&&!BleedBefore(action)){CheckBattleEnd();return true;}
            if(action.Rule!=null){ApplyRule(action);AfterBehavior(action);ActivateEnemyConditions();}
            else{
                if(behavior)ApplyCardEffect(action);
                ActivateEnemyConditions();
                if(Health>0&&EnemyHealth>0){if(behavior)AfterBehavior(action);FillNumber(action);}
            }
            CheckBattleEnd();return true;
        }

        private void ActivateEnemyConditions(){if(Health<=0||EnemyHealth<=0)return;foreach(var special in Enemy.ActivateSpecials(Round,setup)){pending.Add(special);Timeline.Add(special);Log("条件达成，特殊技能加入本回合："+special.Label);}}

        private void Damage(bool toEnemy, int value)
        {
            Actor(toEnemy).ReceiveDamage(Actor(!toEnemy).OutgoingDamage(value));
        }
    }
}
