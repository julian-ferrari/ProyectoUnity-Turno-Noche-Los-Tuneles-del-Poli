using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Sistema que gestiona las noches del juego (1-5)
/// Muestra imágenes de transición y maneja el game over
/// </summary>
public class NightSystem : MonoBehaviour
{
    public static NightSystem Instance { get; private set; }

    [Header("Night Images")]
    [Tooltip("Sprites de las 5 noches (noche1.png a noche5.png)")]
    public Sprite[] nightSprites = new Sprite[5];

    [Tooltip("Sprite de game over (cuando pierdes en noche 5)")]
    public Sprite gameOverSprite;

    [Header("Timing")]
    [Tooltip("Duración del fade in de la imagen")]
    public float fadeInDuration = 1.5f;

    [Tooltip("Tiempo que la imagen permanece visible")]
    public float displayDuration = 3f;

    [Tooltip("Duración del fade out")]
    public float fadeOutDuration = 1.5f;

    [Header("Audio (Opcional)")]
    public AudioClip nightStartSound;
    public AudioClip gameOverSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    [Header("Scene References")]
    public string gameSceneName = "PoliNights";
    public string mainMenuSceneName = "MainMenu";

    // Estado actual
    private int currentNight = 1;
    private Canvas nightCanvas;
    private Image nightImage;
    private Image fadeImage;
    private AudioSource audioSource;
    private bool isShowingNight = false;

    // Flag estático para saber si debemos mostrar la imagen de noche al cargar
    private static bool shouldShowNightImage = false;
    private static int nightToShow = 1;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupUI();
        SetupAudio();

        // Suscribirse al evento de carga de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void SetupUI()
    {
        // Crear Canvas para las imágenes de noche
        GameObject canvasObj = new GameObject("NightSystemCanvas");
        canvasObj.transform.SetParent(transform);

        nightCanvas = canvasObj.AddComponent<Canvas>();
        nightCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        nightCanvas.sortingOrder = 1000; // Por encima de la UI normal pero debajo del capture system

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Crear imagen de fade (fondo negro para transiciones)
        GameObject fadeObj = new GameObject("FadeImage");
        fadeObj.transform.SetParent(nightCanvas.transform, false);

        fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.raycastTarget = true; // Bloquear clicks durante transiciones

        RectTransform fadeRect = fadeImage.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;

        // Crear imagen de noche
        GameObject imageObj = new GameObject("NightImage");
        imageObj.transform.SetParent(nightCanvas.transform, false);

        nightImage = imageObj.AddComponent<Image>();
        nightImage.color = new Color(1, 1, 1, 0);
        nightImage.preserveAspect = false;
        nightImage.raycastTarget = false;

        RectTransform imageRect = nightImage.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        // Ocultar todo al inicio
        nightCanvas.gameObject.SetActive(false);

        Debug.Log("NightSystem UI creada - Imagen configurada para pantalla completa");
    }

