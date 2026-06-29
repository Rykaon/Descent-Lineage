using System;
using UnityEngine;

public class FossilSystem
{
    public FossilTier[] Tiers =
    {
        new FossilTier { MinValue = 0, Level = 1, BoardCapacity = 1 },
        new FossilTier { MinValue = 2, Level = 2, BoardCapacity = 2 },
        new FossilTier { MinValue = 4, Level = 3, BoardCapacity = 3 },
        new FossilTier { MinValue = 10, Level = 4, BoardCapacity = 4 },
        new FossilTier { MinValue = 20, Level = 5, BoardCapacity = 5 },
        new FossilTier { MinValue = 36, Level = 6, BoardCapacity = 6 },
        new FossilTier { MinValue = 62, Level = 7, BoardCapacity = 7 },
        new FossilTier { MinValue = 100, Level = 8, BoardCapacity = 8 },
        new FossilTier { MinValue = 154, Level = 9, BoardCapacity = 9 },
    };

    public void Initialize()
    {
        
    }

    private int CalculateLevel(int fossilValue)
    {
        int level = 0;

        foreach (var tier in Tiers)
        {
            if (fossilValue >= tier.MinValue)
            {
                level = Math.Max(level, tier.Level);
            }
        }

        return level;
    }

    private int CalculateBoardCapacity(int fossilValue)
    {
        int boardCapacity = 0;

        foreach (var tier in Tiers)
        {
            if (fossilValue >= tier.MinValue)
            {
                boardCapacity = Math.Max(boardCapacity, tier.BoardCapacity);
            }
        }

        return boardCapacity;
    }

    public void ApplyLevel(PlayerState player)
    {
        player.Fossil.Level = CalculateLevel(player.Fossil.FossilValue);
        player.Board.BoardCapacity = CalculateBoardCapacity(player.Fossil.FossilValue);
    }
}

public struct FossilTier
{
    public int MinValue;
    public int Level;
    public int BoardCapacity;
}