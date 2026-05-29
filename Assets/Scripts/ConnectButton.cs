using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class ConnectButton : MonoBehaviour
{
    private NetworkManager _networkManager;

    private void Awake()
    {
        _networkManager = GetComponent<NetworkManager>();
    }

    private void OnGUI()
    {
        if (_networkManager.IsClient || _networkManager.IsServer)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(10f, 10f, 180f, 120f));

        if (GUILayout.Button("Host"))
        {
            _networkManager.StartHost();
        }

        if (GUILayout.Button("Client"))
        {
            _networkManager.StartClient();
        }

        GUILayout.EndArea();
    }
}
