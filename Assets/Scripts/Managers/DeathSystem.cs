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
        if (unit.PendingDamage <= 0)
        {
            return;
        }

        int damage = unit.PendingDamage;

        unit.CurrentHealth -= damage;
        unit.PendingDamage = 0;

        BattleSystem.RaiseDamageApplied(unit, damage);
    }

    private void CheckDeath(BattleUnitInstance unit)
    {
        if (unit.CurrentHealth > 0)
        {
            return;
        }

        Kill(unit);

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
        unit.Path.Clear();
        unit.CurrentHealth = 0;
        unit.TimeWithoutMoving = 0f;
        unit.TimeSinceNotEngaged = 0f;
        unit.ClearAttackSlotReservation();
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
