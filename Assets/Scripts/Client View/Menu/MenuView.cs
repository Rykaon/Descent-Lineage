using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public sealed class MenuView : MonoBehaviour
{
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private TMP_InputField portInput;
    [SerializeField] private string gameClientSceneName = "GameClient";

    private void Start()
    {
        addressInput.text = "127.0.0.1";
        portInput.text = "7779";
    }

    public void OnPlaytestClicked()
    {
        NetworkConfig.Role = NetworkGameRole.Client;
        NetworkConfig.Address = addressInput.text;

        if (ushort.TryParse(portInput.text, out ushort port))
        {
            NetworkConfig.Port = port;
        }
        else
        {
            NetworkConfig.Port = 7778;
        }

        SceneManager.LoadScene(gameClientSceneName);
    }
}