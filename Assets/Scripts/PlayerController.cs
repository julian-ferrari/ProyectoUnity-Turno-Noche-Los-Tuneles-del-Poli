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

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource breathingAudioSource; // AudioSource separado para respiración
    [Space(5)]
    public AudioClip[] footstepSounds; // Múltiples sonidos de pasos para variedad
    public AudioClip breathingSound; // Respiración acelerada
    [Space(5)]
    public float footstepInterval = 0.5f; // Intervalo entre pasos al caminar
    public float runFootstepInterval = 0.3f; // Intervalo entre pasos al correr
    public float crouchFootstepInterval = 0.7f; // Intervalo entre pasos agachado
    [Space(5)]
    public float footstepVolume = 0.5f;
    public float breathingVolume = 0.3f;

    // REFERENCIA AL MENÚ DE PAUSA
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

    // Audio variables
    private float footstepTimer = 0f;
    private bool isMoving = false;
    private bool wasRunning = false;

    void Start()
    {
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

        // Rigidbody setup
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
        SetupAudio();

        // Cursor setup
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize camera height
        targetCameraHeight = normalCameraHeight;
        currentCameraHeight = normalCameraHeight;
    }

    void SetupAudio()
    {
        // Configurar AudioSource principal si no existe
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 15f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        // Configurar AudioSource para respiración (separado para que no interfiera con pasos)
        if (breathingAudioSource == null)
        {
            GameObject breathingObj = new GameObject("BreathingAudioSource");
            breathingObj.transform.SetParent(transform);
            breathingObj.transform.localPosition = Vector3.zero;

            breathingAudioSource = breathingObj.AddComponent<AudioSource>();
            breathingAudioSource.spatialBlend = 0f; // 2D sound (suena como si viniera del jugador)
            breathingAudioSource.loop = true;
            breathingAudioSource.volume = breathingVolume;
            breathingAudioSource.playOnAwake = false;
        }

        // Asignar clip de respiración
        if (breathingSound != null)
        {
            breathingAudioSource.clip = breathingSound;
        }

        Debug.Log("Sistema de audio del jugador configurado");
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
            flashlight = playerCamera.transform.Find("LuzLinterna")?.GetComponent<Light>();

            if (flashlight == null)
            {
                flashlight = playerCamera.GetComponentInChildren<Light>();
            }

            if (flashlight != null)
            {
                flashlight.enabled = false;
            }
        }
    }

    void Update()
    {
        if (pauseMenu != null && pauseMenu.IsPaused)
        {
            return;
        }

        HandleInput();
        HandleMovement();
        HandleMouseLook();
        UpdateCameraHeight();
        CheckGrounded();
        UpdateInteractionMessage();
        UpdateAudio(); // Sistema de audio
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouched)
        {
            HandleJump();
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            HandleCrouch();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlight();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            HandleInteraction();
        }
    }

    void HandleMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.W)) vertical = 1f;
        if (Input.GetKey(KeyCode.S)) vertical = -1f;
        if (Input.GetKey(KeyCode.A)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D)) horizontal = 1f;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 movement = (forward * vertical + right * horizontal).normalized;

        // Determinar si está corriendo
        bool isRunning = false;

        if (movement.magnitude > 0)
        {
            isMoving = true;

            if (Input.GetKey(KeyCode.LeftShift) && !isCrouched)
            {
                currentSpeed = runSpeed;
                currentNoiseLevel = 3f;
                isRunning = true;
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
            isMoving = false;
            currentNoiseLevel = 0f;
        }

        // Controlar respiración al correr
        HandleBreathing(isRunning);

        Vector3 moveVelocity = movement * currentSpeed;
        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0, mouseX, 0);

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
        }
        else
        {
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            isGrounded = Physics.Raycast(rayStart, Vector3.down, 1.2f, groundMask);
        }
    }

    void HandleJump()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        currentNoiseLevel = 2f;
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
    }

    void ToggleFlashlight()
    {
        if (flashlight != null)
        {
            flashlight.enabled = !flashlight.enabled;

            foreach (Transform child in playerCamera.transform)
            {
                if (child.GetComponent<Light>() == null)
                {
                    child.gameObject.SetActive(true);

                    Renderer rend = child.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.enabled = true;
                    }

                    MeshRenderer mesh = child.GetComponent<MeshRenderer>();
                    if (mesh != null)
                    {
                        mesh.enabled = true;
                    }
                }
            }
        }
        else
        {
            SetupFlashlight();
        }
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

    // ===================== SISTEMA DE AUDIO =====================

    void UpdateAudio()
    {
        // Solo reproducir pasos si está en el suelo y moviéndose
        if (isGrounded && isMoving)
        {
            PlayFootsteps();
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    void PlayFootsteps()
    {
        if (footstepSounds == null || footstepSounds.Length == 0)
        {
            Debug.LogWarning("No hay sonidos de pasos asignados!");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("AudioSource es null en PlayFootsteps!");
            return;
        }

        footstepTimer += Time.deltaTime;

        // Determinar intervalo según el tipo de movimiento
        float currentInterval = footstepInterval;
        if (Input.GetKey(KeyCode.LeftShift) && !isCrouched)
        {
            currentInterval = runFootstepInterval;
        }
        else if (isCrouched)
        {
            currentInterval = crouchFootstepInterval;
        }

        // Reproducir sonido de paso
        if (footstepTimer >= currentInterval)
        {
            footstepTimer = 0f;

            // Elegir un sonido aleatorio de pasos
            AudioClip footstep = footstepSounds[Random.Range(0, footstepSounds.Length)];

            if (footstep != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f); // Variar tono ligeramente
                audioSource.PlayOneShot(footstep, footstepVolume);
                Debug.Log($"Reproduciendo paso: {footstep.name}, Volumen: {footstepVolume}");
            }
            else
            {
                Debug.LogWarning("AudioClip de footstep es null!");
            }
        }
    }

    void HandleBreathing(bool isRunning)
    {
        if (breathingAudioSource == null || breathingSound == null) return;

        // Activar respiración acelerada al correr
        if (isRunning && !wasRunning)
        {
            // Empezar a respirar aceleradamente
            breathingAudioSource.Play();
            wasRunning = true;
        }
        else if (!isRunning && wasRunning)
        {
            // Detener respiración con fade out suave
            StartCoroutine(FadeOutBreathing());
            wasRunning = false;
        }

        // Ajustar volumen según velocidad
        if (isRunning && breathingAudioSource.isPlaying)
        {
            breathingAudioSource.volume = Mathf.Lerp(breathingAudioSource.volume, breathingVolume, Time.deltaTime * 2f);
        }
    }

    System.Collections.IEnumerator FadeOutBreathing()
    {
        float startVolume = breathingAudioSource.volume;
        float fadeTime = 1f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            breathingAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
            yield return null;
        }

        breathingAudioSource.Stop();
        breathingAudioSource.volume = breathingVolume;
    }

    // ===================== FIN SISTEMA DE AUDIO =====================

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && (pauseMenu == null || !pauseMenu.IsPaused))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void PickupKey(KeyItem keyItem)
    {
        InventoryManager inventory = FindFirstObjectByType<InventoryManager>();

        if (inventory != null)
        {
            inventory.AddKey(keyItem);
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

    public void OnGamePaused()
    {
        Debug.Log("PlayerController: Juego pausado");
    }

    public void OnGameResumed()
    {
        if (Application.isFocused)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        Debug.Log("PlayerController: Juego reanudado");
    }
}