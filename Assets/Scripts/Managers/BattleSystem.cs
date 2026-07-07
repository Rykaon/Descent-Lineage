using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleSystem
{
    public SharedBoardState BoardState { get; private set; }
    public BattleState BattleState { get; private set; }
    public BattleStateBuilder BattleStateBuilder { get; private set; }

    public BattleHexGrid HexGrid { get; private set; }
    public BattleHexOccupation HexOccupation { get; private set; }
    public BattleHexPathfinder HexPathfinder { get; private set; }

    public TargetingSystem TargetingSystem { get; private set; }
    public HexAttackPositionSystem HexAttackPositionSystem { get; private set; }
    public HexMovementSystem HexMovementSystem { get; private set; }
    public HexEngagementSystem HexEngagementSystem { get; private set; }
    public AttackSystem AttackSystem { get; private set; }
    public DeathSystem DeathSystem { get; private set; }
    public BattleEffectSystem EffectSystem { get; private set; }
    public BattleStatusSystem StatusSystem { get; private set; }
    public BattleModifierSystem ModifierSystem { get; private set; }
    public BattleAbilitySystem AbilitySystem { get; private set; }
    public BattleCladeSystem CladeSystem { get; private set; }
    public BattleEventBuffer EventBuffer { get; private set; }

    public IUnitDefinitionDatabase unitDatabase { get; private set; }
    public IMutationDefinitionDatabase mutationDatabase { get; private set; }
    public ICladeDefinitionDatabase cladeDatabase { get; private set; }

    public event Action<BattleUnitInstance, int, DamageDelivery> OnDamageApplied;

    public void Initialize(IUnitDefinitionDatabase unitDatabase, IMutationDefinitionDatabase mutationDatabase, ICladeDefinitionDatabase cladeDatabase, SharedBoardState state)
    {
        BoardState = state;
        this.unitDatabase = unitDatabase;
        this.mutationDatabase = mutationDatabase;
        this.cladeDatabase = cladeDatabase;

        BattleStateBuilder = new BattleStateBuilder();

        HexGrid = new BattleHexGrid(BoardState);

        HexOccupation = new BattleHexOccupation(HexGrid);
        HexPathfinder = new BattleHexPathfinder();

        HexPathfinder.Initialize(this);

        TargetingSystem = new TargetingSystem();
        HexAttackPositionSystem = new HexAttackPositionSystem();
        HexMovementSystem = new HexMovementSystem();
        HexEngagementSystem = new HexEngagementSystem();
        AttackSystem = new AttackSystem();
        DeathSystem = new DeathSystem();
        EffectSystem = new BattleEffectSystem();
        StatusSystem = new BattleStatusSystem();
        ModifierSystem = new BattleModifierSystem();
        AbilitySystem = new BattleAbilitySystem();
        CladeSystem = new BattleCladeSystem();
        EventBuffer = new BattleEventBuffer();

        TargetingSystem.Initialize(this);
        HexAttackPositionSystem.Initialize(this);
        HexMovementSystem.Initialize(this);
        HexEngagementSystem.Initialize(this);
        AttackSystem.Initialize(this);
        DeathSystem.Initialize(this);
        EffectSystem.Initialize(this);
        StatusSystem.Initialize(this);
        ModifierSystem.Initialize(this);
        AbilitySystem.Initialize(this);
        CladeSystem.Initialize(this);
    }

    public void StartServerBattle(GameState gameState)
    {
        ClearBattle();

        BattleState = BattleStateBuilder.Build(gameState, this);
        CladeSystem.BuildClades();
        EffectSystem.BuildEffectsForBattle();
    }

    public void SetClientBattleState(BattleState battleState)
    {
        ClearBattle();

        BattleState = battleState;
        EffectSystem.BuildEffectsForBattle();
    }

    public void Tick(float deltaTime)
    {
        HexOccupation.Rebuild(BattleState);

        StatusSystem.Tick(deltaTime);
        ModifierSystem.Tick(deltaTime);
        CladeSystem.Tick(deltaTime);

        TargetingSystem.Tick(deltaTime);
        HexAttackPositionSystem.Tick(deltaTime);
        HexMovementSystem.Tick(deltaTime);
        HexEngagementSystem.Tick(deltaTime);
        EffectSystem.Tick(deltaTime);
        AttackSystem.Tick(deltaTime);
        AbilitySystem.Tick(deltaTime);
        DeathSystem.Tick();
    }

    public void RaiseDamageApplied(BattleUnitInstance target, int damage, DamageDelivery delivery)
    {
        OnDamageApplied?.Invoke(target, damage, delivery);
    }

    public bool TryGetWinner(BattleState battleState, out int winnerPlayerId)
    {
        winnerPlayerId = -1;

        HashSet<int> livingPlayers = new();

        foreach (var unit in battleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            livingPlayers.Add(unit.OwnerPlayerId);
        }

        if (livingPlayers.Count != 1)
        {
            return false;
        }

        winnerPlayerId = livingPlayers.First();

        return true;
    }

    public void ClearBattle()
    {
        BattleState?.Clear();

        BattleState = null;

        TargetingSystem.Clear();
        HexAttackPositionSystem.Clear();
        HexMovementSystem.Clear();
        HexEngagementSystem.Clear();
        AttackSystem.Clear();
        DeathSystem.Clear();
        EffectSystem.Clear();
        StatusSystem.Clear();
        AbilitySystem.Clear();
        CladeSystem.Clear();
        ModifierSystem.Clear();
        EventBuffer.Clear();
    }
}

