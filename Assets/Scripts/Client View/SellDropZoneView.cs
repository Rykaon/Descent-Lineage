using UnityEngine;
using UnityEngine.UI;

public class SellDropZoneView : MonoBehaviour
{
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Image background;
    [SerializeField] private TMPro.TextMeshProUGUI label;
    [SerializeField] private Color normalColor = new Color(1f, 0f, 0f, 0.35f);
    [SerializeField] private Color highlightedColor = new Color(1f, 0.5f, 0f, 0.55f);

    public void Show(int cost)
    {
        label.text = "Vendre pour " + cost.ToString() + " ambres ?";
        gameObject.SetActive(true);
        SetHighlighted(false);
    }

    public void Hide()
    {
        SetHighlighted(false);
        gameObject.SetActive(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (background != null)
            background.color = highlighted ? highlightedColor : normalColor;

        if (visualRoot != null)
            visualRoot.SetActive(true);
    }
}