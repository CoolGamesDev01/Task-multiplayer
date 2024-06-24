using UnityEngine;

public class JoinLobbyButton : MonoBehaviour
{
    public string lobbyId;

    public void OnJoinLobbyButtonClicked()
    {
        GameManager.Instance.JoinLobby(lobbyId);
    }
}
