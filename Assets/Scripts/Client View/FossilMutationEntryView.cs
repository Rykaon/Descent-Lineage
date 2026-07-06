using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class FossilMutationEntryView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text biomeText;
    [SerializeField] private TMP_Text rankText;

    private ClientFossilMutationMirror mutation;
    private FossilRegistryPopUpView popup;

    public void Initialize(ClientFossilMutationMirror mutation, FossilRegistryPopUpView popup)
    {
        this.mutation = mutation;
        this.popup = popup;

        nameText.text = mutation.DisplayName;
        biomeText.text = $"Biome :\n{mutation.Biome}";
        rankText.text = $"Rang {mutation.Rank}";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        popup.ShowEligibleUnits(mutation.MutationId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        popup.HideEligibleUnits();
    }
}