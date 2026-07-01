using TMPro;
using UnityEngine;
using System.Collections;

public class DamagePopUpView : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    private DamagePopUpPool pool;
    private Coroutine routine;

    public void Initialize(DamagePopUpPool pool)
    {
        this.pool = pool;
    }

    public void Play(int damage, Color color)
    {
        text.text = damage.ToString();
        text.color = color;

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(PopupRoutine());
    }

    private IEnumerator PopupRoutine()
    {
        float duration = 0.5f;
        float elapsed = 0f;

        canvasGroup.alpha = 1f;

        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(start, end, t);
            canvasGroup.alpha = 1f - t;

            yield return null;
        }

        routine = null;
        pool.Release(this);
    }
}
