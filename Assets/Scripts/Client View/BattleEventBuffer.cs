using System.Collections.Generic;

public sealed class BattleEventBuffer
{
    public readonly List<BattleDamageEvent> DamageEvents = new();

    public void AddDamage(BattleDamageEvent damageEvent)
    {
        DamageEvents.Add(damageEvent);
    }

    public void Clear()
    {
        DamageEvents.Clear();
    }
}

public struct BattleDamageEvent
{
    public string SourceBattleInstanceId;
    public string TargetBattleInstanceId;
    public int Amount;
    public DamageDelivery Delivery;
}