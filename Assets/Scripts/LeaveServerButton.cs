using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaveServerButton : MonoBehaviour
{
    public void LeaveServer()
    {
        SceneManager.LoadScene("client_scene"); // Tutaj podaj nazw� sceny, do kt�rej chcesz si� cofn��.
    }
}