public sealed class BattleUnitInstance
{
    public string BattleInstanceId;
    public string BoardInstanceId;
    public string DefinitionId;

    public int OwnerPlayerId;

    public Vector2 LastPosition;
    public Vector2 Position;

    public List<string> MutationIds = new();
    public readonly List<BattleEffectRuntime> Effects = new();
    public readonly List<BattleStatusRuntime> Statuses = new();
    public readonly Dictionary<string, float> CladeEffectTimers = new();

    public int MaxHealth;
    public int CurrentHealth;

    public BaseStats BaseStats;
    public BaseStats CurrentStats;

    public string AbilityId;

    public DamageProfile BasicAttackDamageProfile;
    public DamageProfile AbilityDamageProfile;

    public AttackRangeTier AttackRangeTier;
    public CollisionBodyPreset CollisionBodyPreset;

    public readonly List<DamageContext> PendingDamageContexts = new();

    public int CurrentMana;
    public float AttackCooldownRemaining;
    public readonly Dictionary<string, int> CladeStacks = new();

    public float TimeWithoutMoving;

    public bool IsEngaged;
    public bool IsDead;

    public NavPresence NavPresence = NavPresence.AgentOnly;
    public float RepathTimer;

    public readonly List<Vector2> DesiredPath = new();
    public Vector2 LastPathTargetPosition;

    public string CurrentTargetBattleInstanceId;
    public Vector2 CurrentMoveDestination;
    public Vector2 Forward;

    public readonly Dictionary<string, float> RejectedTargets = new();

    public readonly Dictionary<Vector2, float> RejectedAttackSlots = new();
    public string LastTargetBattleInstanceId;

    public Vector2 DesiredVelocity;
    public Vector2 FinalVelocity;

    public float DebugBlockedTimer;
    public Vector2 DebugLastPosition;

    public BattleUnitDecision Decision;
    public string DecisionReason;

    public bool HasDesiredAttackPosition;
    public Vector2 DesiredAttackPosition;








    public BattleHexCoord CurrentHex;
    public BattleHexCoord DesiredHex;

    public readonly List<BattleHexCoord> HexPath = new();
    public bool HasHexPath => HexPath != null && HexPathIndex >= 0 && HexPathIndex < HexPath.Count;
    public int HexPathIndex;

    public BattleHexCoord ReservedAttackHex;
    public bool HasReservedAttackHex;
    public string ReservedAttackTargetId;
    public float HexMoveBudget;
    public float HexRepathCooldown;

    public bool HasPreparedHexMove;
    public BattleHexCoord PreparedHexMove;

    public string LastDamageSourceBattleInstanceId;

    public bool HasStatus(BattleStatusId statusId)
    {
        return GetStatus(statusId) != null;
    }

    public BattleStatusRuntime GetStatus(BattleStatusId statusId)
    {
        for (int i = 0; i < Statuses.Count; i++)
        {
            BattleStatusRuntime status = Statuses[i];

            if (status.StatusId == statusId)
            {
                return status;
            }
        }

        return null;
    }

    public bool RemoveStatus(BattleStatusId statusId)
    {
        for (int i = Statuses.Count - 1; i >= 0; i--)
        {
            BattleStatusRuntime status = Statuses[i];

            if (status.StatusId != statusId)
            {
                continue;
            }

            Statuses.RemoveAt(i);
            return true;
        }

        return false;
    }

    public int RemoveAllStatuses(BattleStatusId statusId)
    {
        int removedCount = 0;

        for (int i = Statuses.Count - 1; i >= 0; i--)
        {
            BattleStatusRuntime status = Statuses[i];

            if (status.StatusId != statusId)
            {
                continue;
            }

            Statuses.RemoveAt(i);
            removedCount++;
        }

        return removedCount;
    }

    public bool IsCamouflaged()
    {
        return HasStatus(BattleStatusId.Camouflage);
    }

    public void ClearDecision()
    {
        Decision = BattleUnitDecision.None;
        DecisionReason = null;
    }

    public void ClearSlotFailureMemory()
    {
        RejectedAttackSlots.Clear();
    }

    public void ClearTargetFailureMemory()
    {
        RejectedTargets.Clear();
    }

    public void ResetNavigationState()
    {
        ClearHexPath();

        HasReservedAttackHex = false;
        ReservedAttackHex = default;
    }

    public void OnEngaged(bool value)
    {
        IsEngaged = value;

        if (IsEngaged)
        {
            ResetNavigationState();

            ClearSlotFailureMemory();
            ClearTargetFailureMemory();

            Decision = BattleUnitDecision.Attack;
            DecisionReason = "Target in attack range";
        }
    }

