using UnityEngine;

public class HexEngagementSystem
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
                continue;

            TickUnit(unit, deltaTime);
        }
    }

    private void TickUnit(BattleUnitInstance unit, float deltaTime)
    {
        var target = BattleSystem.BattleState.GetUnitByBattleId(unit.CurrentTargetBattleInstanceId);

        if (target == null || target.IsDead)
        {
            unit.IsEngaged = false;
            return;
        }

        /*Debug.Log(
            $"[RANGE] " +
            $"unit={unit.BattleInstanceId} " +
            $"target={target.BattleInstanceId} " +
            $"range={unit.AttackRangeTier} " +
            $"distance={BattleRangeUtility.DistanceBetweenFootprints(unit, target, BattleSystem)}");*/

        if (BattleRangeUtility.CanAttack(unit, target, BattleSystem))
        {
            unit.OnEngaged(true);
        }
        else
        {
            unit.IsEngaged = false;
        }

        if (BattleRangeUtility.CanAttack(unit, target, BattleSystem))
        {
            unit.OnEngaged(true);
        }
        else
        {
            unit.IsEngaged = false;
        }
    }

    public void Clear()
    {

    }
}