using UnityEngine;

public class FaunaShopView : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private FaunaShopSlotView[] slotViews;

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

        Refresh(player.FaunaShop);
    }

    private void Refresh(FaunaShopState shop)
    {
        for (int i = 0; i < slotViews.Length; i++)
        {
            int slotIndex = i;
            FaunaShopSlot slot = shop.Slots[i];

            slotViews[i].Bind(slot, () =>
            {
                inputController.BuyFaunaShopSlot(slotIndex);
            });
        }
    }
}