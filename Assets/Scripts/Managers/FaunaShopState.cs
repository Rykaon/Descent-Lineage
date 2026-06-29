using System.Collections.Generic;
using UnityEngine;

public class FaunaShopState
{
    public FaunaShopSlot[] Slots;
    public int RefreshCost;
    public bool CanRefreshShop;

    public void Initalize()
    {
        Slots = new FaunaShopSlot[3];
    }
}

public class FaunaShopSlot
{
    public string FaunaDefinitionId;
    public string MutationId;
    public int FossilValue;
    public int Cost;

    public bool IsBrick;

    public static FaunaShopSlot CreateBrick()
    {
        return new FaunaShopSlot
        {
            FaunaDefinitionId = null,
            MutationId = null,
            FossilValue = 0,
            Cost = 0,

            IsBrick = true,
        };
    }

    public static FaunaShopSlot CreateShopSlot(string faunaDefinitionId, string mutationId, int fossilValue, int cost)
    {
        return new FaunaShopSlot
        {
            FaunaDefinitionId = faunaDefinitionId,
            MutationId = mutationId,
            FossilValue = fossilValue,
            Cost = cost,

            IsBrick = false,
        };
    }
}
