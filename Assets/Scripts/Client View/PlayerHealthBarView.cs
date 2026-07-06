using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarView : MonoBehaviour
{
    [SerializeField] private Image immediateFill;
    [SerializeField] private Image delayedFill;

    [SerializeField] private float delayedSpeed = 2f;
    private float targetValue;
    private bool isInitialized = false;

    public void Bind(int current, int max)
    {
        SetValue(current, max);

        delayedFill.fillAmount = targetValue;
        immediateFill.fillAmount = targetValue;

        isInitialized = true;
    }

    public void SetValue(int current, int max)
    {
        targetValue = max <= 0 ? 0f : (float)current / max;

        immediateFill.fillAmount = targetValue;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        delayedFill.fillAmount = Mathf.MoveTowards(delayedFill.fillAmount, targetValue, delayedSpeed * Time.deltaTime);
    }
}
