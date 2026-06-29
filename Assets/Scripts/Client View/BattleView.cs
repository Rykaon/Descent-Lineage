using System.Collections.Generic;
using UnityEngine;

public class BattleView : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private BoardView boardView;
    [SerializeField] private DamagePopUpPool damagePopUpPool;
    [SerializeField] private HealthBarPool healthBarPool;

    private readonly Dictionary<string, UnitView> activeBattleViewsByBoardId = new();

    private BattleState battleState;

    private void Start()
    {
        gameController.OnBattleStarted += HandleBattleStarted;
        gameController.OnBattleEnded += HandleBattleEnded;
        gameController.Battle.OnDamageApplied += HandleDamageApplied;
    }

    private void HandleBattleStarted(BattleState state)
    {
        battleState = state;
        activeBattleViewsByBoardId.Clear();

        foreach (var unit in battleState.Units)
        {
            UnitView view = boardView.GetUnitView(unit.BoardInstanceId);

            if (view == null)
            {
                continue;
            }

            view.damagePopUpPool = damagePopUpPool;
            view.healthBarPool = healthBarPool;
            view.InstantiateHealthBar(unit);

            activeBattleViewsByBoardId.Add(unit.BoardInstanceId, view);
        }
    }

    private void HandleDamageApplied(BattleUnitInstance unit, int damage)
    {
        if (!activeBattleViewsByBoardId.TryGetValue(unit.BoardInstanceId, out UnitView view))
        {
            return;
        }

        view.ShowDamagePopUp(damage);
        view.RefreshHealthBar(unit);
    }

    private void HandleBattleEnded() 
    {
        foreach (var view in activeBattleViewsByBoardId.Values)
        {
            view.gameObject.SetActive(true);
            view.ResetToBoardPosition();
            view.ReleaseHealthBar();
        }

        battleState = null;
        activeBattleViewsByBoardId.Clear();
    }

    private void Update()
    {
        if (battleState == null)
        {
            return;
        }

        if (gameController.State.Phase != GamePhase.Battle)
        {
            return;
        }

        foreach (var unit in battleState.Units)
        {
            if (!activeBattleViewsByBoardId.TryGetValue(unit.BoardInstanceId, out UnitView view))
            {
                continue;
            }

            if (unit.IsDead)
            {
                view.ReleaseHealthBar();
                view.gameObject.SetActive(false);
                continue;
            }

            view.gameObject.SetActive(true);

            //DrawDebug(unit);

            Vector3 position = new(unit.Position.x, 0f, unit.Position.y);

            Vector3 smoothedPosition = Vector3.Lerp(view.transform.position, position, 1f - Mathf.Exp(-25f * Time.deltaTime));

            view.SetBattlePosition(position, unit.CurrentStats.MoveSpeed);

            Vector3 viewPos = view.transform.position;

            /*Debug.DrawLine(
                new Vector3(unit.Position.x, 0.2f, unit.Position.y),
                viewPos + Vector3.up * 0.2f,
                Color.magenta,
                0f);

            Debug.DrawRay(
                new Vector3(unit.Position.x, 0.2f, unit.Position.y),
                Vector3.up * 0.5f,
                Color.blue,
                0f);

            Debug.DrawRay(
                viewPos + Vector3.up * 0.2f,
                Vector3.up * 0.5f,
                Color.yellow,
                0f);*/

            if (unit.HexPath != null && unit.HexPathIndex < unit.HexPath.Count)
            {
                Vector2 next = gameController.Battle.HexGrid.HexToWorld(unit.HexPath[unit.HexPathIndex]);

                view.LookDirection(next - unit.Position);
            }

            BattleUnitInstance target = null;

            if (!string.IsNullOrEmpty(unit.CurrentTargetBattleInstanceId))
            {
                target = battleState.Units.Find(u =>
                    u.BattleInstanceId == unit.CurrentTargetBattleInstanceId);
            }

            if (target != null && !target.IsDead)
            {
                view.LookDirection(target.Position - unit.Position);
            }
            else if (unit.Path.HasPath)
            {
                view.LookDirection(unit.Path.CurrentWaypoint - unit.Position);
            }

            //DrawAttackRange(unit);
            DrawNavigationState(unit);
        }
    }

    private void DrawAttackRange(BattleUnitInstance unit)
    {
        var target = battleState.GetUnitByBattleId(unit.CurrentTargetBattleInstanceId);

        if (target == null || target.IsDead)
            return;

        Color color = BattleRangeUtility.CanAttack(unit, target, gameController.Battle)
            ? Color.red
            : Color.yellow;

        Vector3 from = new Vector3(unit.Position.x, 0.35f, unit.Position.y);
        Vector3 to = new Vector3(target.Position.x, 0.35f, target.Position.y);

        Debug.DrawLine(from, to, color, 0f);
    }

    private void DrawNavigationState(BattleUnitInstance unit)
    {
        if (unit.IsDead)
            return;

        Color color;

        if (unit.IsEngaged)
            color = Color.red;
        else if (string.IsNullOrEmpty(unit.CurrentTargetBattleInstanceId))
            color = Color.gray;
        else if (unit.Decision == BattleUnitDecision.WaitingForPath)
            color = Color.black;
        else if (unit.HexPath == null || unit.HexPathIndex >= unit.HexPath.Count)
            color = Color.blue;
        else
            color = Color.green;

        Vector3 p = new Vector3(unit.Position.x, 1.4f, unit.Position.y);
        Debug.DrawRay(p, Vector3.up * 0.6f, color, 0f);
    }
}
