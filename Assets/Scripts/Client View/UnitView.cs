using UnityEngine;

public class UnitView : MonoBehaviour
{
    private bool IsDragged;
    public string UnitInstanceId { get; private set; }
    public Vector3 Position { get; private set; }
    public Vector3 BattlePosition { get; private set; }

    private Vector3 moveFrom;
    private Vector3 moveTo;
    private float moveElapsed;
    private float moveDuration;
    private bool isBattleInterpolating;

    [SerializeField] private float battlePositionLerpSpeed = 20f;
    private Vector3 visualTargetPosition;
    private bool hasVisualTargetPosition;

    [SerializeField] private Transform healthBarAnchor;
    [SerializeField] private Transform popUpAnchor;

    public HealthBarPool healthBarPool;
    public DamagePopUpPool damagePopUpPool;
    private HealthBarView healthBar;

    public void Bind(string unitInstanceId)
    {
        UnitInstanceId = unitInstanceId;
    }

    public void SetPosition(Vector3 position)
    {
        Position = position;

        if (!hasVisualTargetPosition)
        {
            transform.position = Position;
        }
    }

    public void SetBattlePosition(Vector3 simulationPosition, float moveSpeed)
    {
        Debug.Log($"[UNIT VIEW SET BATTLE POS] unit={UnitInstanceId} target={simulationPosition} current={transform.position}");
        BattlePosition = simulationPosition;

        if (hasVisualTargetPosition && Vector3.Distance(moveTo, simulationPosition) < 0.001f)
        {
            return;
        }

        moveFrom = transform.position;
        moveTo = simulationPosition;

        moveElapsed = 0f;
        moveDuration = 1f / Mathf.Max(moveSpeed, 0.01f);

        isBattleInterpolating = true;
        hasVisualTargetPosition = true;
    }

    public void ResetToBoardPosition()
    {
        hasVisualTargetPosition = false;
        visualTargetPosition = Position;
        BattlePosition = Position;

        transform.position = Position;
        transform.rotation = Quaternion.identity;
    }

    public void SetDragging(bool isDragging, bool resetPosition)
    {
        IsDragged = isDragging;

        if (resetPosition)
        {
            gameObject.transform.position = Position;
        }
    }

    public void LookDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Vector3 forward = new(direction.x, 0f, direction.y);
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    public void InstantiateHealthBar(BattleClientUnit unit)
    {
        healthBar = healthBarPool.Get(healthBarAnchor.position);
        healthBar.Bind(healthBarAnchor, unit.CurrentHealth, unit.MaxHealth);
    }

    public void RefreshHealthBar(BattleClientUnit unit)
    {
        healthBar.SetValue(unit.CurrentHealth, unit.MaxHealth);
    }

    public void ReleaseHealthBar()
    {
        if (healthBar == null)
        {
            return;
        }

        healthBar.Release();
        healthBar = null;
    }

    public void ShowDamagePopUp(int damage, Color color)
    {
        DamagePopUpView popup = damagePopUpPool.Get(popUpAnchor.position);

        popup.Play(damage, color);
    }
    private void LateUpdate()
    {
        if (!hasVisualTargetPosition || IsDragged)
            return;

        if (!isBattleInterpolating)
            return;

        moveElapsed += Time.deltaTime;

        float t = Mathf.Clamp01(moveElapsed / moveDuration);

        transform.position = Vector3.LerpUnclamped(moveFrom, moveTo, t);

        if (t >= 1f)
        {
            transform.position = moveTo;
            isBattleInterpolating = false;
        }
    }
}
