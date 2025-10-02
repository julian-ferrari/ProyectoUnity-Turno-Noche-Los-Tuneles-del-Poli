using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float jumpForce = 8f;

    [Header("Camera")]
    public Camera playerCamera;
    public float mouseSensitivity = 2f;
    public float upDownRange = 60f;
    public float normalCameraHeight = 0.8f;
    public float crouchCameraHeight = 0.6f;

    [Header("Ground Check")]
    public float groundDistance = 0.2f;
    public LayerMask groundMask = 1;
    public Transform groundCheckPoint;

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

    [Header("Flashlight")]
    public Light flashlight;

    // REFERENCIA AL MENÚ DE PAUSA - NUEVA
    private PoliNightsPauseMenu pauseMenu;

    // Components
    private Rigidbody rb;
    private CapsuleCollider playerCollider;

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

    void Start()
    {
        // ENCONTRAR REFERENCIA AL MENÚ DE PAUSA - NUEVO
        pauseMenu = FindFirstObjectByType<PoliNightsPauseMenu>();
        if (pauseMenu == null)
        {
            Debug.LogWarning("No se encontró PoliNightsPauseMenu en la escena!");
        }

        // Get components
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        currentSpeed = walkSpeed;

        // Guardar configuración original del collider
        if (playerCollider != null)
        {
            originalColliderHeight = playerCollider.height;
            originalColliderCenter = playerCollider.center;
        }

        // Rigidbody setup MEJORADO
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                            RigidbodyConstraints.FreezeRotationY |
                            RigidbodyConstraints.FreezeRotationZ;
            rb.mass = 1f;
        }

        // Camera setup
        SetupCamera();
        SetupFlashlight();
        SetupGroundCheck();

        // Cursor setup
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize camera height
        targetCameraHeight = normalCameraHeight;
        currentCameraHeight = normalCameraHeight;
    }

    void SetupGroundCheck()
    {
        if (groundCheckPoint == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheckPoint");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -playerCollider.height / 2f + 0.1f, 0);
            groundCheckPoint = groundCheckObj.transform;
        }
    }

    void SetupCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        if (playerCamera != null)
        {
            GameObject holderObj = new GameObject("CameraHolder");
            holderObj.transform.SetParent(transform);
            holderObj.transform.localPosition = new Vector3(0, normalCameraHeight, 0);
            holderObj.transform.localRotation = Quaternion.identity;
            cameraHolder = holderObj.transform;

            playerCamera.transform.SetParent(cameraHolder);
            playerCamera.transform.localPosition = Vector3.zero;
            playerCamera.transform.localRotation = Quaternion.identity;
        }
    }

    void SetupFlashlight()
    {
        if (playerCamera != null)
        {
            Debug.Log("=== SETUP LINTERNA ===");
            Debug.Log("Buscando objetos hijos de la cámara:");

            for (int i = 0; i < playerCamera.transform.childCount; i++)
            {
                Transform child = playerCamera.transform.GetChild(i);
                Debug.Log($"  Hijo {i}: '{child.name}' - Activo: {child.gameObject.activeSelf}");

                Component[] components = child.GetComponents<Component>();
                foreach (Component comp in components)
                {
                    Debug.Log($"    - Componente: {comp.GetType().Name}");
                }
            }

            flashlight = playerCamera.transform.Find("LuzLinterna")?.GetComponent<Light>();

            if (flashlight == null)
            {
                Debug.LogError("❌ No encontró 'LuzLinterna'! Buscando cualquier Light...");
                flashlight = playerCamera.GetComponentInChildren<Light>();

                if (flashlight != null)
                {
                    Debug.Log($"✅ Encontró Light en: '{flashlight.name}'");
                }
                else
                {
                    Debug.LogError("❌ No se encontró NINGUNA Light!");
                }
            }
            else
            {
                Debug.Log($"✅ Encontró LuzLinterna correctamente: {flashlight.name}");
            }

            if (flashlight != null)
            {
                flashlight.enabled = false;
                Debug.Log("🔦 Linterna configurada y apagada inicialmente");
            }

            Debug.Log("=== FIN SETUP LINTERNA ===");
        }
    }

    void Update()
    {
        // VERIFICAR SI EL JUEGO ESTÁ PAUSADO - NUEVO
        if (pauseMenu != null && pauseMenu.IsPaused)
        {
            return; // No procesar input del jugador si está pausado
        }

        HandleInput();
        HandleMovement();
        HandleMouseLook();
        UpdateCameraHeight();
        CheckGrounded();
        UpdateInteractionMessage();
    }

    void HandleInput()
    {
        // Jump - MEJORADO
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouched)
        {
            HandleJump();
        }

        // Crouch
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            HandleCrouch();
        }

        // Flashlight
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlight();
        }

        // Interaction
        if (Input.GetKeyDown(KeyCode.E))
        {
            HandleInteraction();
        }

        // NOTA: NO manejamos ESC aquí porque lo maneja PoliNightsPauseMenu
    }

    void HandleMovement()
    {
        // Obtener input DIRECTAMENTE
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.W)) vertical = 1f;
        if (Input.GetKey(KeyCode.S)) vertical = -1f;
        if (Input.GetKey(KeyCode.A)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D)) horizontal = 1f;

        // Calcular dirección de movimiento
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 movement = (forward * vertical + right * horizontal).normalized;

        // Determinar velocidad
        if (movement.magnitude > 0)
        {
            if (Input.GetKey(KeyCode.LeftShift) && !isCrouched)
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

        // Aplicar movimiento SIMPLE Y DIRECTO
        Vector3 moveVelocity = movement * currentSpeed;
        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
    }

    void HandleMouseLook()
    {
        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotación horizontal del jugador
        transform.Rotate(0, mouseX, 0);

        // Rotación vertical de la cámara
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);

        if (cameraHolder != null)
        {
            cameraHolder.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
        }
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
        if (groundCheckPoint != null)
        {
            isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundDistance, groundMask);
            Debug.DrawLine(groundCheckPoint.position, groundCheckPoint.position + Vector3.down * groundDistance,
                          isGrounded ? Color.green : Color.red);
        }
        else
        {
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            isGrounded = Physics.Raycast(rayStart, Vector3.down, 1.2f, groundMask);
            Debug.DrawRay(rayStart, Vector3.down * 1.2f, isGrounded ? Color.green : Color.red);
        }
    }

    void HandleJump()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        currentNoiseLevel = 2f;
        Debug.Log($"¡SALTANDO! Fuerza aplicada: {jumpForce}, isGrounded: {isGrounded}");
    }

    void HandleCrouch()
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

        Debug.Log("Crouch: " + isCrouched);
    }

    void ToggleFlashlight()
    {
        Debug.Log("=== TOGGLE LINTERNA ===");

        if (flashlight != null)
        {
            flashlight.enabled = !flashlight.enabled;
            Debug.Log($"🔦 Linterna {(flashlight.enabled ? "ENCENDIDA" : "APAGADA")}");

            Debug.Log("Objetos en la cámara después del toggle:");
            for (int i = 0; i < playerCamera.transform.childCount; i++)
            {
                Transform child = playerCamera.transform.GetChild(i);
                bool hasRenderer = child.GetComponent<Renderer>() != null;
                bool hasLight = child.GetComponent<Light>() != null;

                Debug.Log($"  {child.name}: Activo={child.gameObject.activeSelf}, Renderer={hasRenderer}, Light={hasLight}");

                if (hasRenderer)
                {
                    Renderer rend = child.GetComponent<Renderer>();
                    Debug.Log($"    Renderer enabled: {rend.enabled}");
                }
            }

            foreach (Transform child in playerCamera.transform)
            {
                if (child.GetComponent<Light>() == null)
                {
                    child.gameObject.SetActive(true);

                    Renderer rend = child.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.enabled = true;
                        Debug.Log($"✅ Forzado visible: {child.name}");
                    }

                    MeshRenderer mesh = child.GetComponent<MeshRenderer>();
                    if (mesh != null)
                    {
                        mesh.enabled = true;
                        Debug.Log($"✅ MeshRenderer enabled: {child.name}");
                    }
                }
            }
        }
        else
        {
            Debug.LogError("❌ flashlight es NULL!");
            Debug.Log("Intentando encontrar la luz nuevamente...");
            SetupFlashlight();
        }

        Debug.Log("=== FIN TOGGLE LINTERNA ===");
    }

    Transform FindFlashlightModel(Transform parent)
    {
        string[] possibleNames = { "modelo de la linterna", "linterna", "Linterna", "FlashlightModel", "Flashlight_Model" };

        foreach (string name in possibleNames)
        {
            Transform found = parent.Find(name);
            if (found != null && found != flashlight.transform)
            {
                return found;
            }
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.GetComponent<Light>() == null)
            {
                return child;
            }
        }

        return null;
    }

    void HandleInteraction()
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

    void OnApplicationFocus(bool hasFocus)
    {
        // SOLO configurar cursor si NO está pausado - MODIFICADO
        if (hasFocus && (pauseMenu == null || !pauseMenu.IsPaused))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Métodos públicos para inventario y mensajes
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
        // Opcional: remover la llave después de usar
    }

    public void ShowInteractionMessage(string message)
    {
        currentInteractionMessage = message;
        showingMessage = true;
        messageTimer = messageDuration;
        Debug.Log("Mensaje: " + message);
    }

    public void HideInteractionMessage()
    {
        currentInteractionMessage = "";
        showingMessage = false;
        messageTimer = 0f;
    }

    // MÉTODO PÚBLICO PARA EL MENÚ DE PAUSA - NUEVO
    public void OnGamePaused()
    {
        // Método llamado cuando el juego se pausa
        // Aquí puedes agregar lógica específica del jugador al pausar
        Debug.Log("PlayerController: Juego pausado");
    }

    // MÉTODO PÚBLICO PARA EL MENÚ DE PAUSA - NUEVO
    public void OnGameResumed()
    {
        // Método llamado cuando el juego se reanuda
        // Restaurar configuración del cursor si es necesario
        if (Application.isFocused)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        Debug.Log("PlayerController: Juego reanudado");
    }
}