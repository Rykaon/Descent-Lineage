using UnityEngine;

public class DeathSystem
{
    private BattleSystem BattleSystem;

    public void Initialize(BattleSystem battleSystem)
    {
        BattleSystem = battleSystem;
    }

    public void Tick()
    {
        foreach (var unit in BattleSystem.BattleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            ApplyPendingDamage(unit);

            CheckDeath(unit);
        }
    }

    private void ApplyPendingDamage(BattleUnitInstance unit)
    {
        if (unit.PendingDamageContexts.Count == 0)
        {
            return;
        }

        foreach (DamageContext context in unit.PendingDamageContexts)
        {
            int damage = context.FinalDamage;

            if (damage <= 0)
            {
                continue;
            }

            unit.CurrentHealth -= damage;

            BattleSystem.EventBuffer.AddDamage(new BattleDamageEvent
            {
                SourceBattleInstanceId = context.Attacker != null ? context.Attacker.BattleInstanceId : null,
                TargetBattleInstanceId = unit.BattleInstanceId,
                Amount = damage,
                Delivery = context.Delivery
            });
        }

        unit.PendingDamageContexts.Clear();
    }

    private void CheckDeath(BattleUnitInstance unit)
    {
        if (unit.CurrentHealth > 0)
        {
            return;
        }

        BattleUnitInstance killer = BattleSystem.BattleState.GetUnitByBattleId(unit.LastDamageSourceBattleInstanceId);

        Kill(unit);

        BattleSystem.EffectSystem.UnitDeath(unit, killer);

        foreach (var other in BattleSystem.BattleState.Units)
        {
            other.RejectedTargets.Remove(unit.BattleInstanceId);
        }
    }

    private void Kill(BattleUnitInstance unit)
    {
        unit.IsDead = true;
        unit.NavPresence = NavPresence.None;
        unit.IsEngaged = false;
        unit.CurrentHealth = 0;
        unit.TimeWithoutMoving = 0f;
        unit.TimeSinceNotEngaged = 0f;
        unit.HasDesiredAttackPosition = false;
        unit.DesiredPath.Clear();
        unit.CurrentTargetBattleInstanceId = null;
        unit.DesiredVelocity = Vector2.zero;
        unit.FinalVelocity = Vector2.zero;

        unit.ClearHexPath();
    }

    public void Clear()
    {

    }
}
