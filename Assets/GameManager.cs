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
using System.Collections.Generic;

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
        if (MenuUI.activeInHierarchy)
        {
            MenuUI.SetActive(false);
        }
        else
        {
            MenuUI.SetActive(true);
        }
    }

    #region Relay Methods
    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            joinCodeText.text = $"Join code: {joinCode}";

            _transport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

            Debug.Log("Starting Host...");
            NetworkManager.Singleton.StartHost();

            GameObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
            ChangePlayerColor(playerPrefab, Color.blue);

            ShowHideMenuUI();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to create Relay session: {e.Message}");
        }
    }

    public async void JoinRelay(string joinCode = null)
    {
        if (string.IsNullOrEmpty(joinCode))
        {
            joinCode = joinCodeInputField.text;
        }

        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            _transport.SetClientRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();

            GameObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
            ChangePlayerColor(playerPrefab, Color.red);

            ShowHideMenuUI();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to join Relay session: {e.Message}");
        }
    }

    public async void CreateRelayAndLobby()
    {
        if (!int.TryParse(createLobbyMaxPlayersField.text, out int maxPlayers))
        {
            Debug.Log("Invalid number in max players!");
            return;
        }

        try
        {
            // Tworzenie Relay
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            joinCodeText.text = $"Join code: {joinCode}";

            _transport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

            Debug.Log("Starting Host...");
            NetworkManager.Singleton.StartHost();

            // Tworzenie Lobby i dodanie kodu relay
            await CreateLobbyWithRelay(createLobbyNameField.text, maxPlayers, joinCode);
            ShowHideMenuUI();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to create Relay session: {e.Message}");
        }
    }

    private void ChangePlayerColor(GameObject playerPrefab, Color color)
    {
        Renderer renderer = playerPrefab.GetComponent<Renderer>();

        renderer.sharedMaterial.color = color;
    }

    public void LeaveRelay()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Relay stopped by Host!");
            joinCodeText.text = "";
            ShowHideMenuUI();
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
                JoinLobbyButton joinLobbyButton = newLobbyItem.GetComponent<JoinLobbyButton>();
                joinLobbyButton.lobbyId = lobby.Id;
                newLobbyItem.GetChild(0).GetComponent<TextMeshProUGUI>().text = lobby.Name;
                newLobbyItem.GetChild(1).GetComponent<TextMeshProUGUI>().text = lobby.Players.Count + "/" + lobby.MaxPlayers;

                // Przekazanie kodu relay do przycisku do³¹czenia
                if (lobby.Data.ContainsKey("relayJoinCode"))
                {
                    joinLobbyButton.relayJoinCode = lobby.Data["relayJoinCode"].Value;
                }
            }

            await Task.Delay(2000);
        }
    }

    private async Task CreateLobbyWithRelay(string lobbyName, int maxPlayers, string relayJoinCode)
    {
        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions();
            options.Data = new Dictionary<string, DataObject>
            {
                { "relayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
            };

            createdLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            joinedLobbyId = createdLobby.Id;
            Debug.Log("Successfully created lobby with relay code!");
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
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID);
            joinedLobbyId = lobbyID;
            Debug.Log("Successfully connected to lobby with ID: " + joinedLobbyId);
            if (lobby.Data.ContainsKey("relayJoinCode"))
            {
                string relayJoinCode = lobby.Data["relayJoinCode"].Value;
                JoinRelay(relayJoinCode);
            }
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
                Debug.Log("Lobby is null, stopping heartbeat.");
                return;
            }

            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to send heartbeat: {e.Message}");
                return;              
            }

            await Task.Delay(15000);
        }
    }


    public async void LeaveLobby()
    {
        try
        {
            if (createdLobby != null)
            {
                await LobbyService.Instance.DeleteLobbyAsync(createdLobby.Id);
                createdLobby = null;
            }
            else if (!string.IsNullOrEmpty(joinedLobbyId))
            {
                var playerId = AuthenticationService.Instance.PlayerId;
                await LobbyService.Instance.RemovePlayerAsync(joinedLobbyId, playerId);
                joinedLobbyId = null;
                Debug.Log("Successfully left lobby.");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to leave lobby: {e.Message}");
        }
        finally
        {
            LeaveRelay();
        }
    }

    #endregion
}
