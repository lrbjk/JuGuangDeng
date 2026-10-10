namespace RainbowLi
{
    public enum BattleSide { Player, Enemy }
    public enum BattleEventKind { Damage, HealthLoss, Healing, ShieldChanged, PowerChanged, StatusApplied, StatusChanged, StatusRemoved, Parry, Death }
    public enum BattleEventSource { Card, EnemySkill, Sequence, Engraving, Status, Round, Encounter, Service }
    public enum BattleEventTiming { Action, BeforeAction, AfterAction, RoundStart, RoundEnd, EncounterStart, Service }

    // Immutable values, never references to live cards, actions or actor states.
    public sealed class BattleEvent
    {
        public long Id { get; }
        public BattleEventKind Kind { get; }
        public BattleSide SourceSide { get; }
        public BattleSide TargetSide { get; }
        public BattleEventSource Source { get; }
        public BattleEventTiming Timing { get; }
        public string EffectCode { get; }
        public string CardInstanceId { get; }
        public CardEffect? Status { get; }
        public EffectLifetime? Lifetime { get; }
        public int RequestedAmount { get; }
        public int ResolvedAmount { get; }
        public int Amount { get; }
        public int Before { get; }
        public int After { get; }
        public int Delta => After - Before;
        public int Blocked { get; }
        public bool Parried { get; }
        public int HealthBefore { get; }
        public int HealthAfter { get; }
        public int ShieldBefore { get; }
        public int ShieldAfter { get; }
        public int Round { get; }
        public int Slot { get; }
        public int ActionOrder { get; }
        public int EffectIndex { get; }
        public bool IsRepeat { get; }
        public int RuleTriggerCount { get; }

        internal BattleEvent(long id, BattleEventKind kind, BattleSide sourceSide, BattleSide targetSide,
            BattleEventSource source, BattleEventTiming timing, string code, string cardId,
            int requested, int resolved, int amount, int before, int after, int blocked, bool parried,
            int healthBefore, int healthAfter, int shieldBefore, int shieldAfter,
            int round, int slot, int order, int effectIndex, bool repeat, int triggers,
            CardEffect? status = null, EffectLifetime? lifetime = null)
        {
            Id=id; Kind=kind; SourceSide=sourceSide; TargetSide=targetSide; Source=source; Timing=timing;
            EffectCode=code; CardInstanceId=cardId; RequestedAmount=requested; ResolvedAmount=resolved;
            Amount=amount; Before=before; After=after; Blocked=blocked; Parried=parried;
            HealthBefore=healthBefore; HealthAfter=healthAfter; ShieldBefore=shieldBefore; ShieldAfter=shieldAfter;
            Round=round; Slot=slot; ActionOrder=order; EffectIndex=effectIndex; IsRepeat=repeat;
            RuleTriggerCount=triggers; Status=status; Lifetime=lifetime;
        }
    }
}
