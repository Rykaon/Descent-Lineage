using UnityEngine;
using UnityEngine.UI;

public class HealthBarView : MonoBehaviour
{
    [SerializeField] private Image healthImmediateFill;
    [SerializeField] private Image healthDelayedFill;
    [SerializeField] private Image manaImmediateFill;
    [SerializeField] private Image manaDelayedFill;

    [SerializeField] private float healthDelayedSpeed = 2f;
    [SerializeField] private float manaDelayedSpeed = 2f;
    private float healthTargetValue;
    private float manaTargetValue;

    private HealthBarPool pool;
    private Transform anchor;

    public void Initialize(HealthBarPool pool)
    {
        this.pool = pool;
    }

    public void Bind(Transform anchor, int currentHealth, int maxHealth, int currentMana, int maxMana)
    {
        this.anchor = anchor;

        SetHealthValue(currentHealth, maxHealth);
        SetManaValue(currentMana, maxMana);

        healthDelayedFill.fillAmount = healthTargetValue;
        healthImmediateFill.fillAmount = healthTargetValue;

        manaDelayedFill.fillAmount = manaTargetValue;
        manaImmediateFill.fillAmount = manaTargetValue;
    }

    public void SetHealthValue(int current, int max)
    {
        healthTargetValue = max <= 0 ? 0f : (float)current / max;

        healthImmediateFill.fillAmount = healthTargetValue;
    }

    public void SetManaValue(int current, int max)
    {
        manaTargetValue = max <= 0 ? 0f : (float)current / max;

        manaImmediateFill.fillAmount = manaTargetValue;
    }

    private void Update()
    {
        if (anchor == null)
        {
            return;
        }

        transform.position = anchor.position;

        healthDelayedFill.fillAmount = Mathf.MoveTowards(healthDelayedFill.fillAmount, healthTargetValue, healthDelayedSpeed * Time.deltaTime);
        manaDelayedFill.fillAmount = Mathf.MoveTowards(manaDelayedFill.fillAmount, manaTargetValue, manaDelayedSpeed * Time.deltaTime);
    }

    public void Release()
    {
        pool.Release(this);
    }
}