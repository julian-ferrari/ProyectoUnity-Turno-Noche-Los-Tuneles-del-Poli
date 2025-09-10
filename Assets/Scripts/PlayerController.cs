using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEditorInternal.Profiling.Memory.Experimental;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float jumpForce = 5f;

    [Header("Camera")]
    public Camera playerCamera;
    public float mouseSensitivity = 2f;
    public float upDownRange = 60f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask = 1;

    [Header("Stealth")]
    public float currentNoiseLevel = 0f;

    [Header("Inventory")]
    public List<string> keys = new List<string>();
    public int maxKeys = 10;

    [Header("Interaction UI")]
    public string currentInteractionMessage = "";
    private bool showingMessage = false;
    private float messageTimer = 0f;
    private float messageDuration = 3f;

    private Rigidbody rb;
    private bool isCrouched = false;
    private bool isGrounded = false;
    private float currentSpeed;
    private float verticalRotation = 0;

    // Variables para el nuevo Input System
    private Vector2 moveInput;
    private Vector2 mouseInput;
    private bool jumpInput;
    private bool sprintInput;
    private bool crouchInput;
    private bool flashlightInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentSpeed = walkSpeed;

        // ✅ CONFIGURACIÓN MEJORADA DEL RIGIDBODY
        if (rb != null)
        {
            // Congelar TODAS las rotaciones
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                            RigidbodyConstraints.FreezeRotationY |
                            RigidbodyConstraints.FreezeRotationZ;

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.linearDamping = 10f;  // Aumentar para detener más rápido
            rb.angularDamping = 100f; // MUY alto para evitar rotaciones

            // Asegurar que la masa sea consistente
            rb.mass = 1f;
        }

        // ✅ NUEVA configuración de cámara
        if (playerCamera == null)
        {
            // Buscar cámara hijo primero
            playerCamera = GetComponentInChildren<Camera>();

            // Si no encuentra una, usar la Main Camera
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        // Solo configurar posición si la cámara NO es hijo ya
        if (playerCamera != null && playerCamera.transform.parent != transform)
        {
            playerCamera.transform.position = transform.position + Vector3.up * 0.8f;
            playerCamera.transform.rotation = transform.rotation;
        }

        // Cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleMouseLook();
        HandleFlashlight();
        HandleInteraction();
        UpdateInteractionMessage();
        CheckGrounded();
    }

    void CheckGrounded()
    {
        // Raycast más preciso desde múltiples puntos
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;

        // Raycast principal
        bool centerHit = Physics.Raycast(rayStart, Vector3.down, 1.2f, groundMask);

        // Raycasts adicionales para mejor detección
        Vector3 forward = transform.forward * 0.3f;
        Vector3 right = transform.right * 0.3f;

        bool frontHit = Physics.Raycast(rayStart + forward, Vector3.down, 1.2f, groundMask);
        bool backHit = Physics.Raycast(rayStart - forward, Vector3.down, 1.2f, groundMask);
        bool rightHit = Physics.Raycast(rayStart + right, Vector3.down, 1.2f, groundMask);
        bool leftHit = Physics.Raycast(rayStart - right, Vector3.down, 1.2f, groundMask);

        isGrounded = centerHit || frontHit || backHit || rightHit || leftHit;

        // Debug visual
        Debug.DrawRay(rayStart, Vector3.down * 1.2f, isGrounded ? Color.green : Color.red);
        Debug.DrawRay(rayStart + forward, Vector3.down * 1.2f, frontHit ? Color.blue : Color.red);
        Debug.DrawRay(rayStart + right, Vector3.down * 1.2f, rightHit ? Color.blue : Color.red);

        // Debug solo ocasionalmente
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Ground Check - Center: {centerHit}, Front: {frontHit}, Back: {backHit}, Right: {rightHit}, Left: {leftHit}");
            Debug.Log($"Player Y: {transform.position.y}, Velocity Y: {rb.linearVelocity.y}");
        }
    }

    void HandleMovement()
    {
        // Obtener input de movimiento
        moveInput = Vector2.zero;

        if (Keyboard.current.aKey.isPressed) moveInput.x = -1f;
        if (Keyboard.current.dKey.isPressed) moveInput.x = 1f;
        if (Keyboard.current.wKey.isPressed) moveInput.y = 1f;
        if (Keyboard.current.sKey.isPressed) moveInput.y = -1f;

        // Movimiento relativo a la cámara
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3 movement = (forward * moveInput.y + right * moveInput.x).normalized * currentSpeed;

        // Aplicar movimiento manteniendo velocidad Y (para saltos)
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

        // Calcular nivel de ruido
        if (movement.magnitude > 0)
        {
            if (Keyboard.current.leftShiftKey.isPressed && !isCrouched)
            {
                currentSpeed = runSpeed;
                currentNoiseLevel = 3f;
            }
            else if (isCrouched)
            {
                currentSpeed = crouchSpeed;
                currentNoiseLevel = 0.5f;
            }
            else
            {
                currentSpeed = walkSpeed;
                currentNoiseLevel = 1f;
            }
        }
        else
        {
            currentNoiseLevel = 0f;
        }
    }

    void HandleJump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            currentNoiseLevel = 2f; // El salto hace ruido
        }
    }

    void HandleMouseLook()
    {
        // Obtener input del mouse
        mouseInput = Mouse.current.delta.ReadValue() * mouseSensitivity * Time.deltaTime;

        // Rotación horizontal (Y axis) - rotar el player
        transform.Rotate(0, mouseInput.x, 0);

        // Rotación vertical (X axis) - rotar la cámara
        verticalRotation -= mouseInput.y;
        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);

        if (playerCamera != null)
        {
            // Posición: seguir al player con offset
            Vector3 cameraPosition = transform.position + Vector3.up * 0.8f;
            playerCamera.transform.position = cameraPosition;

            // Rotación: combinar rotación del player (Y) + cámara (X)
            playerCamera.transform.rotation = Quaternion.Euler(verticalRotation, transform.eulerAngles.y, 0);
        }
    }

    void HandleCrouch()
    {
        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            isCrouched = !isCrouched;
            if (isCrouched)
            {
                transform.localScale = new Vector3(1, 0.5f, 1);
                // Bajar cámara al agacharse
                if (playerCamera != null)
                    playerCamera.transform.localPosition = new Vector3(0, 0.4f, 0);
            }
            else
            {
                transform.localScale = new Vector3(1, 1, 1);
                // Subir cámara al pararse
                if (playerCamera != null)
                    playerCamera.transform.localPosition = new Vector3(0, 0.8f, 0);
            }
        }
    }

    void HandleFlashlight()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            // Buscar linterna en hijos o crear una básica
            Light flashlight = GetComponentInChildren<Light>();

            if (flashlight == null)
            {
                // Crear linterna básica si no existe
                GameObject flashlightObj = new GameObject("Flashlight");
                flashlightObj.transform.SetParent(playerCamera.transform);
                flashlightObj.transform.localPosition = Vector3.zero;
                flashlightObj.transform.localRotation = Quaternion.identity;

                flashlight = flashlightObj.AddComponent<Light>();
                flashlight.type = LightType.Spot;
                flashlight.range = 10f;
                flashlight.spotAngle = 45f;
                flashlight.intensity = 2f;
                flashlight.color = Color.white;
                flashlight.enabled = false;
            }

            flashlight.enabled = !flashlight.enabled;
            Debug.Log("Linterna " + (flashlight.enabled ? "encendida" : "apagada"));
        }
    }

    // Para poder desbloquear el cursor con ESC (útil para testing)
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void HandleInteraction()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, 3f);

            foreach (Collider obj in nearbyObjects)
            {
                Door door = obj.GetComponent<Door>();
                if (door != null)
                {
                    door.TryInteract(this);
                    break;
                }
            }
        }
    }

    void UpdateInteractionMessage()
    {
        if (showingMessage)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f)
            {
                HideInteractionMessage();
            }
        }
    }

    public void PickupKey(KeyItem key)
    {
        if (keys.Count < maxKeys)
        {
            if (!keys.Contains(key.keyID))
            {
                keys.Add(key.keyID);
                ShowInteractionMessage("Recogiste: " + key.keyName);
                Debug.Log("Llave añadida al inventario: " + key.keyID);
            }
        }
        else
        {
            ShowInteractionMessage("¡Inventario lleno!");
        }
    }

    public bool HasKey(string keyID)
    {
        return keys.Contains(keyID);
    }

    public void UseKey(string keyID)
    {
    }

    public void ShowInteractionMessage(string message)
    {
        currentInteractionMessage = message;
        showingMessage = true;
        messageTimer = messageDuration;
    }

    public void HideInteractionMessage()
    {
        currentInteractionMessage = "";
        showingMessage = false;
        messageTimer = 0f;
    }

}