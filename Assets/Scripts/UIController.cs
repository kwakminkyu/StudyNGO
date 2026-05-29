using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;

public class UIController : MonoBehaviour
{
    private readonly struct LobbyProfileDefault
    {
        private readonly Sprite _avatarSprite;
        private readonly bool _avatarEnabled;
        private readonly string _nameText;

        public LobbyProfileDefault(Image avatarImage, TextMeshProUGUI nameText)
        {
            _avatarSprite = avatarImage != null ? avatarImage.sprite : null;
            _avatarEnabled = avatarImage != null && avatarImage.enabled;
            _nameText = nameText != null ? nameText.text : string.Empty;
        }

        public void ApplyTo(Image avatarImage, TextMeshProUGUI nameText)
        {
            if (avatarImage != null)
            {
                avatarImage.sprite = _avatarSprite;
                avatarImage.enabled = _avatarEnabled;
            }

            if (nameText != null)
            {
                nameText.text = _nameText;
            }
        }
    }
    
    [Header("Screens")]
    [SerializeField] private GameObject _startScreen;
    [SerializeField] private GameObject _lobbyScreen;

    [Header("Lobby Profile")]
    [SerializeField] private Image _player1Avatar;
    [SerializeField] private TextMeshProUGUI _player1Name;
    [SerializeField] private Image _player2Avatar;
    [SerializeField] private TextMeshProUGUI _player2Name;
    [SerializeField] private Image _player3Avatar;
    [SerializeField] private TextMeshProUGUI _player3Name;
    [SerializeField] private Image _player4Avatar;
    [SerializeField] private TextMeshProUGUI _player4Name;

    private Image[] _lobbyAvatars;
    private TextMeshProUGUI[] _lobbyNames;
    private LobbyProfileDefault[] _lobbyProfileDefaults;

    public int LobbyProfileCount => _lobbyAvatars?.Length ?? 0;

    private void Awake()
    {
        _lobbyAvatars = new[] { _player1Avatar, _player2Avatar, _player3Avatar, _player4Avatar };
        _lobbyNames = new[] { _player1Name, _player2Name, _player3Name, _player4Name };
        _lobbyProfileDefaults = new LobbyProfileDefault[_lobbyAvatars.Length];

        for (int i = 0; i < _lobbyAvatars.Length; i++)
        {
            _lobbyProfileDefaults[i] = new LobbyProfileDefault(_lobbyAvatars[i], _lobbyNames[i]);
        }
    }

    public void ShowStartScreen()
    {
        _startScreen.SetActive(true);
        _lobbyScreen.SetActive(false);
    }

    public void ShowLobbyScreen()
    {
        _startScreen.SetActive(false);
        _lobbyScreen.SetActive(true);
    }

    public void ShowGameScreen()
    {
        _startScreen.SetActive(false);
        _lobbyScreen.SetActive(false);
    }

    public void ClearLobbyProfiles()
    {
        for (int i = 0; i < LobbyProfileCount; i++)
        {
            ClearLobbyProfile(_lobbyAvatars[i], _lobbyNames[i]);
        }
    }

    public void ResetLobbyProfile(int playerIndex)
    {
        if (!TryGetLobbyProfile(playerIndex, out Image avatarImage, out TextMeshProUGUI nameText))
        {
            return;
        }

        _lobbyProfileDefaults[playerIndex].ApplyTo(avatarImage, nameText);
    }

    public void SetLobbyProfile(int playerIndex, CSteamID steamId)
    {
        if (!TryGetLobbyProfile(playerIndex, out Image avatarImage, out TextMeshProUGUI nameText))
        {
            return;
        }

        nameText.text = GetPersonaName(steamId);

        Sprite avatarSprite = CreateAvatarSprite(steamId);
        if (avatarSprite == null)
        {
            return;
        }

        avatarImage.sprite = avatarSprite;
        avatarImage.enabled = true;
    }

    private bool TryGetLobbyProfile(int playerIndex, out Image avatarImage, out TextMeshProUGUI nameText)
    {
        avatarImage = null;
        nameText = null;

        if (_lobbyAvatars == null
            || _lobbyNames == null
            || playerIndex < 0
            || playerIndex >= _lobbyAvatars.Length
            || playerIndex >= _lobbyNames.Length)
        {
            return false;
        }

        avatarImage = _lobbyAvatars[playerIndex];
        nameText = _lobbyNames[playerIndex];
        return avatarImage != null && nameText != null;
    }

    private static void ClearLobbyProfile(Image avatarImage, TextMeshProUGUI nameText)
    {
        if (avatarImage != null)
        {
            avatarImage.sprite = null;
            avatarImage.enabled = false;
        }

        if (nameText != null)
        {
            nameText.text = string.Empty;
        }
    }

    private static string GetPersonaName(CSteamID steamId)
    {
        if (steamId == SteamUser.GetSteamID())
        {
            return SteamFriends.GetPersonaName();
        }

        return SteamFriends.GetFriendPersonaName(steamId);
    }

    private static Sprite CreateAvatarSprite(CSteamID steamId)
    {
        int avatarHandle = SteamFriends.GetLargeFriendAvatar(steamId);
        if (avatarHandle <= 0)
        {
            return null;
        }

        if (!SteamUtils.GetImageSize(avatarHandle, out uint width, out uint height))
        {
            return null;
        }

        int bufferSize = (int)(width * height * 4);
        byte[] source = new byte[bufferSize];
        if (!SteamUtils.GetImageRGBA(avatarHandle, source, bufferSize))
        {
            return null;
        }

        byte[] flipped = new byte[bufferSize];
        int rowSize = (int)width * 4;
        for (int y = 0; y < height; y++)
        {
            System.Array.Copy(
                source,
                (height - 1 - y) * rowSize,
                flipped,
                y * rowSize,
                rowSize);
        }

        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(flipped);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));
    }
}
