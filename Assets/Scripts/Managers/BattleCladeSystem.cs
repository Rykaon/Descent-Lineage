using System.Collections.Generic;
using UnityEngine;

public class BattleCladeSystem
{
    private BattleSystem BattleSystem;

    private readonly Dictionary<int, PlayerCladeState> statesByPlayer = new();

    public void Initialize(BattleSystem battleSystem)
    {
        BattleSystem = battleSystem;
    }

    public void Tick(float deltaTime)
    {
        foreach (BattleUnitInstance unit in BattleSystem.BattleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            foreach (ActiveCladeEffect effect in GetActiveEffects(unit.OwnerPlayerId))
            {
                BattleCladeEffectRuntime runtime = BattleCladeEffectFactory.Create(effect.EffectId);
                runtime.Initialize(BattleSystem);
                runtime.Tick(unit, deltaTime, effect);
            }
        }
    }

    public void BuildClades()
    {
        statesByPlayer.Clear();

        foreach (var unit in BattleSystem.BattleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            if (!statesByPlayer.TryGetValue(unit.OwnerPlayerId, out var state))
            {
                state = new PlayerCladeState { PlayerId = unit.OwnerPlayerId };
                statesByPlayer.Add(unit.OwnerPlayerId, state);
            }

            var definition = BattleSystem.unitDatabase.GetUnit(unit.DefinitionId);

            foreach (string clade in definition.Clades)
            {
                if (!state.CountsByClade.ContainsKey(clade))
                {
                    state.CountsByClade[clade] = 0;
                }

                state.CountsByClade[clade]++;
            }
        }

        ResolveActiveEffects();
    }

    private void ResolveActiveEffects()
    {
        foreach (PlayerCladeState state in statesByPlayer.Values)
        {
            foreach (var pair in state.CountsByClade)
            {
                string cladeId = pair.Key;
                int count = pair.Value;

                CladeDefinition definition = BattleSystem.cladeDatabase.GetClade(cladeId);

                if (definition == null || definition.Tiers == null)
                {
                    continue;
                }

                foreach (CladeTierDefinition tier in definition.Tiers)
                {
                    if (count < tier.RequiredCount || tier.Effects == null)
                    {
                        continue;
                    }

                    foreach (CladeEffectDefinition effect in tier.Effects)
                    {
                        state.ActiveEffects.Add(new ActiveCladeEffect
                        {
                            CladeId = cladeId,
                            RequiredCount = tier.RequiredCount,
                            EffectId = effect.EffectId,
                            Value = effect.Value,
                            Interval = effect.Interval,
                            ScalingId = effect.ScalingId
                        });
                    }
                }
            }
        }
    }

    public IReadOnlyList<ActiveCladeEffect> GetActiveEffects(int playerId)
    {
        if (!statesByPlayer.TryGetValue(playerId, out var state))
        {
            return System.Array.Empty<ActiveCladeEffect>();
        }

        return state.ActiveEffects;
    }

    public void OnUnitDeath(BattleUnitInstance deadUnit)
    {
        foreach (ActiveCladeEffect effect in GetActiveEffects(deadUnit.OwnerPlayerId))
        {
            var runtime = BattleCladeEffectFactory.Create(effect.EffectId);
            runtime.Initialize(BattleSystem);
            runtime.OnUnitDeath(deadUnit, effect);
        }
    }

    public void OnAbilityCast(BattleUnitInstance caster)
    {
        foreach (ActiveCladeEffect effect in GetActiveEffects(caster.OwnerPlayerId))
        {
            var runtime = BattleCladeEffectFactory.Create(effect.EffectId);
            runtime.Initialize(BattleSystem);
            runtime.OnAbilityCast(caster, effect);
        }
    }

    public void Clear()
    {
        statesByPlayer.Clear();
    }
}

public static class BattleCladeEffectFactory
{
    public static BattleCladeEffectRuntime Create(string effectId)
    {
        return effectId switch
        {
            "AttackSpeedFlat" => new AttackSpeedFlatCladeEffectRuntime(),
            "SlashOffenseFlat" => new SlashOffenseFlatCladeEffectRuntime(),
            "SlashDefenseFlat" => new SlashDefenseFlatCladeEffectRuntime(),
            "ImpactOffenseFlat" => new ImpactOffenseFlatCladeEffectRuntime(),
            "ImpactDefenseFlat" => new ImpactDefenseFlatCladeEffectRuntime(),
            "HealthRegen" => new HealthRegenCladeEffectRuntime(),
            "SharedGrief" => new SharedGriefCladeEffectRuntime(),
            "ManaLink" => new ManaLinkCladeEffectRuntime(),

            _ => new NoCladeEffectRuntime()
        };
    }
}

public sealed class PlayerCladeState
{
    public int PlayerId;
    public Dictionary<string, int> CountsByClade = new();
    public List<ActiveCladeEffect> ActiveEffects = new();
}

public sealed class ActiveCladeEffect
{
    public string CladeId;
    public int RequiredCount;
    public string EffectId;
    public float Value;
    public float Interval;
    public string ScalingId;
}