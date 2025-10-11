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

        Debug.Log("Teleportando jugador al inicio");

        // Teleportar jugador al inicio
        if (playerSpawnPoint != null)
        {
            player.transform.position = playerSpawnPoint.position;
            player.transform.rotation = playerSpawnPoint.rotation;
            Debug.Log($"Jugador teleportado a: {playerSpawnPoint.position}");
        }
        else
        {
            // Si no hay spawn point, usar posición por defecto
            player.transform.position = new Vector3(0, 1, 0);
            player.transform.rotation = Quaternion.identity;
            Debug.LogWarning("No hay spawn point asignado, usando posición por defecto (0, 1, 0)");
        }

        // Resetear velocidad
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
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