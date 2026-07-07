using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CladeEntryView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private CanvasGroup canvasGroup;

    public void Refresh(ClientCladeProgressMirror mirror, ICladeDefinitionDatabase database)
    {
        CladeDefinition definition = database.GetClade(mirror.CladeId);

        nameText.text = definition != null ? definition.DisplayName : mirror.CladeId;
        countText.text = $"{mirror.Count}/{mirror.NextThreshold}";

        canvasGroup.alpha = mirror.IsActive ? 1f : 0.35f;
    }
}