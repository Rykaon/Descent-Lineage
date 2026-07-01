using UnityEngine;

public class FaunaShopView : MonoBehaviour
{
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private FaunaShopSlotView[] slotViews;

    public void Refresh(ClientFaunaShopMirror shop)
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