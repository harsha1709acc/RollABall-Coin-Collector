using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Keeps movement physics-based so the ball rolls naturally instead of sliding by transform edits.
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float speed = 12f;
    public float jumpForce = 7f;
    private Rigidbody body;
    private Vector3 startPosition;
    private GameManager gameManager;
    private bool grounded;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        startPosition = transform.position;
        gameManager = FindObjectOfType<GameManager>();
    }

    private void Update()
    {
        if (gameManager != null && gameManager.State != GameManager.GameState.Playing) return;
        if (transform.position.y < -5f)
        {
            transform.position = startPosition;
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = Vector3.zero;
#else
            body.velocity = Vector3.zero;
#endif
            if (gameManager != null) gameManager.Lose();
            return;
        }
        grounded = Physics.Raycast(transform.position, Vector3.down, 0.62f);
        if (JumpPressed() && grounded) body.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        if (gameManager != null && gameManager.State != GameManager.GameState.Playing) return;
        Vector2 input = MoveInput();
        body.AddForce(new Vector3(input.x, 0f, input.y) * speed, ForceMode.Force);
    }

    private Vector2 MoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return Vector2.zero;
        float x = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1 : 0) -
                  (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1 : 0);
        float z = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1 : 0) -
                  (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1 : 0);
        return new Vector2(x, z).normalized;
#else
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;
#endif
    }

    private bool JumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}
