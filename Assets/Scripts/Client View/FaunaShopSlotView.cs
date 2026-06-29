using System;
using UnityEngine;

public class FaunaShopSlotView : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI label;
    [SerializeField] private UnityEngine.UI.Button button;

    public void Bind(FaunaShopSlot slot, Action onClick)
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

        label.text = $"{slot.FaunaDefinitionId}\n{slot.MutationId}\nFossilValue: {slot.FossilValue}\nCost: {slot.Cost}";

        button.interactable = true;
        button.onClick.AddListener(() => onClick.Invoke());
    }
}
