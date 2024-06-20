using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using TMPro;

public class RelayManager : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab; // Prefabrykat klocka Cube
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_Text joinCodeText;

    private UnityTransport _transport;
    private const int MaxPlayers = 5;

    private async void Start()
    {
        _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        await Authenticate();
    }

    private async Task Authenticate()
    {
        try
        {
            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Zalogowano pomyœlnie. Twoje ID: " + AuthenticationService.Instance.PlayerId);
            };
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"B³¹d uwierzytelniania: {e.Message}");
        }
    }

    public async void CreateRelay()
    {
        try
        {
            Allocation a = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);
            joinCodeText.text = $"Kod do³¹czenia: {joinCode}";

            _transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Nie uda³o siê utworzyæ sesji Relay: {e.Message}");
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
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Nie uda³o siê do³¹czyæ do sesji Relay: {e.Message}");
        }
    }
}

