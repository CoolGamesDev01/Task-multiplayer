using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // SprawdŸ, czy obiekt jest lokalnym graczem (tylko lokalny gracz bêdzie móg³ sterowaæ ruchem)
        if (IsLocalPlayer)
        {
            // W³¹cz obs³ugê poruszania siê gracza
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
        rb.velocity = direction * moveSpeed;
    }
}
