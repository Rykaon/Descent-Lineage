using System.Collections.Generic;
using UnityEngine;

public class BattleView : MonoBehaviour
{
    [SerializeField] private BoardView boardView;
    [SerializeField] private DamagePopUpPool damagePopUpPool;
    [SerializeField] private HealthBarPool healthBarPool;
    private ClientGameMirror mirror;

    private readonly Dictionary<string, UnitView> activeBattleViewsByBoardId = new();

    private BattleClientState replicationState;
    public bool HasClientBattleState => replicationState != null;

    public void Initialize(ClientGameMirror mirror)
    {
        this.mirror = mirror;
    }

    public void Bind(BattleClientState state)
    {
        replicationState = state;
        activeBattleViewsByBoardId.Clear();

        foreach (var unit in replicationState.Units)
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

    public void RefreshBattleFrame()
    {
        if (replicationState == null)
        {
            return;
        }

        foreach (BattleClientUnit unit in replicationState.Units)
        {
            if (!activeBattleViewsByBoardId.TryGetValue(unit.BoardInstanceId, out UnitView view))
            {
                continue;
            }

            view.RefreshHealthBar(unit);
            view.RefreshManaBar(unit);
        }
    }

    public void ApplyBattleEvents(BattleEventsSnapshot snapshot)
    {
        if (replicationState == null)
        {
            return;
        }

        foreach (BattleDamageEventSnapshot damageEvent in snapshot.DamageEvents)
        {
            string targetId = damageEvent.TargetBattleInstanceId.ToString();

            BattleClientUnit target = replicationState.GetUnitByBattleId(targetId);

            if (target == null)
            {
                continue;
            }

            if (!activeBattleViewsByBoardId.TryGetValue(target.BoardInstanceId, out UnitView view))
            {
                continue;
            }

            Color color = GetDamageColor(damageEvent.Delivery);

            view.ShowDamagePopUp(damageEvent.Amount, color);
            view.RefreshHealthBar(target);
        }

        foreach (BattleManaEventSnapshot manaEvent in snapshot.ManaEvents)
        {
            BattleClientUnit target = replicationState.GetUnitByBattleId(manaEvent.TargetBattleInstanceId.ToString());

            if (target == null)
                continue;

            target.CurrentMana = manaEvent.CurrentMana;
            target.MaxMana = manaEvent.MaxMana;

            if (activeBattleViewsByBoardId.TryGetValue(target.BoardInstanceId, out UnitView view))
            {
                view.RefreshManaBar(target);
            }
        }

        foreach (BattleHealEventSnapshot healEvent in snapshot.HealEvents)
        {
            BattleClientUnit target = replicationState.GetUnitByBattleId(healEvent.TargetBattleInstanceId.ToString());

            if (target == null)
                continue;

            target.CurrentHealth = healEvent.CurrentHealth;
            target.MaxHealth = healEvent.MaxHealth;

            if (activeBattleViewsByBoardId.TryGetValue(target.BoardInstanceId, out UnitView view))
            {
                view.ShowDamagePopUp(healEvent.Amount, Color.greenYellow);
                view.RefreshHealthBar(target);
            }
        }
    }

    private Color GetDamageColor(DamageDelivery delivery)
    {
        return delivery switch
        {
            DamageDelivery.Ability => Color.cyan,
            DamageDelivery.DirectAttack => Color.red,
            DamageDelivery.DamageOverTime => Color.darkOliveGreen,
            DamageDelivery.TrueDamage => Color.peachPuff,
            _ => Color.red
        };
    }

    public void HandleBattleEnded() 
    {
        foreach (var view in activeBattleViewsByBoardId.Values)
        {
            view.gameObject.SetActive(true);
            view.ResetToBoardPosition();
            view.ReleaseHealthBar();
        }

        replicationState = null;
        activeBattleViewsByBoardId.Clear();
    }

    private void Update()
    {
        if (replicationState == null)
        {
            return;
        }

        if (mirror.Phase != GamePhase.Battle)
        {
            return;
        }

        foreach (var unit in replicationState.Units)
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

            Vector3 position = new(unit.Position.x, 0f, unit.Position.y);

            Vector3 smoothedPosition = Vector3.Lerp(view.transform.position, position, 1f - Mathf.Exp(-25f * Time.deltaTime));

            view.SetBattlePosition(position, unit.MoveSpeed);

            Vector3 viewPos = view.transform.position;

            BattleClientUnit target = null;

            if (!string.IsNullOrEmpty(unit.CurrentTargetBattleInstanceId))
            {
                target = replicationState.GetUnitByBattleId(unit.CurrentTargetBattleInstanceId);
            }

            if (target != null && !target.IsDead)
            {
                view.LookDirection(target.Position - unit.Position);
            }
            else
            {
                Vector2 moveDirection = unit.Position - unit.LastPosition;
                view.LookDirection(moveDirection);
            }
        }
    }
}
