using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleState
{
    private BattleSystem BattleSystem;
    private BattleStateBuilder BattleStateBuilder;

    public List<BattleUnitInstance> Units = new();

    public bool IsBattleFinished;
    public int WinningPlayerId = -1;

    public void Clear()
    {
        Units.Clear();
        IsBattleFinished = false;
        WinningPlayerId = -1;
    }

    public BattleUnitInstance GetUnitByBattleId(string battleInstanceId)
    {
        foreach (var unit in Units)
        {
            if (unit.BattleInstanceId == battleInstanceId)
                return unit;
        }

        return null;
    }
}

public sealed class BattleStateBuilder
{
    public BattleState Build(GameState gameState, BattleSystem battleSystem)
    {
        BattleState battleState = new();


        foreach (var player in gameState.Players)
        {
            foreach (var boardUnit in player.Board.Units)
            {
                BattleHexCoord startHex = battleSystem.HexGrid.BoardNodeToBattleCenter(boardUnit.Node);
                Vector2 startPosition = battleSystem.HexGrid.HexToWorld(startHex);

                gameState.SharedBoard.TryGetTile(boardUnit.Node, out BoardTileState unitTile);

                if (unitTile == null)
                {
                    continue;
                }

                if (unitTile.Location == BoardType.Bench)
                {
                    continue;
                }

                BattleUnitInstance battleUnit = new()
                {
                    BattleInstanceId = Guid.NewGuid().ToString("N"),
                    BoardInstanceId = boardUnit.InstanceId,
                    DefinitionId = boardUnit.DefinitionId,
                    OwnerPlayerId = boardUnit.OwnerPlayerId,

                    Position = startPosition,
                    LastPosition = startPosition,

                    MutationIds = new(boardUnit.MutationIds),

                    MaxHealth = battleSystem.unitDatabase.GetUnit(boardUnit.DefinitionId).BaseStats.HealthPoints,
                    CurrentHealth = battleSystem.unitDatabase.GetUnit(boardUnit.DefinitionId).BaseStats.HealthPoints,

                    BaseStats = battleSystem.unitDatabase.GetUnit(boardUnit.DefinitionId).BaseStats,
                    CurrentStats = battleSystem.unitDatabase.GetUnit(boardUnit.DefinitionId).BaseStats,

                    AttackRangeTier = battleSystem.unitDatabase.GetUnit(boardUnit.DefinitionId).AttackRangeTier,
                    CollisionBodyPreset = battleSystem.unitDatabase.GetUnit(boardUnit.DefinitionId).CollisionBodyPreset,

                    BasicAttackDamageProfile = battleSystem.unitDatabase.GetUnit(boardUnit.DefinitionId).BasicAttackDamageProfile,
                    AbilityDamageProfile = battleSystem.unitDatabase.GetUnit(boardUnit.DefinitionId).AbilityDamageProfile,

                    AttackCooldownRemaining = 0,
                    TimeWithoutMoving = 0,

                    IsEngaged = false,
                    IsDead = false,
                    NavPresence = NavPresence.AgentOnly,
                    RepathTimer = UnityEngine.Random.Range(0.1f, 0.3f),
                    LastPathTargetPosition = default,

                    CurrentTargetBattleInstanceId = null,
                    CurrentMoveDestination = default,
                    Forward = default,

                    LastTargetBattleInstanceId = null,

                    DesiredVelocity = default,
                    FinalVelocity = default,

                    DebugBlockedTimer = 0,
                    DebugLastPosition = default,

                    Decision = BattleUnitDecision.None,
                    DecisionReason = null,
                    DesiredAttackPosition = default,
                    HasDesiredAttackPosition = false,

                    NoProgressTimer = 0,
                    LastDistanceToWaypoint = 0,
                    WasEngagedLastFrame = false,
                    TimeSinceNotEngaged = 0,
                    TimeSinceEngaged = 0,

                    DebugStallTimer = 0,

                    NavigationAge = 0,
                    LastNoProgressPosition = default,
                    HasLastNoProgressPosition = false,
                    PhysicalBlockCooldown = 0,

                    AvoidanceSide = 0,
                    AvoidanceSideLockTimer = 0,
                    AvoidanceObstacleId = null,
                    BypassDirection = default,
                    BypassTimer = 0,
                    BypassObstacleId = null,







                    CurrentHex = startHex,
                    DesiredHex = startHex,

                    HexPathIndex = 0,

                    ReservedAttackHex = default,
                    HasReservedAttackHex = false,
                    ReservedAttackTargetId = null,
                    HexMoveBudget = 1,
                    HexRepathCooldown = 0,

                    HasPreparedHexMove = false,
                    PreparedHexMove = default,
                    LastDamageSourceBattleInstanceId = null,
                };

                battleState.Units.Add(battleUnit);
            }
        }

        return battleState;
    }

    private BattleHexCoord BoardNodeToBattleHex(BoardNode node)
    {
        return new BattleHexCoord(q: node.X + 1, r: node.Y);
    }
}