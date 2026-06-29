using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EconomySystem
{
    private EconomySettings settings;
    public EconomySettings Settings => settings;

    public void Initialize()
    {
        settings = new EconomySettings();
    }

    private int CalculateAmberIncome(PlayerState player, SharedBoardState board)
    {
        return settings.BaseAmberIncome + GetStreakBonus(player.Streak.Value, settings.AmberStreakTiers) + GetAmberInterest(player, board);
    }

    private int GetAmberInterest(PlayerState player, SharedBoardState board)
    {
        List<BiomeType> biomes = new List<BiomeType>();

        foreach (BoardTileState tile in board.GetTilesForPlayerOrdered(player, BoardType.Board))
        {
            if (tile.Biome == BiomeType.None)
            {
                continue;
            }

            if (!biomes.Contains(tile.Biome))
            {
                biomes.Add(tile.Biome);
            }
        }

        return biomes.Count;
    }

    private int CalculateBiomeIncome(PlayerState player)
    {
        return settings.BaseBiomeIncome + GetStreakBonus(player.Streak.Value, settings.BiomeStreakTiers);
    }

    private int GetStreakBonus(int streakValue, StreakIncomeTier[] tiers)
    {
        int absStreak = Math.Abs(streakValue);
        int bonus = 0;

        foreach (var tier in tiers)
        {
            if (absStreak >= tier.MinStreak)
            {
                bonus = Math.Max(bonus, tier.Bonus);
            }
        }

        return bonus;
    }

    public void ApplyInitIncome(PlayerState player)
    {
        player.AmberCount += settings.InitAmberIncome;
        player.BiomeCount += settings.InitBiomeIncome;
    }

    public void ApplyIncome(PlayerState player, SharedBoardState board)
    {
        player.AmberCount += CalculateAmberIncome(player, board);
        player.BiomeCount += CalculateBiomeIncome(player);
    }

    public int GetUnitSellCost(BoardUnitInstance unit)
    {
        int cost = 0;

        switch (unit.MutationIds.Count)
        {
            case 0:
                cost = 1;
                break;

            case 1:
                cost = 3;
                break;

            case 2:
                cost = 6;
                break;

            case 3:
                cost = 9;
                break;

            default:
                cost = 0;
                break;
        }

        return cost;
    }
}

public class EconomySettings
{
    public int InitAmberIncome = 30;
    public int InitBiomeIncome = 30;

    public int BaseAmberIncome = 5;
    public int BaseBiomeIncome = 1;

    public int BiomeToAmberConversion = 3;

    public StreakIncomeTier[] AmberStreakTiers =
    {
        new StreakIncomeTier { MinStreak = 2, Bonus = 1 },
        new StreakIncomeTier { MinStreak = 4, Bonus = 2 },
        new StreakIncomeTier { MinStreak = 6, Bonus = 3 },
    };

    public StreakIncomeTier[] BiomeStreakTiers =
    {
        new StreakIncomeTier { MinStreak = 4, Bonus = 1 },
        //new StreakIncomeTier { MinStreak = 4, Bonus = 2 },
    };
}

public struct StreakIncomeTier
{
    public int MinStreak;
    public int Bonus;
}