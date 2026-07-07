using System.Collections.Generic;

public sealed class BattleEventBuffer
{
    public readonly List<BattleDamageEvent> DamageEvents = new();
    public readonly List<BattleManaEvent> ManaEvents = new();
    public readonly List<BattleHealEvent> HealEvents = new();

    public void AddDamage(BattleDamageEvent damageEvent)
    {
        DamageEvents.Add(damageEvent);
    }

    public void AddManaEvent(BattleUnitInstance target, int amount)
    {
        ManaEvents.Add(new BattleManaEvent
        {
            TargetBattleInstanceId = target.BattleInstanceId,
            CurrentMana = target.CurrentMana,
            MaxMana = target.CurrentStats.ManaMax,
            Amount = amount
        });
    }

    public void AddHealEvent(BattleUnitInstance target, int amount)
    {
        HealEvents.Add(new BattleHealEvent
        {
            TargetBattleInstanceId = target.BattleInstanceId,
            Amount = amount,
            CurrentHealth = target.CurrentHealth,
            MaxHealth = target.MaxHealth
        });
    }

    public void Clear()
    {
        DamageEvents.Clear();
        ManaEvents.Clear();
        HealEvents.Clear();
    }
}

public struct BattleDamageEvent
{
    public string SourceBattleInstanceId;
    public string TargetBattleInstanceId;
    public int Amount;
    public DamageDelivery Delivery;
}

public struct BattleManaEvent
{
    public string TargetBattleInstanceId;
    public int CurrentMana;
    public int MaxMana;
    public int Amount;
}

public struct BattleHealEvent
{
    public string TargetBattleInstanceId;
    public int Amount;
    public int CurrentHealth;
    public int MaxHealth;
}