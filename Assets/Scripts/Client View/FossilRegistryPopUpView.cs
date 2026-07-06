using TMPro;
using UnityEngine.UI;
using UnityEngine;

public sealed class FossilRegistryPopUpView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text xpText;

    [Header("Mutation List")]
    [SerializeField] private Transform mutationContainer;
    [SerializeField] private FossilMutationEntryView mutationEntryPrefab;

    [Header("Hover Panel")]
    [SerializeField] private FossilEligibleUnitsPanelView eligibleUnitsPanel;

    private ClientGameMirror mirror;
    private IUnitDefinitionDatabase unitDatabase;

    private int currentPlayerId;
    private FossilRegistryViewMode currentMode;

    public bool IsOpen => root != null && root.activeSelf;


    public void Initialize(ClientGameMirror mirror, IUnitDefinitionDatabase unitDatabase)
    {
        this.mirror = mirror;
        this.unitDatabase = unitDatabase;

        closeButton.onClick.RemoveListener(Close);
        closeButton.onClick.AddListener(Close);

        Close();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }

    public void Open(int playerId, FossilRegistryViewMode mode)
    {
        currentPlayerId = playerId;
        currentMode = mode;

        root.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (root != null)
        {
            root.SetActive(false);
        }

        if (eligibleUnitsPanel != null)
        {
            eligibleUnitsPanel.ClearDisplay();
        }
    }

    public void Refresh()
    {
        if (mirror == null)
        {
            return;
        }

        ClientFossilMirror fossil = mirror.Players[currentPlayerId].Fossil;

        titleText.text = currentMode == FossilRegistryViewMode.Local ? "Registre Fossile Local" : "Registre fossile adverse";

        levelText.text = $"Niveau fossile : {fossil.FossilLevel}";
        xpText.text = $"XP fossile : {fossil.CurrentXp} / {fossil.NextLevelXp}";

        ClearMutationEntries();

        foreach (var mutation in fossil.Mutations)
        {
            var entry = Instantiate(mutationEntryPrefab, mutationContainer);
            entry.Initialize(mutation, this);
        }

        eligibleUnitsPanel.ClearDisplay();
    }

    public void RefreshIfOpen()
    {
        if (IsOpen)
        {
            Refresh();
        }
    }

    public void ShowEligibleUnits(string mutationId)
    {
        eligibleUnitsPanel.Show(mutationId, unitDatabase);
    }

    public void HideEligibleUnits()
    {
        eligibleUnitsPanel.ClearDisplay();
    }

    private void ClearMutationEntries()
    {
        for (int i = mutationContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(mutationContainer.GetChild(i).gameObject);
        }
    }
}