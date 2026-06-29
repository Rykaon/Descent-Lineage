using System.Collections.Generic;
using UnityEngine;

public class HexMovementSystem
{
    private BattleSystem BattleSystem;

    private readonly Dictionary<BattleHexCoord, BattleUnitInstance> moveReservations = new();

    public void Initialize(BattleSystem battleSystem)
    {
        BattleSystem = battleSystem;
    }

    public void Tick(float deltaTime)
    {
        moveReservations.Clear();

        foreach (var unit in BattleSystem.BattleState.Units)
        {
            PrepareMove(unit, deltaTime);
        }

        foreach (var unit in BattleSystem.BattleState.Units)
        {
            ApplyPreparedMove(unit);
        }
    }

    private void PrepareMove(BattleUnitInstance unit, float deltaTime)
    {
        unit.HasPreparedHexMove = false;

        if (unit.IsDead || unit.IsEngaged)
        {
            return;
        }

        var target = BattleSystem.BattleState.GetUnitByBattleId(unit.CurrentTargetBattleInstanceId);

        if (target == null || target.IsDead)
        {
            unit.ClearHexPath();
            return;
        }

        if (!unit.HasHexPath)
        {
            return;
        }

        unit.HexMoveBudget += unit.CurrentStats.MoveSpeed * deltaTime;
        unit.HexMoveBudget = Mathf.Min(unit.HexMoveBudget, 2f);

        if (unit.HexMoveBudget < 1f)
        {
            return;
        }

        BattleHexCoord nextHex = unit.HexPath[unit.HexPathIndex];

        if (!BattleSystem.HexGrid.CanTraverse(nextHex, unit, BattleSystem.HexOccupation))
        {
            ClearMoveIntent(unit);
            unit.Decision = BattleUnitDecision.WaitingForPath;
            unit.DecisionReason = "Next hex blocked";
            return;
        }

        if (moveReservations.ContainsKey(nextHex))
        {
            ClearMoveIntent(unit);
            unit.Decision = BattleUnitDecision.WaitingForPath;
            unit.DecisionReason = "Next hex reserved by another unit";
            return;
        }

        moveReservations[nextHex] = unit;

        unit.HasPreparedHexMove = true;
        unit.PreparedHexMove = nextHex;
    }

    private void ApplyPreparedMove(BattleUnitInstance unit)
    {
        if (!unit.HasPreparedHexMove)
        {
            return;
        }

        bool isFinalStep = unit.HexPathIndex >= unit.HexPath.Count - 1;

        bool canMove = isFinalStep ? BattleSystem.HexGrid.CanOccupy(unit.PreparedHexMove, unit, BattleSystem.HexOccupation)
            : BattleSystem.HexGrid.CanTraverse(unit.PreparedHexMove, unit, BattleSystem.HexOccupation);

        if (!canMove)
        {
            ClearMoveIntent(unit);

            unit.Decision = BattleUnitDecision.WaitingForPath;
            unit.DecisionReason = isFinalStep ? "Final attack hex no longer occupable" : "Prepared move no longer traversable";
            return;
        }

        MoveToHex(unit, unit.PreparedHexMove);

        unit.HexPathIndex++;
        unit.HexMoveBudget -= 1f;
        unit.HasPreparedHexMove = false;

        unit.Decision = BattleUnitDecision.Moving;
        unit.DecisionReason = "Prepared hex move applied";
    }

    private void MoveToHex(BattleUnitInstance unit, BattleHexCoord nextHex)
    {
        BattleHexCoord previousHex = unit.CurrentHex;

        unit.CurrentHex = nextHex;
        unit.DesiredHex = nextHex;

        unit.LastPosition = unit.Position;
        unit.Position = BattleSystem.HexGrid.HexToWorld(nextHex);

        Vector2 direction = BattleSystem.HexGrid.HexToWorld(nextHex) - BattleSystem.HexGrid.HexToWorld(previousHex);

        if (direction.sqrMagnitude > 0.001f)
        {
            unit.Forward = direction.normalized;
        }
    }

    private void ClearMoveIntent(BattleUnitInstance unit)
    {
        unit.ClearHexPath();

        unit.HasPreparedHexMove = false;
        unit.PreparedHexMove = default;

        unit.HasReservedAttackHex = false;
        unit.ReservedAttackHex = default;
        unit.ReservedAttackTargetId = null;
    }

    private void Engage(BattleUnitInstance unit, BattleUnitInstance target)
    {
        unit.OnEngaged(true);
        unit.ClearHexPath();

        Vector2 toTarget = BattleSystem.HexGrid.HexToWorld(target.CurrentHex) - BattleSystem.HexGrid.HexToWorld(unit.CurrentHex);

        if (toTarget.sqrMagnitude > 0.001f)
        {
            unit.Forward = toTarget.normalized;
        }
    }

    public void Clear()
    {

    }
}
