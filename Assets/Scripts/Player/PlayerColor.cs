using Unity.Netcode;
using UnityEngine;

public class PlayerColor : NetworkBehaviour
{
    private readonly NetworkVariable<Color> netColor = new();
    private readonly Color[] colors = { Color.green, Color.blue, Color.red, Color.yellow };
    private int index;

    [SerializeField] private MeshRenderer _renderer;

    private void Awake()
    {
        netColor.OnValueChanged += OnValueChanged;
    }

    public override void OnDestroy()
    {
        netColor.OnValueChanged -= OnValueChanged;
    }

    private void OnValueChanged(Color prev, Color next)
    {
        _renderer.material.color = next;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            index = (int)OwnerClientId;
            CommitNetworkColorServerRpc(GetNextColor());
        }
        else
        {
            _renderer.material.color = netColor.Value;
        }
    }

    [ServerRpc]
    private void CommitNetworkColorServerRpc(Color color)
    {
        netColor.Value = color;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        CommitNetworkColorServerRpc(GetNextColor());
    }

    private Color GetNextColor()
    {
        return colors[index++ % colors.Length];
    }
}