    public void ClearHexPath()
    {
        HexPath.Clear();
        HexPathIndex = 0;
        DesiredHex = CurrentHex;
    }
}

public struct BattlePosition
{
    public Vector2 Value; 
    public BattlePosition(Vector2 value) 
    {
        Value = value;
    }
}

public static class BattleDebugDraw
{
    public static bool Enabled = true;

    public static bool DrawAttackState = true;
    public static bool DrawPaths = true;
    public static bool DrawVelocities = true;
    public static bool DrawObstacleShapes = true;
    public static bool DrawReservedSlots = true;

    public static void DrawUnitDebug(
        BattleUnitInstance unit,
        BattleState state)
    {
        if (!Enabled || unit.IsDead)
            return;

        if (DrawAttackState)
            DrawEngagement(unit, state);

        if (DrawVelocities)
            DrawVelocitiesDebug(unit);

        if (DrawObstacleShapes)
            DrawNavObstacleShape(unit);
    }

    private static void DrawEngagement(
        BattleUnitInstance unit,
        BattleState state)
    {
        var target = state.GetUnitByBattleId(unit.CurrentTargetBattleInstanceId);

        if (target == null || target.IsDead)
            return;

        Color color = unit.IsEngaged ? Color.red : Color.yellow;

        Debug.DrawLine(
            To3D(unit.Position, 0.15f),
            To3D(target.Position, 0.15f),
            color);
    }

    private static void DrawVelocitiesDebug(BattleUnitInstance unit)
    {
        if (unit.DesiredVelocity.sqrMagnitude > 0.001f)
        {
            Debug.DrawRay(
                To3D(unit.Position, 0.2f),
                To3DVector(unit.DesiredVelocity.normalized * 0.4f),
                Color.blue);
        }

        if (unit.FinalVelocity.sqrMagnitude > 0.001f)
        {
            Debug.DrawRay(
                To3D(unit.Position, 0.25f),
                To3DVector(unit.FinalVelocity.normalized * 0.4f),
                Color.magenta);
        }
    }

    private static void DrawNavObstacleShape(BattleUnitInstance unit)
    {
        if (unit.NavPresence != NavPresence.HardObstacle)
            return;

        if (unit.CurrentStats.CollisionShape == CollisionShapeType.Circle)
        {
            DrawCircle(
                unit.Position,
                unit.CurrentStats.CollisionRadius,
                Color.red);
        }
        else
        {
            DrawCapsule(
                unit.Position,
                unit.Forward,
                unit.CurrentStats.CollisionRadius*0.5f,
                unit.CurrentStats.CollisionHalfLength*0.5f,
                Color.red);
        }
    }

    public static void DrawCircle(
        Vector2 center,
        float radius,
        Color color,
        int segments = 24)
    {
        float step = Mathf.PI * 2f / segments;

        Vector2 previous = center + new Vector2(radius, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * step;
            Vector2 current = center + new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius);

            Debug.DrawLine(
                To3D(previous, 0.3f),
                To3D(current, 0.3f),
                color);

            previous = current;
        }
    }

    public static void DrawCapsule(
        Vector2 center,
        Vector2 forward,
        float radius,
        float halfLength,
        Color color,
        int segments = 12)
    {
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector2.up;

        forward.Normalize();

        Vector2 right = new Vector2(-forward.y, forward.x);

        Vector2 a = center - forward * halfLength;
        Vector2 b = center + forward * halfLength;

        Debug.DrawLine(To3D(a + right * radius, 0.3f), To3D(b + right * radius, 0.3f), color);
        Debug.DrawLine(To3D(a - right * radius, 0.3f), To3D(b - right * radius, 0.3f), color);

        DrawArc(a, -forward, radius, color, segments);
        DrawArc(b, forward, radius, color, segments);
    }

    private static void DrawArc(
        Vector2 center,
        Vector2 forward,
        float radius,
        Color color,
        int segments)
    {
        Vector2 right = new Vector2(-forward.y, forward.x);

        Vector2 previous = center + right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(0f, Mathf.PI, t);

            Vector2 dir =
                Mathf.Cos(angle) * right
                + Mathf.Sin(angle) * forward;

            Vector2 current = center + dir * radius;

            Debug.DrawLine(
                To3D(previous, 0.3f),
                To3D(current, 0.3f),
                color);

            previous = current;
        }
    }

    private static void DrawCross(
        Vector2 position,
        float size,
        Color color)
    {
        Debug.DrawLine(
            To3D(position + Vector2.left * size, 0.35f),
            To3D(position + Vector2.right * size, 0.35f),
            color);

        Debug.DrawLine(
            To3D(position + Vector2.down * size, 0.35f),
            To3D(position + Vector2.up * size, 0.35f),
            color);
    }

    private static Vector3 To3D(Vector2 position, float y)
    {
        return new Vector3(position.x, y, position.y);
    }

    private static Vector3 To3DVector(Vector2 vector)
    {
        return new Vector3(vector.x, 0f, vector.y);
    }
}