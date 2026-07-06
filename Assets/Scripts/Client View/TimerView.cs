using UnityEngine;

public class TimerView : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI label;

    public void RefreshTimer(int remainingTime)
    {
        label.text = remainingTime.ToString();
    }
}
