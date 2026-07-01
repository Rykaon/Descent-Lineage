using UnityEngine;

public enum BattleStatModifierType
{
    Add,
    Multiply,
    Override
}

public enum BattleStatType
{
    HealthPoints,
    AttackSpeed,
    SlashOffense,
    ImpactOffense,
    SlashDefense,
    ImpactDefense,
    MoveSpeed
}

public sealed class BattleStatModifier
{
    public BattleStatType StatType;
    public BattleStatModifierType ModifierType;
    public float Value;

    public BattleStatModifier(BattleStatType statType, BattleStatModifierType modifierType, float value)
    {
        StatType = statType;
        ModifierType = modifierType;
        Value = value;
    }
}