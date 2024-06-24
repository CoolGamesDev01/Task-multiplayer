using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Relay UI Elements")]
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_Text joinCodeText;

    [Header("Lobby UI Elements")]
    [SerializeField] private TMP_InputField createLobbyNameField;
    [SerializeField] private TMP_InputField createLobbyMaxPlayersField;
    [SerializeField] private Transform lobbyItemPrefab;
    [SerializeField] private Transform lobbyContentParent;


    [SerializeField] private GameObject MenuUI;
    [SerializeField] private GameObject LeaveButton;

    private UnityTransport _transport;
    private const int MaxPlayers = 5;
    public string joinedLobbyId;
    private Lobby createdLobby;

    private async void Start()
    {
        Instance = this;
        _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        await UnityServices.InitializeAsync();
        await SignInAnonymouslyAsync();
        ShowLobbies();
    }

    private async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in anonymously");
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Failed to sign in: {ex.Message}");
        }
    }

    public void ShowHideMenuUI()
    {
        if(MenuUI.activeInHierarchy)
        {
            MenuUI.SetActive(false);
            LeaveButton.SetActive(true);
        }
        else
        {
            MenuUI.SetActive(true);
            LeaveButton.SetActive(false);
        }
    }

    #region Relay Methods
    public async void CreateRelay()
    {
        try
        {
            Allocation a = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);
            joinCodeText.text = $"Join code: {joinCode}";

            _transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);

            Debug.Log("Starting Host...");
            NetworkManager.Singleton.StartHost();
            ShowHideMenuUI();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to create Relay session: {e.Message}");
        }
    }

    public async void JoinRelay()
    {
        try
        {
            string joinCode = joinCodeInputField.text;
            JoinAllocation a = await RelayService.Instance.JoinAllocationAsync(joinCode);

            _transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);

            NetworkManager.Singleton.StartClient();
            ShowHideMenuUI();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to join Relay session: {e.Message}");
        }
    }
    #endregion

    #region Lobby Methods
    private async void ShowLobbies()
    {
        while (true)
        {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            foreach (Transform t in lobbyContentParent)
            {
                DestroyImmediate(t.gameObject);
            }

            foreach (Lobby lobby in queryResponse.Results)
            {
                Transform newLobbyItem = Instantiate(lobbyItemPrefab, lobbyContentParent);
                newLobbyItem.GetComponent<JoinLobbyButton>().lobbyId = lobby.Id;
                newLobbyItem.GetChild(0).GetComponent<TextMeshProUGUI>().text = lobby.Name;
                newLobbyItem.GetChild(1).GetComponent<TextMeshProUGUI>().text = lobby.Players.Count + "/" + lobby.MaxPlayers;
            }

            await Task.Delay(2000);
        }
    }

    public async void CreateLobby()
    {
        if (!int.TryParse(createLobbyMaxPlayersField.text, out int maxPlayers))
        {
            return;
        }

        try
        {
            createdLobby = await LobbyService.Instance.CreateLobbyAsync(createLobbyNameField.text, maxPlayers);
            joinedLobbyId = createdLobby.Id;
            Debug.Log("Succesfull created lobby!");
            LobbyHeartbeat(createdLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to create lobby: {e.Message}");
        }
    }

    public async void JoinLobby(string lobbyID)
    {
        try
        {
            await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID);
            joinedLobbyId = lobbyID;
            Debug.Log("Succesfull connected to lobby with ID: " + joinedLobbyId);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
        }
    }

    private async void LobbyHeartbeat(Lobby lobby)
    {
        while (true)
        {
            if (lobby == null)
            {
                return;
            }

            await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);
            await Task.Delay(15000);
        }
    }
    #endregion

    public async void LeaveLobby()
    {
        if (createdLobby != null)
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(createdLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to delete lobby: {e.Message}");
            }
        }
        else if (!string.IsNullOrEmpty(joinedLobbyId))
        {
            try
            {
                var playerId = AuthenticationService.Instance.PlayerId;
                await LobbyService.Instance.RemovePlayerAsync(joinedLobbyId, playerId);
                Debug.Log("Succesfull leave lobby.");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to leave lobby: {e.Message}");
            }
        }
    }

    public void LeaveRelay()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Relay stopped by Host!");
        }
    }

}
