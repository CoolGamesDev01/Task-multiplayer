using UnityEngine;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [SerializeField] private TMP_InputField createLobbyNameField;
    [SerializeField] private TMP_InputField createLobbyMaxPlayersField;

    [SerializeField] private Transform lobbyItemPrefab;
    [SerializeField] private Transform lobbyContentParrent;

    public string joinedLobbyId;
    private Lobby createdLobby;

    private async void Start()
    {
        Instance = this;

        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        ShowLobbies();
    }

    private async void ShowLobbies()
    {
        while (true)
        {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            foreach (Transform t in lobbyContentParrent)
            {
                Destroy(t.gameObject);
            }

            foreach (Lobby lobby in queryResponse.Results)
            {
                Transform newLobbyItem = Instantiate(lobbyItemPrefab, lobbyContentParrent);
                newLobbyItem.GetComponent<JoinLobbyButton>().lobbyId = lobby.Id;               
                newLobbyItem.GetChild(0).GetComponent<TextMeshProUGUI>().text = lobby.Name;
                newLobbyItem.GetChild(1).GetComponent<TextMeshProUGUI>().text = lobby.Players.Count + "/" + lobby.MaxPlayers;
            }

            await Task.Delay(2 * 1000);
        }
    }

    public async void CreateLobby()
    {
        if(!int.TryParse(createLobbyMaxPlayersField.text, out int maxPlayers))
        {
            return;
        }

        Lobby createdLobby = null;

        try
        {
            createdLobby = await LobbyService.Instance.CreateLobbyAsync(createLobbyNameField.text, maxPlayers);
            joinedLobbyId = createdLobby.Id;
            Debug.Log("Utworzobo lobby pomyœlnie!");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        LobbyHeartbeat(createdLobby);
    }

    public async void JoinLobby(string lobbyID)
    {
        try
        {
            await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID);

            joinedLobbyId = lobbyID;
            Debug.Log("Do³¹czono pomyœlnie do lobby od id:" + joinedLobbyId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
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

            await Task.Delay(15 * 1000);
        }
    }

    private async void OnApplicationQuit()
    {
        if (createdLobby != null)
        {
            // Gracz, który utworzy³ lobby, usuwa lobby przy zamkniêciu gry
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(createdLobby.Id);
                Debug.Log("Lobby usuniête pomyœlnie!");
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
        else if (!string.IsNullOrEmpty(joinedLobbyId))
        {
            // Gracz, który do³¹czy³ do lobby, opuszcza lobby przy zamkniêciu gry
            try
            {
        
                // Gracz, który do³¹czy³ do lobby, opuszcza lobby przy zamkniêciu gry
                var playerId = AuthenticationService.Instance.PlayerId;
                await LobbyService.Instance.RemovePlayerAsync(joinedLobbyId, playerId);
                Debug.Log("Opuœciliœmy lobby pomyœlnie!");
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

}