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
    [Tooltip("Se ativo, usa as setas do teclado em vez de WASD.")]
    [SerializeField] private bool useArrowKeys = false;
    [SerializeField] private float moveSpeed = 6f;

    [Header("Mouse Look")]
    [Tooltip("Desativar na cápsula que é movida apenas pelas setas.")]
    [SerializeField] private bool mouseLook = true;
    [SerializeField] private float mouseSensitivity = 0.8f;
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

    private Camera playerCamera;
    private float pitch = 0f;

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

        if (mouseLook && lockCursor)
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
        Vector3 direction = CameraRelativeDirection(moveInput);

        Vector3 velocity = rb.linearVelocity;
        velocity.x = direction.x * moveSpeed;
        velocity.z = direction.z * moveSpeed;
        rb.linearVelocity = velocity;

        if (preventFallingThrough)
            KeepAboveGround();
    }

    private void ReadInput()
    {
        moveInput = Vector3.zero;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (useArrowKeys)
        {
            moveInput.z = (keyboard.upArrowKey.isPressed ? 1f : 0f) + (keyboard.downArrowKey.isPressed ? -1f : 0f);
            moveInput.x = (keyboard.rightArrowKey.isPressed ? 1f : 0f) + (keyboard.leftArrowKey.isPressed ? -1f : 0f);
        }
        else
        {
            moveInput.z = (keyboard.wKey.isPressed ? 1f : 0f) + (keyboard.sKey.isPressed ? -1f : 0f);
            moveInput.x = (keyboard.dKey.isPressed ? 1f : 0f) + (keyboard.aKey.isPressed ? -1f : 0f);
        }

        moveInput = Vector3.ClampMagnitude(moveInput, 1f);
    }

    private void HandleMouseLook()
    {
        if (!mouseLook || playerCamera == null)
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

    /// <summary>Moves relative to camera forward/right axes.</summary>
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