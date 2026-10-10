namespace RainbowLi
{
    public sealed partial class ContainmentGame
    {
        public bool BuyUpgrade(Card card, bool engrave)
        {
            int cost = engrave ? (setup?.EngravingPrice??20) : (setup?.UpgradePrice??12);
            if (Phase != GamePhase.Shop || Coins < cost || !Deck.Contains(card) || !engrave&&!card.CanUpgrade || engrave && card.Engraved) return false;
            var engraving=engrave?DrawEngraving(setup?.EngravingPoolName):null;
            Coins -= cost; if (engrave) {card.Engraved = true;card.Engraving=engraving;card.EngravingCode=engraving.Code;} else card.Upgrade++;
            Message = engrave ? "铭刻完成，每张牌最多一次。" : "卡牌效果 +2。"; return true;
        }

        private void ApplyEngraving(Card card, bool matched,bool enemy)
        {
            if (card == null || !card.Engraved) return;
            if(!matched&&(card.Engraving==null||card.Engraving.Effect==EngravingEffect.FollowUpDamage))return;
            var e=card.Engraving;string code=e?.Code??card.EngravingCode??"LegacyEngraving";
            if(e==null||e.Effect==EngravingEffect.FollowUpDamage)SettleDamage(enemy,!enemy,e==null?card.EngravingDamage:e.Value,code,BattleEventSource.Engraving);
            else if(e.Effect==EngravingEffect.Poison||e.Effect==EngravingEffect.Burn)ApplyStatus(enemy,!enemy,e.Effect==EngravingEffect.Poison?CardEffect.Poison:CardEffect.Burn,e.Value,code,BattleEventSource.Engraving);
            else if(e.Effect==EngravingEffect.PoisonExplosion||e.Effect==EngravingEffect.BurnExplosion){
                var status=e.Effect==EngravingEffect.PoisonExplosion?CardEffect.Poison:CardEffect.Burn;
                int layers=Actor(!enemy).Statuses[status];ChangeStatus(!enemy,status,0,code);
                SettleDamage(enemy,!enemy,CombatResolver.Clamp(e.Effect==EngravingEffect.PoisonExplosion?3L*layers/2:2L*layers),code,BattleEventSource.Engraving);
            }
            Log("铭刻触发：额外伤害 " + card.EngravingDamage);
        }
    }
}
