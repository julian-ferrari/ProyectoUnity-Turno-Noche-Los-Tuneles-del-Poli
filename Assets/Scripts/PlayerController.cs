using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float jumpForce = 5f;

    [Header("Step Climbing")]
    public float maxStepHeight = 0.25f;
    public float stepCheckDistance = 0.4f;
    public LayerMask stepMask = 1;

    [Header("Camera")]
    public Camera playerCamera;
    public float mouseSensitivity = 2f;
    public float upDownRange = 60f;
    public float normalCameraHeight = 0.8f;
    public float crouchCameraHeight = 0.6f;

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

    // Components
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private FlashlightFPSController flashlightController; // ✅ Nueva referencia

    // State variables
    private bool isCrouched = false;
    private bool isGrounded = false;
    private float currentSpeed;
    private float verticalRotation = 0;

    // Camera variables
    private Transform cameraHolder;
    private float targetCameraHeight;
    private float currentCameraHeight;

    // Collider original variables
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    // Input variables
    private Vector2 moveInput;
    private Vector2 mouseInput;

    void Start()
    {
        // Get components
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        currentSpeed = walkSpeed;

        // Guardar configuración original del collider
        if (playerCollider != null)
        {
            originalColliderHeight = playerCollider.height;
            originalColliderCenter = playerCollider.center;
            Debug.Log($"Collider original guardado - Height: {originalColliderHeight}, Center: {originalColliderCenter}");
        }
        else
        {
            Debug.LogError("¡No se encontró CapsuleCollider en el jugador!");
        }

        // Rigidbody setup
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                            RigidbodyConstraints.FreezeRotationY |
                            RigidbodyConstraints.FreezeRotationZ;

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.linearDamping = 10f;
            rb.angularDamping = 100f;
            rb.mass = 1f;
        }

        // Camera setup
        SetupCamera();

        // ✅ Configurar linterna FPS después de la cámara
        SetupFlashlight();

        // Cursor setup
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize camera height
        targetCameraHeight = normalCameraHeight;
        currentCameraHeight = normalCameraHeight;
    }

    void SetupCamera()
    {
        // Find camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        if (playerCamera != null)
        {
            // Create camera holder as child of player
            GameObject holderObj = GameObject.Find("CameraHolder");
            if (holderObj == null || holderObj.transform.parent != transform)
            {
                holderObj = new GameObject("CameraHolder");
                holderObj.transform.SetParent(transform);
                holderObj.transform.localPosition = new Vector3(0, normalCameraHeight, 0);
                holderObj.transform.localRotation = Quaternion.identity;
            }
            cameraHolder = holderObj.transform;

            // Make camera child of camera holder
            if (playerCamera.transform.parent != cameraHolder)
            {
                playerCamera.transform.SetParent(cameraHolder);
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }

            Debug.Log("Cámara configurada correctamente");
        }
        else
        {
            Debug.LogError("No se encontró ninguna cámara!");
        }
    }

    // ✅ NUEVO: Configurar sistema de linterna FPS
    void SetupFlashlight()
    {
        if (playerCamera != null)
        {
            // Buscar si ya existe el componente
            flashlightController = playerCamera.GetComponent<FlashlightFPSController>();

            // Si no existe, agregarlo
            if (flashlightController == null)
            {
                flashlightController = playerCamera.gameObject.AddComponent<FlashlightFPSController>();
                Debug.Log("FlashlightFPSController agregado a la cámara");
            }
        }
        else
        {
            Debug.LogError("No se puede configurar linterna: cámara no encontrada");
        }
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleMouseLook();
        HandleFlashlight(); // ✅ Mantener por compatibilidad, pero ahora usa el nuevo sistema
        HandleInteraction();
        UpdateInteractionMessage();
        UpdateCameraHeight();
        CheckGrounded();
    }

    void UpdateCameraHeight()
    {
        if (cameraHolder != null)
        {
            currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraHeight, Time.deltaTime * 8f);
            cameraHolder.transform.localPosition = new Vector3(0, currentCameraHeight, 0);
        }
    }

    void CheckGrounded()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        bool centerHit = Physics.Raycast(rayStart, Vector3.down, 1.2f, groundMask);

        Vector3 forward = transform.forward * 0.3f;
        Vector3 right = transform.right * 0.3f;

        bool frontHit = Physics.Raycast(rayStart + forward, Vector3.down, 1.2f, groundMask);
        bool backHit = Physics.Raycast(rayStart - forward, Vector3.down, 1.2f, groundMask);
        bool rightHit = Physics.Raycast(rayStart + right, Vector3.down, 1.2f, groundMask);
        bool leftHit = Physics.Raycast(rayStart - right, Vector3.down, 1.2f, groundMask);

        isGrounded = centerHit || frontHit || backHit || rightHit || leftHit;

        Debug.DrawRay(rayStart, Vector3.down * 1.2f, isGrounded ? Color.green : Color.red);
    }

    void HandleMovement()
    {
        moveInput = Vector2.zero;

        if (Keyboard.current.aKey.isPressed) moveInput.x = -1f;
        if (Keyboard.current.dKey.isPressed) moveInput.x = 1f;
        if (Keyboard.current.wKey.isPressed) moveInput.y = 1f;
        if (Keyboard.current.sKey.isPressed) moveInput.y = -1f;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3 movement = (forward * moveInput.y + right * moveInput.x).normalized * currentSpeed;

        if (movement.magnitude > 0 && isGrounded)
        {
            movement = HandleStepClimbing(movement);
        }

        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

        // Calculate noise level
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

    Vector3 HandleStepClimbing(Vector3 movement)
    {
        if (movement.magnitude < 0.1f) return movement;

        float colliderRadius = playerCollider != null ? playerCollider.radius : 0.5f;
        float colliderHeight = playerCollider != null ? playerCollider.height : 1.8f;

        Vector3 rayDirection = movement.normalized;
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        if (!Physics.Raycast(origin, rayDirection, out RaycastHit frontHit, stepCheckDistance, stepMask))
            return movement;

        Vector3 stepCheckOrigin = frontHit.point + Vector3.up * maxStepHeight + rayDirection * 0.05f;
        if (!Physics.Raycast(stepCheckOrigin, Vector3.down, out RaycastHit stepHit, maxStepHeight + 0.3f, groundMask))
            return movement;

        float stepHeight = stepHit.point.y - transform.position.y;

        if (stepHeight > 0.05f && stepHeight <= maxStepHeight)
        {
            float climbSpeed = 12f;
            Vector3 targetPos = new Vector3(rb.position.x, stepHit.point.y, rb.position.z);
            rb.position = Vector3.Lerp(rb.position, targetPos, Time.deltaTime * climbSpeed);
            return movement;
        }

        return movement;
    }

    void HandleJump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            currentNoiseLevel = 2f;
        }
    }

    void HandleMouseLook()
    {
        mouseInput = Mouse.current.delta.ReadValue() * mouseSensitivity * 0.02f;

        transform.Rotate(0, mouseInput.x, 0);

        verticalRotation -= mouseInput.y;
        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);

        if (cameraHolder != null)
        {
            cameraHolder.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
        }
    }

    void HandleCrouch()
    {
        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            isCrouched = !isCrouched;

            if (isCrouched)
            {
                targetCameraHeight = crouchCameraHeight;

                if (playerCollider != null)
                {
                    playerCollider.height = originalColliderHeight * 0.7f;
                    playerCollider.center = new Vector3(originalColliderCenter.x,
                                                       originalColliderCenter.y * 0.7f,
                                                       originalColliderCenter.z);
                }
            }
            else
            {
                targetCameraHeight = normalCameraHeight;

                if (playerCollider != null)
                {
                    playerCollider.height = originalColliderHeight;
                    playerCollider.center = originalColliderCenter;
                }
            }

            Debug.Log($"Crouched: {isCrouched}, Camera target: {targetCameraHeight}, Collider height: {playerCollider?.height}");
        }
    }

    // ✅ MODIFICADO: Ahora usa el nuevo sistema de linterna
    void HandleFlashlight()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (flashlightController != null)
            {
                // Usar el nuevo sistema
                flashlightController.ToggleFlashlight();
            }
            else
            {
                // Fallback al sistema anterior si no está disponible
                Debug.LogWarning("FlashlightFPSController no disponible, usando sistema básico");
                HandleFlashlightLegacy();
            }
        }
    }

    // ✅ Sistema de linterna anterior como fallback
    void HandleFlashlightLegacy()
    {
        Light flashlight = GetComponentInChildren<Light>();

        if (flashlight == null && playerCamera != null)
        {
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

        if (flashlight != null)
        {
            flashlight.enabled = !flashlight.enabled;
            Debug.Log("Linterna " + (flashlight.enabled ? "encendida" : "apagada"));
        }
    }

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

    // ✅ MÉTODOS COMENTADOS temporalmente para el debug
    /*
    public void SetFlashlightVisibility(bool visible)
    {
        if (flashlightController != null)
        {
            flashlightController.SetFlashlightVisibility(visible);
        }
    }

    public void SetFlashlightIntensity(float intensity)
    {
        if (flashlightController != null)
        {
            flashlightController.SetLightIntensity(intensity);
        }
    }
    */
}