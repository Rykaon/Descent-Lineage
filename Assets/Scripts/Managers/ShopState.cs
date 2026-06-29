using System.Collections.Generic;
using UnityEngine;

public class ShopState
{
    public ShopSlot[] Slots;
    public int RefreshCost;
    public bool CanRefreshShop;

    public void Initalize()
    {
        Slots = new ShopSlot[5];
        RefreshCost = 2;
        CanRefreshShop = true;
    }
}

public class ShopSlot
{
    public string UnitDefinitionId;
    public List<string> MutationsId = new();
    public int Cost;
    public bool IsBrick;
    public bool IsMutationUpgrade;

    public static ShopSlot CreateBrick()
    {
        return new ShopSlot
        {
            IsBrick = true,
            Cost = 0
        };
    }

    public static ShopSlot CreateBrickReservedCreature(string unitDefinitionId)
    {
        return new ShopSlot
        {
            UnitDefinitionId = unitDefinitionId,
            IsBrick = true,
            Cost = 0
        };
    }

    public static ShopSlot CreateNewCreature(string unitDefinitionId, int cost)
    {
        return new ShopSlot
        {
            UnitDefinitionId = unitDefinitionId,
            Cost = cost,
            IsMutationUpgrade = false
        };
    }

    public static ShopSlot CreateMutationUpgrade(string unitDefinitionId, List<string> mutationsId, int cost)
    {
        return new ShopSlot
        {
            UnitDefinitionId = unitDefinitionId,
            MutationsId = mutationsId,
            Cost = cost,
            IsMutationUpgrade = true
        };
    }
}
