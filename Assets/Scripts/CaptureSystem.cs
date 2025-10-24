using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CaptureSystem : MonoBehaviour
{
    [Header("Referencias")]
    public Image blackScreen;
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

    private static Vector3 customRespawnPoint = Vector3.zero;
    private static bool hasCustomRespawnPoint = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
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
        if (blackScreen == null)
        {
            GameObject canvasObj = new GameObject("CaptureCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            canvas.overrideSorting = true;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("Canvas de captura creado con sorting order: " + canvas.sortingOrder);

            GameObject blackScreenObj = new GameObject("BlackScreen");
            blackScreenObj.transform.SetParent(canvas.transform, false);

            blackScreen = blackScreenObj.AddComponent<Image>();
            blackScreen.color = new Color(0, 0, 0, 0);
            blackScreen.raycastTarget = false;
            blackScreen.material = null;

            RectTransform rt = blackScreen.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localPosition = Vector3.zero;

            blackScreenObj.transform.SetAsLastSibling();
            blackScreenObj.SetActive(false);

            Debug.Log($"BlackScreen creado: Pos={rt.position}, Size={rt.sizeDelta}");
        }
        else
        {
            Canvas parentCanvas = blackScreen.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
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
            captureAudioSource.spatialBlend = 0f;
            captureAudioSource.playOnAwake = false;
            captureAudioSource.volume = 1f;

            Debug.Log("AudioSource de captura creado");
        }
    }

    public static void TriggerCapture()
    {
        if (instance == null)
        {
            Debug.LogError("¡CaptureSystem instance es NULL!");
            return;
        }

        if (instance.isCapturing)
        {
            Debug.LogWarning("Ya hay una captura en progreso");
            return;
        }

        Debug.Log("<color=magenta>=== TRIGGER CAPTURE LLAMADO ===</color>");

        instance.StartCoroutine(instance.CaptureSequence());
    }

    public static void SetRespawnPoint(Vector3 respawnPoint)
    {
        customRespawnPoint = respawnPoint;
        hasCustomRespawnPoint = true;
        Debug.Log($"<color=cyan>Respawn point personalizado: {respawnPoint}</color>");
    }

    public static void ClearCustomRespawnPoint()
    {
        hasCustomRespawnPoint = false;
        Debug.Log("Respawn point personalizado limpiado");
    }

    IEnumerator CaptureSequence()
    {
        isCapturing = true;

        Debug.Log("<color=red>=== INICIANDO SECUENCIA DE CAPTURA ===</color>");

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

        // Bloquear controles
        player.enabled = false;
        Debug.Log("Controles bloqueados");

        // Detener movimiento
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        // Sonido de captura
        if (captureSound != null && captureAudioSource != null)
        {
            captureAudioSource.PlayOneShot(captureSound);
            Debug.Log("Sonido de captura");
        }

        yield return new WaitForSeconds(0.3f);

        // Fade a negro
        if (blackScreen != null)
        {
            Debug.Log("Fade a negro...");

            Canvas canvas = blackScreen.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 32767;
                canvas.enabled = true;
            }

            blackScreen.gameObject.SetActive(true);
            blackScreen.enabled = true;
            blackScreen.transform.SetAsLastSibling();

            yield return StartCoroutine(FadeToBlack());
        }

        // Pantalla negra
        yield return new WaitForSeconds(blackScreenDuration);

        // Teleportar jugador
        Debug.Log("Teleportando jugador...");
        Vector3 respawnPosition = GetRespawnPosition();
        Quaternion respawnRotation = GetRespawnRotation();

        player.transform.position = respawnPosition;
        player.transform.rotation = respawnRotation;

        Debug.Log($"<color=yellow>Jugador en: {respawnPosition}</color>");

        // Resetear física
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

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
            Debug.Log("Fade desde negro...");
            yield return StartCoroutine(FadeFromBlack());
            blackScreen.gameObject.SetActive(false);
        }

        // Reactivar controles
        player.enabled = true;
        Debug.Log("Controles reactivados");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isCapturing = false;

        Debug.Log("<color=green>=== CAPTURA COMPLETADA ===</color>");

        // MODIFICADO: Llamar al NightSystem DESPUÉS de la secuencia de captura
        NightSystem.OnPlayerCaptured();
    }

    private Vector3 GetRespawnPosition()
    {
        if (hasCustomRespawnPoint)
        {
            Debug.Log($"<color=cyan>Usando respawn desde guardado</color>");
            return customRespawnPoint;
        }

        if (player != null)
        {
            Vector3 spawnPoint = player.GetSpawnPoint();
            Debug.Log($"<color=green>Usando spawn del PlayerController</color>");
            return spawnPoint;
        }

        if (playerSpawnPoint != null)
        {
            Debug.Log($"<color=yellow>Usando playerSpawnPoint del CaptureSystem</color>");
            return playerSpawnPoint.position;
        }

        Debug.LogWarning("Usando posición por defecto");
        return new Vector3(0, 1, 0);
    }

    private Quaternion GetRespawnRotation()
    {
        if (playerSpawnPoint != null)
        {
            return playerSpawnPoint.rotation;
        }
        return Quaternion.identity;
    }

    IEnumerator FadeToBlack()
    {
        if (blackScreen == null)
        {
            Debug.LogError("¡blackScreen es NULL!");
            yield break;
        }

        float elapsed = 0f;
        Color c = blackScreen.color;
        c.a = 0f;
        blackScreen.color = c;

        while (elapsed < fadeSpeed)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeSpeed);
            c.a = alpha;
            blackScreen.color = c;

            Canvas.ForceUpdateCanvases();

            if (debugMode && (int)(elapsed * 10) % 2 == 0)
            {
                Debug.Log($"Fade: {alpha * 100f:F0}%");
            }

            yield return null;
        }

        c.a = 1f;
        blackScreen.color = c;
        Canvas.ForceUpdateCanvases();
    }

    IEnumerator FadeFromBlack()
    {
        if (blackScreen == null)
        {
            Debug.LogError("¡blackScreen es NULL!");
            yield break;
        }

        float elapsed = 0f;
        Color c = blackScreen.color;
        c.a = 1f;
        blackScreen.color = c;

        while (elapsed < fadeSpeed)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeSpeed);
            c.a = alpha;
            blackScreen.color = c;

            if (debugMode && elapsed % 0.2f < Time.deltaTime)
            {
                Debug.Log($"Fade: {(1f - alpha) * 100f:F0}%");
            }

            yield return null;
        }

        c.a = 0f;
        blackScreen.color = c;
    }

    public void SetSpawnPoint(Transform spawnPoint)
    {
        playerSpawnPoint = spawnPoint;
        Debug.Log($"Spawn point configurado: {spawnPoint.position}");
    }
}