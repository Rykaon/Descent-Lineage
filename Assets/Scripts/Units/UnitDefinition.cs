using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UnitDefinition
{
    public string Id;
    public string DisplayName;
    public int Cost;
    public BaseStats BaseStats;
    public DamageProfile BasicAttackDamageProfile;
    public DamageProfile AbilityDamageProfile;
    public AttackRangeTier AttackRangeTier;
    public CollisionBodyPreset CollisionBodyPreset;
    public string[] Clades;
    public string[] InnateKeywordIds;
    public string[] EligibleMutationIds;
    public int MaxPoolCount;
}

[Serializable]
public sealed class BaseStats
{
    public int HealthPoints;
    public float AttackSpeed;
    public int SlashOffense;
    public int ImpactOffense;
    public int SlashDefense;
    public int ImpactDefense;
    public float MoveSpeed;
    public float AttackRange;
    public CollisionShapeType CollisionShape;
    public float CollisionRadius;
    public float CollisionHalfLength;
}

[Serializable]
public struct DamageWeight
{
    public DamageType Type;
    public float Weight;
}

[Serializable]
public sealed class DamageProfile
{
    public DamageWeight[] Weights;
}

public static class BattleGeometryPresets
{
    public static float GetAttackRange(AttackRangeTier tier)
    {
        return tier switch
        {
            AttackRangeTier.Melee => 0.65f,
            AttackRangeTier.Short => 1.75f,
            AttackRangeTier.Medium => 2.85f,
            AttackRangeTier.Long => 3.95f,
            AttackRangeTier.VeryLong => 4.05f,
            _ => 0.40f
        };
    }

    public static void ApplyCollisionPreset(CollisionBodyPreset preset, BaseStats stats)
    {
        switch (preset)
        {
            case CollisionBodyPreset.SmallCircle:
                stats.CollisionShape = CollisionShapeType.Circle;
                stats.CollisionRadius = 0.10f;
                stats.CollisionHalfLength = 0f;
                break;

            case CollisionBodyPreset.MediumCircle:
                stats.CollisionShape = CollisionShapeType.Circle;
                stats.CollisionRadius = 0.20f;
                stats.CollisionHalfLength = 0f;
                break;

            case CollisionBodyPreset.LargeCircle:
                stats.CollisionShape = CollisionShapeType.Circle;
                stats.CollisionRadius = 0.30f;
                stats.CollisionHalfLength = 0f;
                break;

            case CollisionBodyPreset.SmallCapsule:
                stats.CollisionShape = CollisionShapeType.Capsule;
                stats.CollisionRadius = 0.10f;
                stats.CollisionHalfLength = 0.15f;
                break;

            case CollisionBodyPreset.MediumCapsule:
                stats.CollisionShape = CollisionShapeType.Capsule;
                stats.CollisionRadius = 0.20f;
                stats.CollisionHalfLength = 0.30f;
                break;

            case CollisionBodyPreset.LargeCapsule:
                stats.CollisionShape = CollisionShapeType.Capsule;
                stats.CollisionRadius = 0.30f;
                stats.CollisionHalfLength = 0.45f;
                break;
        }
    }
}

public sealed class MutationDefinition
{
    public string Id;
    public string DisplayName;

    public List<BiomeType> EligibleBiomes = new();

    public List<string> GrantedKeywordIds = new();

    public string Description;
}

public sealed class FaunaDefinition
{
    public string Id;
    public string DisplayName;
    public int FossilValue;
    public List<BiomeType> EligibleBiomes = new();
    public List<MutationDefinition> EligibleMutations = new();
    public int Cost;

    public int MaxPoolCount;
}