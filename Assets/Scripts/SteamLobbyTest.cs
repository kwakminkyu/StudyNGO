using UnityEngine;
using Unity.Netcode;
//using Netcode.Transports;
using Steamworks;

public class SteamLobbyTest : MonoBehaviour
{
    private enum LobbyVisibility
    {
        Private,
        FriendsOnly,
        Public,
        Invisible
    }

    private const string HostSteamIdKey = "HostSteamId";
    private const string GameStartedKey = "GameStarted";

    [SerializeField] private UIController _uiController;
    [SerializeField] private LobbyVisibility _lobbyVisibility = LobbyVisibility.FriendsOnly;
    [SerializeField, Range(1, 4)] private int _maxLobbyMembers = 4;

    private Callback<LobbyCreated_t> _lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;
    private Callback<LobbyEnter_t> _lobbyEntered;
    private Callback<LobbyDataUpdate_t> _lobbyDataUpdate;
    private Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
    private Callback<AvatarImageLoaded_t> _avatarImageLoaded;

    private CSteamID _currentLobbyId;
    private bool _networkCallbacksRegistered;
    private bool _isReturningToLobbyAfterOpponentLeft;
    private bool _steamLobbyInitialized;

    public bool IsInitialized => SteamManager.Initialized;
    public bool IsInLobby => _currentLobbyId.IsValid();

    private void Start()
    {
        TryInitializeSteamLobby();
        RegisterNetworkCallbacks();
    }

    private void Update()
    {
        TryInitializeSteamLobby();
        RegisterNetworkCallbacks();
    }

    private void OnDestroy()
    {
        UnregisterNetworkCallbacks();
        DisposeSteamCallbacks();
    }

    public void CreateLobby()
    {
        if (!EnsureSteamLobbyReady())
        {
            Debug.LogWarning("Steam is not initialized.");
            return;
        }

        SteamMatchmaking.CreateLobby(GetLobbyType(), _maxLobbyMembers);
    }

    public void SetLobbyVisibility(int visibilityIndex)
    {
        if (!System.Enum.IsDefined(typeof(LobbyVisibility), visibilityIndex))
        {
            Debug.LogWarning($"Invalid lobby visibility index: {visibilityIndex}");
            return;
        }

        _lobbyVisibility = (LobbyVisibility)visibilityIndex;
    }

    public void SetLobbyPrivate()
    {
        _lobbyVisibility = LobbyVisibility.Private;
    }

    public void SetLobbyFriendsOnly()
    {
        _lobbyVisibility = LobbyVisibility.FriendsOnly;
    }

    public void SetLobbyPublic()
    {
        _lobbyVisibility = LobbyVisibility.Public;
    }

    public void SetLobbyInvisible()
    {
        _lobbyVisibility = LobbyVisibility.Invisible;
    }

    public void StartGame()
    {
        StartGameAsHost();
    }

    public void StartGameAsHost()
    {
        if (!EnsureSteamLobbyReady())
        {
            Debug.LogWarning("Steam is not initialized.");
            return;
        }

        if (!_currentLobbyId.IsValid())
        {
            Debug.LogWarning("Lobby is not created yet.");
            return;
        }

        if (!IsLocalLobbyOwner())
        {
            Debug.LogWarning("Only the lobby owner can start the game.");
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            return;
        }

        if (!NetworkManager.Singleton.StartHost())
        {
            Debug.LogError("Failed to start host.");
            return;
        }

        _uiController?.ShowGameScreen();
        SteamMatchmaking.SetLobbyData(_currentLobbyId, GameStartedKey, "1");
    }

    public void InviteFriend()
    {
        if (!EnsureSteamLobbyReady())
        {
            Debug.LogWarning("Steam is not initialized.");
            return;
        }

        if (!_currentLobbyId.IsValid())
        {
            Debug.LogWarning("Lobby is not created yet.");
            return;
        }

        if (!SteamUtils.IsOverlayEnabled())
        {
            Debug.LogWarning("Steam overlay is not enabled. Invite dialog cannot be opened.");
            return;
        }
        Debug.Log($"Overlay Enabled: {SteamUtils.IsOverlayEnabled()}");
        Debug.Log($"Lobby Valid: {_currentLobbyId.IsValid()}");
        Debug.Log($"Lobby ID: {_currentLobbyId}");
        Debug.Log($"Lobby Owner: {SteamMatchmaking.GetLobbyOwner(_currentLobbyId)}");
        Debug.Log($"My Steam ID: {SteamUser.GetSteamID()}");

        SteamFriends.ActivateGameOverlayInviteDialog(_currentLobbyId);
        Debug.Log($"Opened Steam invite dialog. LobbyId: {_currentLobbyId}");
    }

    public void LeaveLobby()
    {
        if (!_currentLobbyId.IsValid())
        {
            return;
        }

        SteamMatchmaking.LeaveLobby(_currentLobbyId);
        _currentLobbyId = CSteamID.Nil;
    }

