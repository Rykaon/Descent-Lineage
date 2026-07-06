using System;
using UnityEngine;

public class ShopView : MonoBehaviour
{
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private ShopSlotView[] slotViews;
    [SerializeField] private ResourceCounterView resourceCounter;

    public void Refresh(ClientShopMirror shop)
    {
        for (int i = 0; i < slotViews.Length; i++)
        {
            int slotIndex = i;
            ShopSlot slot = shop.Slots[i];

            slotViews[i].Bind(slot, () =>
            {
                inputController.BuyShopSlot(slotIndex);
            });
        }
    }

    public void RefreshResourcesCount(int AmberCount, int BiomeCount)
    {
        resourceCounter.SetResources(AmberCount, BiomeCount);
    }
}
