using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Sprawd�, czy obiekt jest lokalnym graczem (tylko lokalny gracz b�dzie m�g� sterowa� ruchem)
        if (IsLocalPlayer)
        {
            // W��cz obs�ug� poruszania si� gracza
            enabled = true;
        }
    }

    void Update()
    {
        if (!IsLocalPlayer)
            return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }
}
