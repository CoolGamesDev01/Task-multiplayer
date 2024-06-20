using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    private Vector2 moveInput;
    private bool isJumping;

    void Update()
    {
        Move();

        // Obs�uga skoku na urz�dzeniach mobilnych
        if (IsLocalPlayer)
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                CheckMobileInput();
            }
            else
            {
                CheckKeyboardInput();
            }
        }
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnJump()
    {
        isJumping = true;
    }

    private void Move()
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Skok
        if (isJumping)
        {
            Jump();
            isJumping = false;
        }
    }

    private void Jump()
    {
        // Implementacja skoku, np. dodanie si�y do Rigidbody lub zmiana pozycji gracza w g�r�
        // Przyk�ad dodania si�y do Rigidbody:
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void CheckKeyboardInput()
    {
        // Mo�esz doda� obs�ug� skoku na klawiaturze, np. po naci�ni�ciu spacji
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isJumping = true;
        }
    }

    private void CheckMobileInput()
    {
        // Obs�uga skoku na ekranie dotykowym
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                isJumping = true;
            }
        }
    }
}
