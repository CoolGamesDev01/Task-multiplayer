using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaveServerButton : MonoBehaviour
{
    public void LeaveServer()
    {
        SceneManager.LoadScene("client_scene"); // Tutaj podaj nazwê sceny, do której chcesz siê cofn¹æ.
    }
}
