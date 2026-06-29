using System.IO.Hashing;
using UnityEngine;

public class PlayerState
{
    public int PlayerId;
    public int Life;
    public int AmberCount;
    public int BiomeCount;
    public StreakState Streak;

    public FossilState Fossil;
    public ShopState Shop;
    public FaunaShopState FaunaShop;
    public PlayerBoardState Board;

    public void Initialize()
    {
        AmberCount = 0;
        BiomeCount = 0;

        Fossil = new FossilState();
        Fossil.Initialize();

        Shop = new ShopState();
        Shop.Initalize();

        FaunaShop = new FaunaShopState();
        FaunaShop.Initalize();

        Board = new PlayerBoardState();
        Board.Initialize();
    }

    public void CalculateStreak(bool isWinner)
    {
        if (isWinner)
        {
            if (Streak.Value <= 0)
            {
                Streak.Value = 1;
            }
            else
            {
                Streak.Value++;
            }
        }
        else
        {
            if (Streak.Value >= 0)
            {
                Streak.Value = -1;
            }
            else
            {
                Streak.Value--;
            }
        }
    }

    public void AddAmber(int value)
    {
        AmberCount += value;
    }
}

public struct StreakState
{
    public int Value;
}
