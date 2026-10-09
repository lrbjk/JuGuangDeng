namespace RainbowLi
{
    public sealed partial class ContainmentGame
    {
        public bool BuyUpgrade(Card card, bool engrave)
        {
            int cost = engrave ? 20 : 12;
            if (Phase != GamePhase.Shop || Coins < cost || !Deck.Contains(card) || !engrave&&!card.CanUpgrade || engrave && card.Engraved) return false;
            Coins -= cost; if (engrave) card.Engraved = true; else card.Upgrade++;
            Message = engrave ? "铭刻完成，每张牌最多一次。" : "卡牌效果 +2。"; return true;
        }

        private void ApplyEngraving(Card card, bool matched)
        {
            if (!matched || card == null || !card.Engraved) return;
            Damage(true, card.EngravingDamage);
            Log("铭刻触发：额外伤害 " + card.EngravingDamage);
        }
    }
}
