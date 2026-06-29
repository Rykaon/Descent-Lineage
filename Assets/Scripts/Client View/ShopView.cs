using System;
using UnityEngine;

public class ShopView : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private ShopSlotView[] slotViews;

    private int localPlayerId = 0;

    private void OnEnable()
    {
        gameController.OnShopChanged += HandleShopChanged;
    }

    private void OnDisable()
    {
        gameController.OnShopChanged -= HandleShopChanged;
    }

    private void HandleShopChanged(PlayerState player)
    {
        if (player.PlayerId != localPlayerId)
            return;

        Refresh(player.Shop);
    }

    private void Refresh(ShopState shop)
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
}