    public void SetLobbyWaiting()
    {
        if (!_currentLobbyId.IsValid() || !IsLocalLobbyOwner())
        {
            return;
        }

        SteamMatchmaking.SetLobbyData(_currentLobbyId, GameStartedKey, "0");
        RefreshLobbyProfiles();
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

    public bool IsLocalLobbyOwner()
    {
        return _currentLobbyId.IsValid()
            && SteamMatchmaking.GetLobbyOwner(_currentLobbyId) == SteamUser.GetSteamID();
    }

    private void TryInitializeSteamLobby()
    {
        if (_steamLobbyInitialized)
        {
            return;
        }

        if (!IsInitialized)
        {
            return;
        }

        Debug.Log("Steam Initialized");
        Debug.Log($"Steam Name: {SteamFriends.GetPersonaName()}");
        Debug.Log($"Steam ID: {SteamUser.GetSteamID()}");

        RegisterSteamCallbacks();
        _uiController?.ShowStartScreen();
        _steamLobbyInitialized = true;
    }

    private bool EnsureSteamLobbyReady()
    {
        TryInitializeSteamLobby();
        return _steamLobbyInitialized;
    }

    private ELobbyType GetLobbyType()
    {
        return _lobbyVisibility switch
        {
            LobbyVisibility.Private => ELobbyType.k_ELobbyTypePrivate,
            LobbyVisibility.Public => ELobbyType.k_ELobbyTypePublic,
            LobbyVisibility.Invisible => ELobbyType.k_ELobbyTypeInvisible,
            _ => ELobbyType.k_ELobbyTypeFriendsOnly
        };
    }

    private void RegisterSteamCallbacks()
    {
        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        _avatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
    }

    private void DisposeSteamCallbacks()
    {
        _lobbyCreated?.Dispose();
        _gameLobbyJoinRequested?.Dispose();
        _lobbyEntered?.Dispose();
        _lobbyDataUpdate?.Dispose();
        _lobbyChatUpdate?.Dispose();
        _avatarImageLoaded?.Dispose();

        _lobbyCreated = null;
        _gameLobbyJoinRequested = null;
        _lobbyEntered = null;
        _lobbyDataUpdate = null;
        _lobbyChatUpdate = null;
        _avatarImageLoaded = null;
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
            SetLobbyWaiting();
            networkManager.Shutdown();
            _uiController?.ShowLobbyScreen();
            _isReturningToLobbyAfterOpponentLeft = false;
            return;
        }

        if (!networkManager.IsServer)
        {
            LeaveLobby();
            _uiController?.ShowStartScreen();
        }
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"Lobby create failed: {callback.m_eResult}");
            return;
        }

        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        SteamMatchmaking.SetLobbyData(_currentLobbyId, HostSteamIdKey, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(_currentLobbyId, GameStartedKey, "0");
        SteamMatchmaking.SetLobbyJoinable(_currentLobbyId, true);

        Debug.Log($"Lobby created: {_currentLobbyId}");
        Debug.Log($"HostSteamId: {SteamUser.GetSteamID()}");

        _uiController?.ShowLobbyScreen();
        RefreshLobbyProfiles();
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"Join requested: {callback.m_steamIDLobby}");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        EChatRoomEnterResponse enterResponse = (EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse;
        if (enterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            Debug.LogError($"Lobby enter failed: {enterResponse}");
            return;
        }

        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        _uiController?.ShowLobbyScreen();
        RefreshLobbyProfiles();

        string hostSteamId = SteamMatchmaking.GetLobbyData(_currentLobbyId, HostSteamIdKey);

        Debug.Log($"Entered lobby: {_currentLobbyId}");
        Debug.Log($"HostSteamId from lobby: {hostSteamId}");

        TryStartClientFromLobbyData();
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
    {
        if (callback.m_ulSteamIDLobby != _currentLobbyId.m_SteamID || callback.m_bSuccess == 0)
        {
            return;
        }

        TryStartClientFromLobbyData();
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        if (callback.m_ulSteamIDLobby != _currentLobbyId.m_SteamID)
        {
            return;
        }

        RefreshLobbyProfiles();
    }

    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        if (!_currentLobbyId.IsValid())
        {
            return;
        }

        RefreshLobbyProfiles();
    }

    private void TryStartClientFromLobbyData()
    {
        if (!_currentLobbyId.IsValid() || IsLocalLobbyOwner())
        {
            return;
        }

        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (SteamMatchmaking.GetLobbyData(_currentLobbyId, GameStartedKey) != "1")
        {
            return;
        }

        string hostSteamId = SteamMatchmaking.GetLobbyData(_currentLobbyId, HostSteamIdKey);
        if (!ulong.TryParse(hostSteamId, out ulong hostSteamIdValue) || hostSteamIdValue == 0)
        {
            Debug.LogError($"Invalid host Steam ID in lobby data: {hostSteamId}");
            return;
        }

        // if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is not SteamNetworkingSocketsTransport steamTransport)
        // {
        //     Debug.LogError("Network transport is not SteamNetworkingSocketsTransport.");
        //     return;
        // }

        //steamTransport.ConnectToSteamID = hostSteamIdValue;
        Debug.Log($"Starting client for host Steam ID: {hostSteamId}");

        if (!NetworkManager.Singleton.StartClient())
        {
            Debug.LogError("Failed to start client.");
            return;
        }

        _uiController?.ShowGameScreen();
    }

    private void RefreshLobbyProfiles()
    {
        if (_uiController == null || !_currentLobbyId.IsValid())
        {
            return;
        }

        int profileCount = _uiController.LobbyProfileCount;
        bool[] filledProfiles = new bool[profileCount];

        CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(_currentLobbyId);
        if (lobbyOwner.IsValid() && profileCount > 0)
        {
            _uiController.SetLobbyProfile(0, lobbyOwner);
            filledProfiles[0] = true;
        }

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(_currentLobbyId);
        int nextProfileIndex = lobbyOwner.IsValid() ? 1 : 0;

        for (int i = 0; i < memberCount && nextProfileIndex < profileCount; i++)
        {
            CSteamID lobbyMember = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobbyId, i);
            if (!lobbyMember.IsValid() || lobbyMember == lobbyOwner)
            {
                continue;
            }

            _uiController.SetLobbyProfile(nextProfileIndex, lobbyMember);
            filledProfiles[nextProfileIndex] = true;
            nextProfileIndex++;
        }

        for (int i = 0; i < filledProfiles.Length; i++)
        {
            if (!filledProfiles[i])
            {
                _uiController.ResetLobbyProfile(i);
            }
        }
    }
}
