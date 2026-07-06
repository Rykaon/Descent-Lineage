using UnityEngine;

public enum GamePhase
{
    None,
    Initialization,
    Setup,
    Preparation,
    PreBattle,
    Battle,
    PostBattle,
    End
}

public enum BiomeType
{
    None,
    Forest,
    Swamp,
    Savanna,
    Coast,
    Mountain
}

public enum UnitCategory
{
    PlayerUnit,
    Wildlife
}

public enum BoardType
{
    None,
    Bench,
    Board
}

public enum DamageType
{
    Slash,
    Impact
}

public enum NavPresence
{
    None,
    AgentOnly,
    HardObstacle
}

public enum CollisionShapeType
{
    Circle,
    Capsule
}

public enum BattleUnitDecision
{
    None,
    Moving,
    NoTarget,
    WaitingForPath,
    WaitingForTarget,
    Attack,
    MoveToReservedSlot,
    MoveToFallbackPosition,
    NoReachableAttackPosition
}

public enum AttackRangeTier
{
    Melee = 1,
    Short = 2,
    Medium = 3,
    Long = 4,
    VeryLong = 5
}

public enum CollisionBodyPreset
{
    SmallCircle,
    MediumCircle,
    LargeCircle,
    SmallCapsule,
    MediumCapsule,
    LargeCapsule
}

public enum NetworkGameRole
{
    Local,
    DedicatedServer,
    Client
}

public enum NetworkPlayerSlot
{
    None = -1,
    PlayerOne = 0,
    PlayerTwo = 1
}

public enum FossilRegistryTarget
{
    LocalPlayer,
    Opponent
}

public enum FossilRegistryViewMode
{
    Local,
    Opponent
}