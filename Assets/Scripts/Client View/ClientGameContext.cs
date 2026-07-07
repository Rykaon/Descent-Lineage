using UnityEngine;

public sealed class ClientGameContext : MonoBehaviour
{
    [Header("Databases")]
    [SerializeField] private UnitDefinitionDatabaseAsset unitDatabaseAsset;
    [SerializeField] private MutationDefinitionDatabaseAsset mutationDatabaseAsset;
    [SerializeField] private FaunaDefinitionDatabaseAsset faunaDatabaseAsset;
    [SerializeField] private CladeDefinitionDatabaseAsset cladeDatabaseAsset;

    public ClientGameMirror Mirror { get; private set; }
    public BattleClientState BattleState { get; private set; }
    public BattleEventsSnapshot LastBattleEvents { get; private set; }

    public IUnitDefinitionDatabase UnitDatabase { get; private set; }
    public IMutationDefinitionDatabase MutationDatabase { get; private set; }
    public IFaunaDefinitionDatabase FaunaDatabase { get; private set; }
    public ICladeDefinitionDatabase CladeDatabase { get; private set; }

    public event System.Action FossilChanged;
    public event System.Action BoardChanged;
    public event System.Action CladeChanged;
    public event System.Action ShopChanged;
    public event System.Action FaunaShopChanged;
    public event System.Action PlayerChanged;
    public event System.Action PhaseChanged;

    public event System.Action PreparationTicked;

    public event System.Action BattleInitChanged;
    public event System.Action BattleFrameChanged;
    public event System.Action<BattleEventsSnapshot> BattleEventsReceived;
    public event System.Action BattleEnded;

    public bool HasInitialState { get; private set; }

    public event System.Action InitialStateReady;

    private void Awake()
    {
        Mirror = new ClientGameMirror();

        UnitDatabase = unitDatabaseAsset.Build();
        MutationDatabase = mutationDatabaseAsset.Build();
        FaunaDatabase = faunaDatabaseAsset.Build();
        CladeDatabase = cladeDatabaseAsset.Build();
    }

    public void NotifyInitialStateReady()
    {
        HasInitialState = true;
        InitialStateReady?.Invoke();
    }

    public void NotifyFossilChanged() => FossilChanged?.Invoke();
    public void NotifyBoardChanged() => BoardChanged?.Invoke();
    public void NotifyCladeChanged() => CladeChanged?.Invoke();
    public void NotifyShopChanged() => ShopChanged?.Invoke();
    public void NotifyFaunaShopChanged() => FaunaShopChanged?.Invoke();
    public void NotifyPlayerChanged() => PlayerChanged?.Invoke();
    public void NotifyPhaseChanged() => PhaseChanged?.Invoke();

    public void NotifyPreparationTicked() => PreparationTicked?.Invoke();

    public void SetBattleState(BattleClientState state)
    {
        BattleState = state;
        BattleInitChanged?.Invoke();
    }

    public void NotifyBattleFrameChanged()
    {
        BattleFrameChanged?.Invoke();
    }

    public void NotifyBattleEventsReceived(BattleEventsSnapshot snapshot)
    {
        LastBattleEvents = snapshot;
        BattleEventsReceived?.Invoke(snapshot);
    }

    public void NotifyBattleEnded()
    {
        BattleEnded?.Invoke();
    }
}