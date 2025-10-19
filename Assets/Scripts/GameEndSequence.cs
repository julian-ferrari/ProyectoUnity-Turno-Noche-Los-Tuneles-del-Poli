using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Maneja la secuencia de final del juego: fade out, logo, y vuelta al menú
/// </summary>
public class GameEndSequence : MonoBehaviour
{
    [Header("Logo Settings")]
    [Tooltip("Logo del juego que aparecerá")]
    public Sprite gameLogo;

    [Tooltip("Tamaño del logo en pantalla")]
    public Vector2 logoSize = new Vector2(600, 400);

    [Header("Timing")]
    [Tooltip("Duración del fade a negro (segundos)")]
    public float fadeOutDuration = 2f;

    [Tooltip("Tiempo que permanece en negro antes de mostrar logo")]
    public float blackScreenDelay = 1f;

    [Tooltip("Duración del fade in del logo")]
    public float logoFadeInDuration = 1.5f;

    [Tooltip("Tiempo que el logo permanece visible")]
    public float logoDisplayTime = 5f;

    [Tooltip("Duración del fade out final")]
    public float finalFadeOutDuration = 1.5f;

    [Header("Audio")]
    [Tooltip("Música de créditos/final (opcional)")]
    public AudioClip endGameMusic;

    [Tooltip("Volumen de la música")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    [Header("Scene")]
    [Tooltip("Nombre de la escena del menú principal")]
    public string mainMenuSceneName = "MainMenu";

    // Referencias UI
    private Canvas endCanvas;
    private Image fadeImage;
    private Image logoImage;
    private AudioSource musicSource;

    private bool isEnding = false;

    void Awake()
    {
        SetupUI();
        SetupAudio();
    }

    void Start()
    {
        // Asegurar que todo esté oculto al inicio
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }

        if (logoImage != null)
        {
            Color c = logoImage.color;
            c.a = 0f;
            logoImage.color = c;
            logoImage.gameObject.SetActive(false);
        }
    }

    void SetupUI()
    {
        // Crear Canvas
        GameObject canvasObj = new GameObject("EndGameCanvas");
        canvasObj.transform.SetParent(transform);
        DontDestroyOnLoad(canvasObj);

        endCanvas = canvasObj.AddComponent<Canvas>();
        endCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        endCanvas.sortingOrder = 200; // Muy alto para estar encima de todo

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Crear imagen de fade (fondo negro)
        GameObject fadeObj = new GameObject("FadeImage");
        fadeObj.transform.SetParent(endCanvas.transform, false);

        fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.raycastTarget = false;

        RectTransform fadeRect = fadeImage.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;

        // Crear imagen del logo
        GameObject logoObj = new GameObject("GameLogo");
        logoObj.transform.SetParent(endCanvas.transform, false);

        logoImage = logoObj.AddComponent<Image>();
        logoImage.sprite = gameLogo;
        logoImage.color = new Color(1, 1, 1, 0);
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;

        RectTransform logoRect = logoImage.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0.5f);
        logoRect.anchorMax = new Vector2(0.5f, 0.5f);
        logoRect.sizeDelta = logoSize;
        logoRect.anchoredPosition = Vector2.zero;

        Debug.Log("UI de final del juego creada");
    }

    void SetupAudio()
    {
        GameObject audioObj = new GameObject("EndGameMusic");
        audioObj.transform.SetParent(transform);
        DontDestroyOnLoad(audioObj);

        musicSource = audioObj.AddComponent<AudioSource>();
        musicSource.spatialBlend = 0f; // 2D
        musicSource.loop = false;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        if (endGameMusic != null)
        {
            musicSource.clip = endGameMusic;
        }
    }

    /// <summary>
    /// Inicia la secuencia de final del juego
    /// </summary>
    public void TriggerGameEnd()
    {
        if (isEnding)
        {
            Debug.LogWarning("La secuencia de final ya está en curso");
            return;
        }

        isEnding = true;
        StartCoroutine(EndGameSequence());
    }

    IEnumerator EndGameSequence()
    {
        Debug.Log("=== INICIANDO SECUENCIA DE FINAL ===");

        // Detener tiempo del juego (opcional)
        // Time.timeScale = 1f; // Mantener normal para el fade

        // 1. FADE OUT A NEGRO
        Debug.Log("Fase 1: Fade to black");
        fadeImage.gameObject.SetActive(true);
        yield return StartCoroutine(FadeToBlack(fadeOutDuration));

        // 2. PANTALLA NEGRA
        Debug.Log($"Fase 2: Pantalla negra ({blackScreenDelay}s)");
        yield return new WaitForSeconds(blackScreenDelay);

        // 3. REPRODUCIR MÚSICA (si existe)
        if (endGameMusic != null && musicSource != null)
        {
            musicSource.Play();
            Debug.Log("Reproduciendo música de final");
        }

        // 4. MOSTRAR LOGO CON FADE IN
        Debug.Log("Fase 3: Mostrando logo");
        logoImage.gameObject.SetActive(true);
        yield return StartCoroutine(FadeInLogo(logoFadeInDuration));

        // 5. MANTENER LOGO VISIBLE
        Debug.Log($"Fase 4: Logo visible ({logoDisplayTime}s)");
        yield return new WaitForSeconds(logoDisplayTime);

        // 6. FADE OUT LOGO
        Debug.Log("Fase 5: Fade out logo");
        yield return StartCoroutine(FadeOutLogo(finalFadeOutDuration));

        // 7. CARGAR MENÚ PRINCIPAL
        Debug.Log("Fase 6: Cargando menú principal");
        LoadMainMenu();
    }

    IEnumerator FadeToBlack(float duration)
    {
        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
        Debug.Log("Fade to black completado");
    }

    IEnumerator FadeInLogo(float duration)
    {
        float elapsed = 0f;
        Color c = logoImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            logoImage.color = c;
            yield return null;
        }

        c.a = 1f;
        logoImage.color = c;
        Debug.Log("Logo fade in completado");
    }

    IEnumerator FadeOutLogo(float duration)
    {
        float elapsed = 0f;
        Color startColor = logoImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

            logoImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        logoImage.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        Debug.Log("Logo fade out completado");
    }

    void LoadMainMenu()
    {
        Debug.Log($"Cargando escena: {mainMenuSceneName}");

        // Restaurar time scale
        Time.timeScale = 1f;

        // Detener música
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        // Limpiar cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Cargar menú principal
        SceneManager.LoadScene(mainMenuSceneName);

        Debug.Log("=== FINAL DEL JUEGO COMPLETADO ===");
    }

    /// <summary>
    /// Para testear la secuencia desde el editor
    /// </summary>
    [ContextMenu("Test End Game Sequence")]
    void TestEndSequence()
    {
        TriggerGameEnd();
    }
}