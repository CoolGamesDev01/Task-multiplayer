using UnityEngine;

public class JoinLobbyButton : MonoBehaviour
{
    public string lobbyId;
    public string relayJoinCode;

    public void OnJoinLobbyButtonClicked()
    {
        GameManager.Instance.JoinLobby(lobbyId);

    }
}
