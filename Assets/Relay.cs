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
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_Text joinCodeText;


    private UnityTransport _transport;
    private const int MaxPlayers = 5;

    private async void Awake()
    {
        _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        await Authenticate();
    }

    private async Task Authenticate()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateRelay()
    {
        try
        {
            Allocation a = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);
            joinCodeText.text = $"Join Code: {joinCode}";

            _transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
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
            Debug.LogError(e);
        }
    }
}
