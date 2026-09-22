using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Move a cápsula com WASD (ou setas) e impede que ela atravesse / caia
/// por baixo do terreno plano. Inclui mouse look (FPS) com a câmara filha do Player.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CapsuleMovement : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float turnSmoothTime = 0.08f;
    [SerializeField] private float jumpForce = 5f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 1f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private bool lockCursor = true;

    [Header("Chão")]
    [Tooltip("Transform do terreno plano. Se vazio, é procurado automaticamente um objeto chamado \"Ground\".")]
    [SerializeField] private Transform ground;
    [Tooltip("Distância extra que a cápsula pode descer antes de ser corrigida (evita atravessar o chão).")]
    [SerializeField] private float groundTolerance = 0.35f;
    [SerializeField] private bool preventFallingThrough = true;

    private Rigidbody rb;
    private Collider bodyCollider;
    private Collider groundCollider;
    private Vector3 moveInput;
    private float turnVelocity;

    private Camera playerCamera;
    private float pitch = 0f;
    private bool isGrounded;
    private float groundCheckDistance = 0.1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (ground == null)
        {
            GameObject found = GameObject.Find("Ground");
            if (found != null)
                ground = found.transform;
        }

        // Câmara é filha do Player
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        ReadInput();
        HandleMouseLook();
    }

    private void FixedUpdate()
    {
        CheckGrounded();

        Vector3 direction = CameraRelativeDirection(moveInput);

        Vector3 velocity = rb.linearVelocity;
        velocity.x = direction.x * moveSpeed;
        velocity.z = direction.z * moveSpeed;

        // Salto
        if (isGrounded && Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
        {
            velocity.y = jumpForce;
        }

        rb.linearVelocity = velocity;

        if (preventFallingThrough)
            KeepAboveGround();
    }

    private void CheckGrounded()
    {
        if (bodyCollider == null) return;
        float rayStart = transform.position.y;
        float rayLength = bodyCollider.bounds.extents.y + groundCheckDistance;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, rayLength, ~0, QueryTriggerInteraction.Ignore);
    }

    private void ReadInput()
    {
        moveInput = Vector3.zero;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveInput.z += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveInput.z -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput.x -= 1f;
        }

        moveInput = Vector3.ClampMagnitude(moveInput, 1f);
    }

    private void HandleMouseLook()
    {
        if (playerCamera == null)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 mouseDelta = mouse.delta.ReadValue();

        // Yaw: roda o Player (e a câmara junto) no eixo Y
        float yaw = mouseDelta.x * mouseSensitivity;
        transform.Rotate(0f, yaw, 0f);

        // Pitch: roda apenas a câmara localmente no eixo X
        pitch -= mouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    /// <summary>W = para a frente da câmara, A/D = esquerda/direita da câmara.</summary>
    private Vector3 CameraRelativeDirection(Vector3 input)
    {
        if (input.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null)
            return input;

        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return Vector3.ClampMagnitude(forward * input.z + right * input.x, 1f);
    }

    private void RotateTo(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                                            ref turnVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    /// <summary>
    /// Rede de segurança: se a cápsula chegar a entrar por baixo do topo do
    /// terreno (túnel de colisão a alta velocidade), é empurrada de volta
    /// para cima e a velocidade descendente é anulada.
    /// </summary>
    private void KeepAboveGround()
    {
        float halfHeight = bodyCollider != null ? bodyCollider.bounds.extents.y : 1f;

        float? floorY = FloorHeightBelow(halfHeight);
        if (!floorY.HasValue)
            return;

        float feetY = transform.position.y - halfHeight;
        float floor = floorY.Value;

        if (feetY >= floor - 0.001f)
            return;

        Vector3 position = transform.position;
        position.y = floor + halfHeight;
        transform.position = position;

        Vector3 velocity = rb.linearVelocity;
        if (velocity.y < 0f)
            velocity.y = 0f;
        rb.linearVelocity = velocity;
    }

    private float? FloorHeightBelow(float halfHeight)
    {
        float probeDistance = halfHeight + groundTolerance;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit,
                            probeDistance, ~0, QueryTriggerInteraction.Ignore))
            return hit.point.y;

        if (ground == null)
            return null;

        if (groundCollider == null)
            groundCollider = ground.GetComponent<Collider>();

        if (groundCollider == null)
            return ground.position.y;

        Bounds bounds = groundCollider.bounds;
        Vector3 pos = transform.position;
        Vector3 flatPosition = new Vector3(pos.x, bounds.center.y, pos.z);

        return bounds.Contains(flatPosition) ? bounds.max.y : (float?)null;
    }
}