    void SetupAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"NightSystem: Escena cargada - {scene.name}");

        // Si estamos en la escena del juego y debemos mostrar la imagen de noche
        if (scene.name == gameSceneName && shouldShowNightImage)
        {
            shouldShowNightImage = false;
            StartCoroutine(ShowNightImageSequence(nightToShow));
        }

        // Si cargamos el menú principal, asegurar que todo esté limpio
        if (scene.name == mainMenuSceneName)
        {
            Debug.Log("NightSystem: Limpiando UI para menú principal");
            CleanupUI();
        }
    }

    /// <summary>
    /// Limpia la UI del NightSystem (oculta todo)
    /// </summary>
    void CleanupUI()
    {
        if (nightCanvas != null)
        {
            nightCanvas.gameObject.SetActive(false);
        }

        if (nightImage != null)
        {
            Color c = nightImage.color;
            c.a = 0;
            nightImage.color = c;
        }

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0;
            fadeImage.color = c;
        }

        isShowingNight = false;
        Debug.Log("NightSystem UI limpiada");
    }

    /// <summary>
    /// Inicia una nueva partida desde la noche 1
    /// </summary>
    public static void StartNewGame()
    {
        if (Instance == null)
        {
            Debug.LogError("NightSystem Instance no existe. Asegúrate de que esté en la escena MainMenu.");
            return;
        }

        Debug.Log("=== INICIANDO NUEVA PARTIDA - NOCHE 1 ===");

        // Resetear el sistema de guardado
        SaveSystem.DeleteSave();

        // Establecer noche inicial
        Instance.currentNight = 1;
        shouldShowNightImage = true;
        nightToShow = 1;

        // Cargar escena del juego
        SceneManager.LoadScene(Instance.gameSceneName);
    }

    /// <summary>
    /// Llamado cuando el guardia captura al jugador
    /// DEBE ser llamado DESPUÉS de la secuencia de captura
    /// </summary>
    public static void OnPlayerCaptured()
    {
        if (Instance == null)
        {
            Debug.LogError("NightSystem Instance no existe");
            return;
        }

        Instance.HandleCapture();
    }

    void HandleCapture()
    {
        if (isShowingNight)
        {
            Debug.LogWarning("Ya se está procesando una transición de noche");
            return;
        }

        Debug.Log($"<color=red>=== JUGADOR CAPTURADO EN NOCHE {currentNight} ===</color>");

        // Incrementar noche
        currentNight++;

        if (currentNight > 5)
        {
            // Game Over
            Debug.Log("<color=red>=== GAME OVER - Perdiste en la noche 5 ===</color>");
            StartCoroutine(GameOverSequence());
        }
        else
        {
            // Continuar a la siguiente noche
            Debug.Log($"<color=yellow>=== AVANZANDO A NOCHE {currentNight} ===</color>");
            StartCoroutine(ShowNightImageSequence(currentNight));
        }
    }

    IEnumerator ShowNightImageSequence(int nightNumber)
    {
        isShowingNight = true;

        Debug.Log($"Mostrando imagen de noche {nightNumber}");

        // Asegurar que el índice es válido
        int spriteIndex = nightNumber - 1;
        if (spriteIndex < 0 || spriteIndex >= nightSprites.Length || nightSprites[spriteIndex] == null)
        {
            Debug.LogError($"Sprite de noche {nightNumber} no asignado");
            isShowingNight = false;
            yield break;
        }

        // Configurar la imagen
        nightImage.sprite = nightSprites[spriteIndex];
        nightCanvas.gameObject.SetActive(true);

        // Reproducir sonido
        if (nightStartSound != null)
        {
            audioSource.PlayOneShot(nightStartSound);
        }

        // Fade in
        yield return StartCoroutine(FadeImage(nightImage, 0f, 1f, fadeInDuration));

        // Mantener visible
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        yield return StartCoroutine(FadeImage(nightImage, 1f, 0f, fadeOutDuration));

        // Ocultar canvas
        nightCanvas.gameObject.SetActive(false);

        isShowingNight = false;

        Debug.Log($"Noche {nightNumber} iniciada");
    }

    IEnumerator GameOverSequence()
    {
        isShowingNight = true;

        Debug.Log("=== INICIANDO SECUENCIA DE GAME OVER ===");

        // Esperar un momento
        yield return new WaitForSeconds(0.5f);

        // Configurar imagen de game over
        if (gameOverSprite != null)
        {
            nightImage.sprite = gameOverSprite;
        }
        else
        {
            Debug.LogWarning("No hay sprite de Game Over asignado");
        }

        nightCanvas.gameObject.SetActive(true);

        // Reproducir sonido de game over
        if (gameOverSound != null)
        {
            audioSource.PlayOneShot(gameOverSound);
        }

        // Fade in de la imagen de game over
        yield return StartCoroutine(FadeImage(nightImage, 0f, 1f, fadeInDuration));

        // Mantener visible más tiempo para game over
        yield return new WaitForSeconds(displayDuration + 2f);

        // Fade out de la imagen de game over
        yield return StartCoroutine(FadeImage(nightImage, 1f, 0f, fadeOutDuration));

        // Fade a negro completo para transición
        yield return StartCoroutine(FadeImage(fadeImage, 0f, 1f, fadeOutDuration));

        // Resetear sistema
        currentNight = 1;
        SaveSystem.DeleteSave();

        Debug.Log("Cargando menú principal...");

        // Cargar escena de forma asíncrona
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainMenuSceneName);

        // Esperar a que la escena esté lista
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log("Menú principal cargado, haciendo fade desde negro");

        // Esperar un frame para asegurar que la escena esté completamente inicializada
        yield return new WaitForEndOfFrame();

        // Fade desde negro en el menú principal
        yield return StartCoroutine(FadeImage(fadeImage, 1f, 0f, 1f));

        // Limpiar y ocultar todo
        CleanupUI();

        Debug.Log("=== GAME OVER COMPLETO - MENÚ PRINCIPAL VISIBLE ===");
    }

    IEnumerator FadeImage(Image image, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = image.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Usar unscaled para que funcione aunque el juego esté pausado
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            color.a = alpha;
            image.color = color;
            yield return null;
        }

        color.a = endAlpha;
        image.color = color;
    }

    // Getters públicos
    public int GetCurrentNight() => currentNight;

    public bool IsShowingNight() => isShowingNight;

    // Método para guardar/cargar el número de noche
    public void SaveNightNumber()
    {
        PlayerPrefs.SetInt("CurrentNight", currentNight);
        PlayerPrefs.Save();
    }

    public void LoadNightNumber()
    {
        currentNight = PlayerPrefs.GetInt("CurrentNight", 1);
        Debug.Log($"Noche cargada desde PlayerPrefs: {currentNight}");
    }

    // Para testing en el editor
    [ContextMenu("Test - Show Night 1")]
    void TestNight1() => StartCoroutine(ShowNightImageSequence(1));

    [ContextMenu("Test - Show Night 2")]
    void TestNight2() => StartCoroutine(ShowNightImageSequence(2));

    [ContextMenu("Test - Show Night 3")]
    void TestNight3() => StartCoroutine(ShowNightImageSequence(3));

    [ContextMenu("Test - Show Night 4")]
    void TestNight4() => StartCoroutine(ShowNightImageSequence(4));

    [ContextMenu("Test - Show Night 5")]
    void TestNight5() => StartCoroutine(ShowNightImageSequence(5));

    [ContextMenu("Test - Game Over")]
    void TestGameOver() => StartCoroutine(GameOverSequence());

    [ContextMenu("Reset Night to 1")]
    void ResetNight()
    {
        currentNight = 1;
        PlayerPrefs.SetInt("CurrentNight", 1);
        PlayerPrefs.Save();
        Debug.Log("Noche reseteada a 1");
    }

    [ContextMenu("Force Cleanup UI")]
    void ForceCleanup()
    {
        CleanupUI();
    }
}