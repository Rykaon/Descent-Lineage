using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitDefinitionAsset", menuName = "Scriptable Objects/UnitDefinitionAsset")]
public sealed class UnitDefinitionAsset : ScriptableObject
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

    public UnitDefinition ToCore()
    {
        BaseStats stats = new BaseStats();

        stats.HealthPoints = BaseStats.HealthPoints;
        stats.AttackSpeed = BaseStats.AttackSpeed;
        stats.SlashOffense = BaseStats.SlashOffense;
        stats.ImpactOffense = BaseStats.ImpactOffense;
        stats.SlashDefense = BaseStats.SlashDefense;
        stats.ImpactDefense = BaseStats.ImpactDefense;
        stats.MoveSpeed = BaseStats.MoveSpeed;
        stats.AttackRange = BattleGeometryPresets.GetAttackRange(AttackRangeTier);
        BattleGeometryPresets.ApplyCollisionPreset(CollisionBodyPreset, stats);

        return new UnitDefinition
        {
            Id = Id,
            DisplayName = DisplayName,
            Cost = Cost,
            BaseStats = stats,
            BasicAttackDamageProfile = BasicAttackDamageProfile,
            AbilityDamageProfile = AbilityDamageProfile,
            AttackRangeTier = AttackRangeTier,
            Clades = Clades,
            InnateKeywordIds = InnateKeywordIds,
            EligibleMutationIds = EligibleMutationIds,
            MaxPoolCount = MaxPoolCount
        };
    }
}