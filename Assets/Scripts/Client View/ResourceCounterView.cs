using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceCounterView : MonoBehaviour
{


    [Header("Resource 1")]
    [SerializeField] private Image AmberIcon;
    [SerializeField] private TMP_Text AmberText;

    [Header("Resource 2")]
    [SerializeField] private Image BiomeIcon;
    [SerializeField] private TMP_Text BiomeText;

    [Header("Optional Styling")]
    [SerializeField] private Image Background;
    [SerializeField] private Image Border;

    public void SetAmberCount(int value)
    {
        AmberText.text = value.ToString();
    }

    public void SetBiomeCount(int value)
    {
        BiomeText.text = value.ToString();
    }

    public void SetResources(int AmberCount, int BiomeCount)
    {
        SetAmberCount(AmberCount);
        SetBiomeCount(BiomeCount);
    }
}
