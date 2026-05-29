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

    private LobbyProfileDefault _player1Default;
    private LobbyProfileDefault _player2Default;

    private void Awake()
    {
        _player1Default = new LobbyProfileDefault(_player1Avatar, _player1Name);
        _player2Default = new LobbyProfileDefault(_player2Avatar, _player2Name);
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
        ClearLobbyProfile(_player1Avatar, _player1Name);
        ClearLobbyProfile(_player2Avatar, _player2Name);
    }

    public void ResetLobbyProfile(int playerIndex)
    {
        LobbyProfileDefault profileDefault = playerIndex == 0 ? _player1Default : _player2Default;
        Image avatarImage = playerIndex == 0 ? _player1Avatar : _player2Avatar;
        TextMeshProUGUI nameText = playerIndex == 0 ? _player1Name : _player2Name;

        profileDefault.ApplyTo(avatarImage, nameText);
    }

    public void SetLobbyProfile(int playerIndex, CSteamID steamId)
    {
        Image avatarImage = playerIndex == 0 ? _player1Avatar : _player2Avatar;
        TextMeshProUGUI nameText = playerIndex == 0 ? _player1Name : _player2Name;

        if (avatarImage == null || nameText == null)
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
