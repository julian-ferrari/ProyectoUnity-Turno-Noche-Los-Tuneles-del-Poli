using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CaptureSystem : MonoBehaviour
{
    [Header("Referencias")]
    public Image blackScreen; // Panel negro para fade
    public AudioSource captureAudioSource;
    public AudioClip captureSound;

    [Header("Configuración")]
    public float fadeSpeed = 1f;
    public float blackScreenDuration = 2f;

    [Header("Spawn del Jugador")]
    public Transform playerSpawnPoint;

    [Header("Debug")]
    public bool debugMode = false;

    private PlayerController player;
    private bool isCapturing = false;

    private static CaptureSystem instance;

    // NUEVO: Sistema de spawn point personalizado desde guardado
    private static Vector3 customRespawnPoint = Vector3.zero;
    private static bool hasCustomRespawnPoint = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Importante para que no se destruya
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupUI();
        SetupAudio();
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();

        if (player == null)
        {
            Debug.LogError("¡No se encontró PlayerController!");
        }

        if (blackScreen != null)
        {
            Color c = blackScreen.color;
            c.a = 0f;
            blackScreen.color = c;
            blackScreen.gameObject.SetActive(false);

            if (debugMode)
                Debug.Log("BlackScreen inicializado correctamente");
        }
        else
        {
            Debug.LogError("¡BlackScreen es NULL después de Setup!");
        }
    }

    void SetupUI()
    {
        // Si no hay blackScreen asignado, crear uno
        if (blackScreen == null)
        {
            // SIEMPRE crear un nuevo Canvas dedicado para la captura
            GameObject canvasObj = new GameObject("CaptureCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767; // Máximo valor posible
            canvas.overrideSorting = true;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("Canvas de captura creado con sorting order: " + canvas.sortingOrder);

            // Crear panel negro
            GameObject blackScreenObj = new GameObject("BlackScreen");
            blackScreenObj.transform.SetParent(canvas.transform, false);

            blackScreen = blackScreenObj.AddComponent<Image>();
            blackScreen.color = new Color(0, 0, 0, 0);
            blackScreen.raycastTarget = false;
            blackScreen.material = null; // Asegurar que no tenga material custom

            RectTransform rt = blackScreen.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; // Margen izquierdo/inferior
            rt.offsetMax = Vector2.zero; // Margen derecho/superior
            rt.localScale = Vector3.one;
            rt.localPosition = Vector3.zero;

            // Mover al final de la jerarquía para estar encima
            blackScreenObj.transform.SetAsLastSibling();

            blackScreenObj.SetActive(false);

            Debug.Log($"BlackScreen creado: Pos={rt.position}, Size={rt.sizeDelta}, Anchors=[{rt.anchorMin},{rt.anchorMax}]");
        }
        else
        {
            Debug.Log("BlackScreen ya está asignado en el Inspector");

            // Verificar configuración del Canvas existente
            Canvas parentCanvas = blackScreen.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                Debug.Log($"Canvas existente: RenderMode={parentCanvas.renderMode}, SortingOrder={parentCanvas.sortingOrder}");

                // Forzar configuración correcta
                parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                parentCanvas.sortingOrder = 32767;
                parentCanvas.overrideSorting = true;
            }
        }
    }

    void SetupAudio()
    {
        if (captureAudioSource == null)
        {
            captureAudioSource = gameObject.AddComponent<AudioSource>();
            captureAudioSource.spatialBlend = 0f; // 2D sound
            captureAudioSource.playOnAwake = false;
            captureAudioSource.volume = 1f;

            Debug.Log("AudioSource de captura creado");
        }
    }

    public static void TriggerCapture()
    {
        if (instance == null)
        {
            Debug.LogError("¡CaptureSystem instance es NULL! ¿Está en la escena?");
            return;
        }

        if (instance.isCapturing)
        {
            Debug.LogWarning("Ya hay una captura en progreso, ignorando...");
            return;
        }

        Debug.Log("<color=magenta>=== TRIGGER CAPTURE LLAMADO ===</color>");
        instance.StartCoroutine(instance.CaptureSequence());
    }

    // NUEVO: Método para establecer el punto de respawn desde el sistema de guardado
    public static void SetRespawnPoint(Vector3 respawnPoint)
    {
        customRespawnPoint = respawnPoint;
        hasCustomRespawnPoint = true;
        Debug.Log($"<color=cyan>CaptureSystem: Respawn point personalizado establecido en {respawnPoint}</color>");
    }

    // NUEVO: Método para limpiar el respawn point personalizado
    public static void ClearCustomRespawnPoint()
    {
        hasCustomRespawnPoint = false;
        Debug.Log("CaptureSystem: Respawn point personalizado limpiado");
    }

    IEnumerator CaptureSequence()
    {
        isCapturing = true;

        Debug.Log("<color=red>=== INICIANDO SECUENCIA DE CAPTURA ===</color>");

        // Verificar que el jugador existe
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("¡No se pudo encontrar al jugador!");
                isCapturing = false;
                yield break;
            }
        }

        // Bloquear controles del jugador
        player.enabled = false;
        Debug.Log("Controles del jugador bloqueados");

        // Detener movimiento del jugador
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        // Reproducir sonido de captura
        if (captureSound != null && captureAudioSource != null)
        {
            captureAudioSource.PlayOneShot(captureSound);
            Debug.Log("Reproduciendo sonido de captura");
        }
        else
        {
            Debug.LogWarning("No hay sonido de captura asignado");
        }

        // Esperar un momento antes del fade
        yield return new WaitForSeconds(0.3f);

        // CRÍTICO: Verificar blackScreen antes de usarlo
        if (blackScreen == null)
        {
            Debug.LogError("¡BlackScreen es NULL! Intentando recuperar...");
            SetupUI(); // Intentar crear de nuevo

            if (blackScreen == null)
            {
                Debug.LogError("¡No se pudo crear BlackScreen! Abortando fade.");
                // Continuar sin fade
                yield return new WaitForSeconds(blackScreenDuration);
            }
        }

        // Fade a negro
        if (blackScreen != null)
        {
            Debug.Log("Iniciando fade a negro...");

            // CRÍTICO: Forzar configuración visual
            Canvas canvas = blackScreen.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 32767;
                canvas.enabled = true;
                Debug.Log($"Canvas forzado: enabled={canvas.enabled}, sortingOrder={canvas.sortingOrder}");
            }

            blackScreen.gameObject.SetActive(true);
            blackScreen.enabled = true;
            blackScreen.transform.SetAsLastSibling(); // Mover al frente

            // Verificar que realmente está activo
            Debug.Log($"BlackScreen estado: active={blackScreen.gameObject.activeSelf}, enabled={blackScreen.enabled}, color={blackScreen.color}");

            yield return StartCoroutine(FadeToBlack());
            Debug.Log("Fade a negro completado");
        }
        else
        {
            Debug.LogError("BlackScreen sigue siendo NULL después de setup!");
        }

        // Mantener pantalla negra
        Debug.Log($"Manteniendo pantalla negra por {blackScreenDuration} segundos...");
        yield return new WaitForSeconds(blackScreenDuration);

        // MODIFICADO: Teleportar jugador usando el sistema de prioridades
        Debug.Log("Teleportando jugador...");
        Vector3 respawnPosition = GetRespawnPosition();
        Quaternion respawnRotation = GetRespawnRotation();

        player.transform.position = respawnPosition;
        player.transform.rotation = respawnRotation;

        Debug.Log($"<color=yellow>Jugador teleportado a: {respawnPosition}</color>");

        // Resetear velocidad
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        // Resetear Character Controller si existe
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            cc.enabled = true;
        }

        // Resetear guardias
        GuardAI[] guards = FindObjectsByType<GuardAI>(FindObjectsSortMode.None);
        foreach (GuardAI guard in guards)
        {
            guard.ResetGuard();
        }
        Debug.Log($"Reseteados {guards.Length} guardias");

        // Fade desde negro
        if (blackScreen != null)
        {
            Debug.Log("Iniciando fade desde negro...");
            yield return StartCoroutine(FadeFromBlack());
            blackScreen.gameObject.SetActive(false);
            Debug.Log("Fade desde negro completado");
        }

        // Reactivar controles del jugador
        player.enabled = true;
        Debug.Log("Controles del jugador reactivados");

        // Asegurar que el cursor esté bloqueado
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isCapturing = false;

        Debug.Log("<color=green>=== SECUENCIA DE CAPTURA COMPLETADA ===</color>");
    }

    // NUEVO: Método para obtener la posición de respawn con sistema de prioridades
    private Vector3 GetRespawnPosition()
    {
        // Prioridad 1: Respawn point desde guardado (cuando se carga una partida)
        if (hasCustomRespawnPoint)
        {
            Debug.Log($"<color=cyan>Usando respawn point desde guardado: {customRespawnPoint}</color>");
            return customRespawnPoint;
        }

        // Prioridad 2: Spawn point del PlayerController
        if (player != null)
        {
            Vector3 spawnPoint = player.GetSpawnPoint();
            Debug.Log($"<color=green>Usando spawn point del PlayerController: {spawnPoint}</color>");
            return spawnPoint;
        }

        // Prioridad 3: playerSpawnPoint del CaptureSystem
        if (playerSpawnPoint != null)
        {
            Debug.Log($"<color=yellow>Usando playerSpawnPoint del CaptureSystem: {playerSpawnPoint.position}</color>");
            return playerSpawnPoint.position;
        }

        // Fallback: Posición por defecto
        Debug.LogWarning("No hay spawn point configurado, usando posición por defecto (0, 1, 0)");
        return new Vector3(0, 1, 0);
    }

    // NUEVO: Método para obtener la rotación de respawn
    private Quaternion GetRespawnRotation()
    {
        // Si hay playerSpawnPoint, usar su rotación
        if (playerSpawnPoint != null)
        {
            return playerSpawnPoint.rotation;
        }

        // Caso contrario, usar rotación identidad
        return Quaternion.identity;
    }

    IEnumerator FadeToBlack()
    {
        if (blackScreen == null)
        {
            Debug.LogError("¡blackScreen es NULL en FadeToBlack!");
            yield break;
        }

        float elapsed = 0f;
        Color c = blackScreen.color;
        c.a = 0f;
        blackScreen.color = c;

        Debug.Log($"Fade to black iniciado (duración: {fadeSpeed}s) - Color inicial: {c}");

        while (elapsed < fadeSpeed)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeSpeed);
            c.a = alpha;
            blackScreen.color = c;

            // Forzar actualización visual
            Canvas.ForceUpdateCanvases();

            if (debugMode && (int)(elapsed * 10) % 2 == 0)
            {
                Debug.Log($"Fade progress: {alpha * 100f:F0}% - Color actual: {blackScreen.color}");
            }

            yield return null;
        }

        c.a = 1f;
        blackScreen.color = c;
        Canvas.ForceUpdateCanvases();

        Debug.Log($"Pantalla completamente negra - Color final: {blackScreen.color}, Visible: {blackScreen.IsActive()}");
    }

    IEnumerator FadeFromBlack()
    {
        if (blackScreen == null)
        {
            Debug.LogError("¡blackScreen es NULL en FadeFromBlack!");
            yield break;
        }

        float elapsed = 0f;
        Color c = blackScreen.color;
        c.a = 1f;
        blackScreen.color = c;

        Debug.Log($"Fade from black iniciado (duración: {fadeSpeed}s)");

        while (elapsed < fadeSpeed)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeSpeed);
            c.a = alpha;
            blackScreen.color = c;

            if (debugMode && elapsed % 0.2f < Time.deltaTime)
            {
                Debug.Log($"Fade progress: {(1f - alpha) * 100f:F0}%");
            }

            yield return null;
        }

        c.a = 0f;
        blackScreen.color = c;
        Debug.Log("Pantalla completamente transparente (alpha = 0)");
    }

    public void SetSpawnPoint(Transform spawnPoint)
    {
        playerSpawnPoint = spawnPoint;
        Debug.Log($"Spawn point configurado: {spawnPoint.position}");
    }
}