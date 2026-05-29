using UnityEngine;
using Steamworks;
using Unity.Netcode;

public class SteamBootstrap  : MonoBehaviour
{
    [SerializeField] private UIController _uiController;
    private SteamLobbyTest _steamLobbyTest;
    private bool _networkCallbacksRegistered;
    private bool _isReturningToLobbyAfterOpponentLeft;
    private bool _steamLobbyInitialized;

    public bool IsInitialized => SteamManager.Initialized;

    private void Start()
    {
        TryInitializeSteamLobby();
        RegisterNetworkCallbacks();
    }

    private void Update()
    {
        RegisterNetworkCallbacks();
    }

    private void TryInitializeSteamLobby()
    {
        if (_steamLobbyInitialized)
        {
            return;
        }

        if (IsInitialized)
        {
            Debug.Log("Steam Initialized");
            Debug.Log($"Steam Name: {SteamFriends.GetPersonaName()}");
            Debug.Log($"Steam ID: {SteamUser.GetSteamID()}");

            _steamLobbyTest = new SteamLobbyTest(_uiController);
            _uiController?.ShowStartScreen();
            _steamLobbyInitialized = true;
        }
    }

    private void OnDestroy()
    {
        UnregisterNetworkCallbacks();

        _steamLobbyTest?.Dispose();
        _steamLobbyTest = null;
    }

    public void CreateLobby()
    {
        if (!EnsureSteamLobbyReady())
        {
            Debug.LogWarning("Steam is not initialized.");
            return;
        }

        _steamLobbyTest.CreateLobby();
    }

    public void StartGame()
    {
        if (!EnsureSteamLobbyReady())
        {
            Debug.LogWarning("Steam is not initialized.");
            return;
        }

        _steamLobbyTest.StartGameAsHost();
    }

    public void InviteFriend()
    {
        if (!EnsureSteamLobbyReady())
        {
            Debug.LogWarning("Steam is not initialized.");
            return;
        }

        _steamLobbyTest.InviteFriend();
    }

    private bool EnsureSteamLobbyReady()
    {
        TryInitializeSteamLobby();
        return _steamLobbyTest != null;
    }

    public void PrintSteamStatus()
    {
        Debug.Log($"Steam Initialized: {IsInitialized}");

        if (IsInitialized)
        {
            Debug.Log($"Steam Name: {SteamFriends.GetPersonaName()}");
            Debug.Log($"Steam ID: {SteamUser.GetSteamID()}");
            Debug.Log($"Steam Overlay Enabled: {SteamUtils.IsOverlayEnabled()}");
        }
    }

    private void RegisterNetworkCallbacks()
    {
        if (_networkCallbacksRegistered || NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        _networkCallbacksRegistered = true;
    }

    private void UnregisterNetworkCallbacks()
    {
        if (!_networkCallbacksRegistered || NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        _networkCallbacksRegistered = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            return;
        }

        if (_isReturningToLobbyAfterOpponentLeft)
        {
            return;
        }

        if (networkManager.IsServer && clientId != NetworkManager.ServerClientId)
        {
            _isReturningToLobbyAfterOpponentLeft = true;
            _steamLobbyTest?.SetLobbyWaiting();
            networkManager.Shutdown();
            _uiController.ShowLobbyScreen();
            _isReturningToLobbyAfterOpponentLeft = false;
            return;
        }

        if (!networkManager.IsServer)
        {
            _steamLobbyTest?.LeaveLobby();
            _uiController.ShowStartScreen();
        }
    }
}
