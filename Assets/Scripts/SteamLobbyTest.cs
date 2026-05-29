using UnityEngine;
using Unity.Netcode;
//using Netcode.Transports;
using Steamworks;

public class SteamLobbyTest
{
    private const string HostSteamIdKey = "HostSteamId";
    private const string GameStartedKey = "GameStarted";

    private Callback<LobbyCreated_t> _lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;
    private Callback<LobbyEnter_t> _lobbyEntered;
    private Callback<LobbyDataUpdate_t> _lobbyDataUpdate;
    private Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
    private Callback<AvatarImageLoaded_t> _avatarImageLoaded;

    private readonly UIController _uiController;
    private CSteamID _currentLobbyId;

    public bool IsInLobby => _currentLobbyId.IsValid();

    public SteamLobbyTest(UIController uiController)
    {
        _uiController = uiController;

        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        _avatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
    }

    public void Dispose()
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

    public void CreateLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
    }

    public void StartGameAsHost()
    {
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

        SteamFriends.ActivateGameOverlayInviteDialog(_currentLobbyId);
        Debug.Log("Opened Steam friends overlay.");
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

    public bool IsLocalLobbyOwner()
    {
        return SteamMatchmaking.GetLobbyOwner(_currentLobbyId) == SteamUser.GetSteamID();
    }

    private void RefreshLobbyProfiles()
    {
        if (_uiController == null || !_currentLobbyId.IsValid())
        {
            return;
        }

        bool player1Filled = false;
        bool player2Filled = false;

        CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(_currentLobbyId);
        if (lobbyOwner.IsValid())
        {
            _uiController.SetLobbyProfile(0, lobbyOwner);
            player1Filled = true;
        }

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(_currentLobbyId);
        int nextProfileIndex = lobbyOwner.IsValid() ? 1 : 0;

        for (int i = 0; i < memberCount && nextProfileIndex < 2; i++)
        {
            CSteamID lobbyMember = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobbyId, i);
            if (!lobbyMember.IsValid() || lobbyMember == lobbyOwner)
            {
                continue;
            }

            _uiController.SetLobbyProfile(nextProfileIndex, lobbyMember);
            if (nextProfileIndex == 0)
            {
                player1Filled = true;
            }
            else
            {
                player2Filled = true;
            }

            nextProfileIndex++;
        }

        if (!player1Filled)
        {
            _uiController.ResetLobbyProfile(0);
        }

        if (!player2Filled)
        {
            _uiController.ResetLobbyProfile(1);
        }
    }
}
