using UnityEngine;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField createLobbyNameField;
    [SerializeField] private TMP_InputField createLobbyMaxPlayersField;

    [SerializeField] private Transform lobbyItemPrefab;
    [SerializeField] private Transform lobbyContentParrent;

    public string joinedLobbyId;

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        ShowLobbies();
    }

    private async void ShowLobbies()
    {
        while (Application.isPlaying)
        {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            foreach (Transform t in lobbyContentParrent)
            {
                Destroy(t.gameObject);
            }

            foreach (Lobby lobby in queryResponse.Results)
            {
                Transform newLobbyItem = Instantiate(lobbyItemPrefab, lobbyContentParrent);
            }

            await Task.Delay(1000);
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
    }

}