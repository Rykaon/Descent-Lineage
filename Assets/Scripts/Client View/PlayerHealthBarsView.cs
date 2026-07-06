using UnityEngine;

public class PlayerHealthBarsView : MonoBehaviour
{
    [SerializeField] private PlayerHealthBarView localHealthBar;
    [SerializeField] private PlayerHealthBarView opponentHealthBar;

    private void Start()
    {
        localHealthBar.Bind(100, 100);
        opponentHealthBar.Bind(100, 100);
    }

    public void SetValues(int localCurrent, int localMax, int opponentCurrent, int opponentMax)
    {
        localHealthBar.SetValue(localCurrent, localMax);
        opponentHealthBar.SetValue(opponentCurrent, opponentMax);
    }
}
