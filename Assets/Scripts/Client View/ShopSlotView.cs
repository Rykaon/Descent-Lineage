using System;
using UnityEngine;

public class ShopSlotView : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI label;
    [SerializeField] private UnityEngine.UI.Button button;

    public void Bind(ShopSlot slot, Action onClick)
    {
        button.onClick.RemoveAllListeners();

        if (slot == null)
        {
            label.text = "-";
            button.interactable = false;
            return;
        }

        if (slot.IsBrick)
        {
            label.text = "Bricked";
            button.interactable = false;
            return;
        }

        label.text = slot.IsMutationUpgrade
            ? $"{slot.UnitDefinitionId}\n{slot.MutationsId[slot.MutationsId.Count - 1]}\nCost: {slot.Cost}"
            : $"{slot.UnitDefinitionId}\nCost: {slot.Cost}";

        button.interactable = true;
        button.onClick.AddListener(() => onClick.Invoke());
    }
}
