using UnityEngine;
using UnityEngine.UI;

public class HealthBarView : MonoBehaviour
{
    [SerializeField] private Image immediateFill;
    [SerializeField] private Image delayedFill;

    [SerializeField] private float delayedSpeed = 2f;
    private float targetValue;

    private HealthBarPool pool;
    private Transform anchor;

    public void Initialize(HealthBarPool pool)
    {
        this.pool = pool;
    }

    public void Bind(Transform anchor, int current, int max)
    {
        this.anchor = anchor;

        SetValue(current, max);

        delayedFill.fillAmount = targetValue;
        immediateFill.fillAmount = targetValue;
    }

    public void SetValue(int current, int max)
    {
        targetValue = max <= 0 ? 0f : (float)current / max;

        immediateFill.fillAmount = targetValue;
    }

    private void Update()
    {
        if (anchor == null)
        {
            return;
        }

        transform.position = anchor.position;

        delayedFill.fillAmount = Mathf.MoveTowards(delayedFill.fillAmount, targetValue, delayedSpeed * Time.deltaTime);
    }

    public void Release()
    {
        pool.Release(this);
    }
}