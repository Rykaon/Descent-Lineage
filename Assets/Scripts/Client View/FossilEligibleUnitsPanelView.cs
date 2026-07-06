using System.Linq;
using TMPro;
using UnityEngine;

public sealed class FossilEligibleUnitsPanelView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform unitContainer;
    [SerializeField] private TMP_Text unitTextPrefab;

    public void Show(string mutationId, IUnitDefinitionDatabase unitDatabase)
    {
        root.SetActive(true);
        titleText.text = "Créatures compatibles";

        Clear();

        int count = 0;

        foreach (var unit in unitDatabase.GetAllUnits())
        {
            if (unit.EligibleMutationIds == null)
            {
                continue;
            }

            if (!unit.EligibleMutationIds.Contains(mutationId))
            {
                continue;
            }

            TMP_Text line = Instantiate(unitTextPrefab, unitContainer);
            line.text = unit.DisplayName;

            count++;
        }
    }

    public void ClearDisplay()
    {
        Clear();

        if (titleText != null)
        {
            titleText.text = "Survole une mutation";
        }
    }

    private void Clear()
    {
        for (int i = unitContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(unitContainer.GetChild(i).gameObject);
        }
    }
}