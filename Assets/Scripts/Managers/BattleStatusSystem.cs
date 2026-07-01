using System.Collections.Generic;

public sealed class BattleStatusSystem
{
    private BattleSystem BattleSystem;

    public void Initialize(BattleSystem battleSystem)
    {
        BattleSystem = battleSystem;
    }

    public void Tick(float deltaTime)
    {
        foreach (var unit in BattleSystem.BattleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            TickUnitStatuses(unit, deltaTime);
        }
    }

    private void TickUnitStatuses(BattleUnitInstance unit, float deltaTime)
    {
        for (int i = unit.Statuses.Count - 1; i >= 0; i--)
        {
            BattleStatusRuntime status = unit.Statuses[i];

            status.Tick(BattleSystem, deltaTime);

            if (!status.IsExpired)
            {
                continue;
            }

            status.OnExpire(BattleSystem);
            unit.Statuses.RemoveAt(i);
        }
    }

    public void ApplyStatus(BattleStatusRuntime newStatus)
    {
        BattleSystem.EffectSystem.BeforeApplyStatus(newStatus);

        BattleUnitInstance target = newStatus.TargetUnit;

        for (int i = 0; i < target.Statuses.Count; i++)
        {
            BattleStatusRuntime existing = target.Statuses[i];

            if (existing.StatusId != newStatus.StatusId)
            {
                continue;
            }

            if (existing.TryMergeWith(newStatus))
            {
                return;
            }

            if (!existing.CanStackWith(newStatus))
            {
                existing.RefreshFrom(newStatus);
                return;
            }
        }

        target.Statuses.Add(newStatus);
    }

    public void Clear()
    {

    }
}