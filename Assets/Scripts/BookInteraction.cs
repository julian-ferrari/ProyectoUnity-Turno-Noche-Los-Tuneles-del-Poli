using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistema para interactuar con libros y mostrar múltiples páginas
/// </summary>
public class BookInteraction : MonoBehaviour
{
    [Header("Book Settings")]
    [Tooltip("Nombre del libro (para debug)")]
    public string bookName = "Libro Misterioso";

    [Tooltip("Todas las páginas del libro (en orden)")]
    public Sprite[] bookPages = new Sprite[4];

    [Tooltip("Distancia para poder interactuar")]
    public float interactionDistance = 3f;

    [Tooltip("Tecla para interactuar (abrir/cerrar)")]
    public KeyCode interactionKey = KeyCode.E;

    [Tooltip("Teclas para navegar páginas")]
    public KeyCode nextPageKey = KeyCode.RightArrow;
    public KeyCode previousPageKey = KeyCode.LeftArrow;

    [Header("UI References")]
    [Tooltip("Canvas que contiene la imagen del libro (se crea automáticamente si es null)")]
    public Canvas bookCanvas;

    [Tooltip("Image component que muestra la página")]
    public Image bookPageImageUI;

    [Tooltip("Text que muestra el número de página")]
    public Text pageNumberText;

    [Header("Visual Feedback")]
    [Tooltip("Mostrar prompt 'Presiona E para leer'")]
    public bool showInteractionPrompt = true;

    [Tooltip("Color del gizmo en el editor")]
    public Color gizmoColor = Color.yellow;

    [Header("Audio (Opcional)")]
    [Tooltip("Sonido al pasar página")]
    public AudioClip pageFlipSound;

    [Tooltip("Sonido al abrir libro")]
    public AudioClip openBookSound;

    [Tooltip("Sonido al cerrar libro")]
    public AudioClip closeBookSound;

    private Transform player;
    private PlayerController playerController;
    private bool playerInRange = false;
    private bool isReadingBook = false;
    private int currentPageIndex = 0;
    private AudioSource audioSource;

    // UI temporal si no está asignada
    private GameObject tempUIHolder;
    private Text instructionText;

    void Start()
    {
        // Buscar al jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }

        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D
        audioSource.playOnAwake = false;

        // Setup UI si no está asignada
        if (bookCanvas == null || bookPageImageUI == null)
        {
            SetupBookUI();
        }

        // Asegurar que el UI esté oculto al inicio
        HideBookPage();

        // Validar páginas
        ValidatePages();

        Debug.Log($"Libro '{bookName}' configurado con {bookPages.Length} páginas");
    }

    void Update()
    {
        if (player == null) return;

        // Verificar distancia al jugador
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionDistance;

        // Si está leyendo, manejar navegación de páginas
        if (isReadingBook)
        {
            HandlePageNavigation();

            // Cerrar libro
            if (Input.GetKeyDown(interactionKey))
            {
                CloseBook();
            }
        }
        else
        {
            // Abrir libro
            if (playerInRange && Input.GetKeyDown(interactionKey))
            {
                OpenBook();
            }
        }

        // Mostrar prompt solo si está en rango y NO está leyendo
        if (showInteractionPrompt && playerController != null)
        {
            if (playerInRange && !isReadingBook)
            {
                playerController.ShowInteractionMessage($"Presiona E para leer '{bookName}'");
            }
            else if (!playerInRange && !isReadingBook)
            {
                playerController.HideInteractionMessage();
            }
        }
    }

    void ValidatePages()
    {
        int validPages = 0;
        for (int i = 0; i < bookPages.Length; i++)
        {
            if (bookPages[i] != null)
                validPages++;
        }

        if (validPages == 0)
        {
            Debug.LogWarning($"Libro '{bookName}': No hay páginas asignadas!");
        }
        else if (validPages < bookPages.Length)
        {
            Debug.LogWarning($"Libro '{bookName}': {validPages}/{bookPages.Length} páginas asignadas");
        }
    }

    void HandlePageNavigation()
    {
        // Página siguiente
        if (Input.GetKeyDown(nextPageKey))
        {
            NextPage();
        }

        // Página anterior
        if (Input.GetKeyDown(previousPageKey))
        {
            PreviousPage();
        }
    }

    void NextPage()
    {
        if (currentPageIndex < bookPages.Length - 1)
        {
            currentPageIndex++;
            UpdateCurrentPage();
            PlayPageFlipSound();
            Debug.Log($"Página siguiente: {currentPageIndex + 1}/{bookPages.Length}");
        }
        else
        {
            Debug.Log("Ya estás en la última página");
        }
    }

    void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdateCurrentPage();
            PlayPageFlipSound();
            Debug.Log($"Página anterior: {currentPageIndex + 1}/{bookPages.Length}");
        }
        else
        {
            Debug.Log("Ya estás en la primera página");
        }
    }

    void UpdateCurrentPage()
    {
        if (bookPageImageUI != null && currentPageIndex >= 0 && currentPageIndex < bookPages.Length)
        {
            bookPageImageUI.sprite = bookPages[currentPageIndex];

            // Actualizar número de página
            if (pageNumberText != null)
            {
                pageNumberText.text = $"Página {currentPageIndex + 1} / {bookPages.Length}";
            }

            // Actualizar texto de instrucción
            if (instructionText != null)
            {
                UpdateInstructionText();
            }
        }
    }

    void UpdateInstructionText()
    {
        string text = "Presiona E para cerrar";

        // Añadir instrucciones de navegación según la página actual
        if (currentPageIndex == 0)
        {
            text += " | → Siguiente página";
        }
        else if (currentPageIndex == bookPages.Length - 1)
        {
            text += " | ← Página anterior";
        }
        else
        {
            text += " | ← → Navegar páginas";
        }

        instructionText.text = text;
    }

    void SetupBookUI()
    {
        Debug.Log("Creando UI de libro automáticamente...");

        // Crear Canvas
        tempUIHolder = new GameObject($"BookUI_{bookName}");
        DontDestroyOnLoad(tempUIHolder);

        bookCanvas = tempUIHolder.AddComponent<Canvas>();
        bookCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        bookCanvas.sortingOrder = 100;

        CanvasScaler scaler = tempUIHolder.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        tempUIHolder.AddComponent<GraphicRaycaster>();

        // Crear fondo negro semi-transparente
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(bookCanvas.transform, false);

        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);
        bg.raycastTarget = true;

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Crear imagen de la página del libro
        GameObject pageObj = new GameObject("BookPage");
        pageObj.transform.SetParent(bookCanvas.transform, false);

        bookPageImageUI = pageObj.AddComponent<Image>();
        bookPageImageUI.sprite = bookPages.Length > 0 ? bookPages[0] : null;
        bookPageImageUI.preserveAspect = true;

        RectTransform pageRect = bookPageImageUI.GetComponent<RectTransform>();
        pageRect.anchorMin = new Vector2(0.5f, 0.5f);
        pageRect.anchorMax = new Vector2(0.5f, 0.5f);
        pageRect.sizeDelta = new Vector2(1200, 800);
        pageRect.anchoredPosition = Vector2.zero;

        // Crear indicador de número de página
        GameObject pageNumObj = new GameObject("PageNumber");
        pageNumObj.transform.SetParent(bookCanvas.transform, false);

        pageNumberText = pageNumObj.AddComponent<Text>();
        pageNumberText.text = "Página 1 / 4";
        pageNumberText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pageNumberText.fontSize = 20;
        pageNumberText.color = new Color(0.8f, 0.8f, 0.8f);
        pageNumberText.alignment = TextAnchor.UpperCenter;

        RectTransform pageNumRect = pageNumberText.GetComponent<RectTransform>();
        pageNumRect.anchorMin = new Vector2(0.5f, 1);
        pageNumRect.anchorMax = new Vector2(0.5f, 1);
        pageNumRect.sizeDelta = new Vector2(300, 40);
        pageNumRect.anchoredPosition = new Vector2(0, -40);

        // Crear texto de instrucción
        GameObject instructionObj = new GameObject("Instruction");
        instructionObj.transform.SetParent(bookCanvas.transform, false);

        instructionText = instructionObj.AddComponent<Text>();
        instructionText.text = "Presiona E para cerrar | → Siguiente página";
        instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionText.fontSize = 24;
        instructionText.color = Color.white;
        instructionText.alignment = TextAnchor.LowerCenter;

        RectTransform instructionRect = instructionText.GetComponent<RectTransform>();
        instructionRect.anchorMin = new Vector2(0.5f, 0);
        instructionRect.anchorMax = new Vector2(0.5f, 0);
        instructionRect.sizeDelta = new Vector2(800, 50);
        instructionRect.anchoredPosition = new Vector2(0, 30);

        // Crear flechas visuales (opcional)
        CreateNavigationArrows();

        Debug.Log("UI de libro creada exitosamente");
    }

    void CreateNavigationArrows()
    {
        // Flecha izquierda
        GameObject leftArrowObj = new GameObject("LeftArrow");
        leftArrowObj.transform.SetParent(bookCanvas.transform, false);

        Text leftArrow = leftArrowObj.AddComponent<Text>();
        leftArrow.text = "◄";
        leftArrow.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        leftArrow.fontSize = 60;
        leftArrow.color = new Color(1, 1, 1, 0.6f);
        leftArrow.alignment = TextAnchor.MiddleCenter;

        RectTransform leftRect = leftArrow.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0.5f);
        leftRect.anchorMax = new Vector2(0, 0.5f);
        leftRect.sizeDelta = new Vector2(80, 80);
        leftRect.anchoredPosition = new Vector2(100, 0);

        // Flecha derecha
        GameObject rightArrowObj = new GameObject("RightArrow");
        rightArrowObj.transform.SetParent(bookCanvas.transform, false);

        Text rightArrow = rightArrowObj.AddComponent<Text>();
        rightArrow.text = "►";
        rightArrow.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rightArrow.fontSize = 60;
        rightArrow.color = new Color(1, 1, 1, 0.6f);
        rightArrow.alignment = TextAnchor.MiddleCenter;

        RectTransform rightRect = rightArrow.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1, 0.5f);
        rightRect.anchorMax = new Vector2(1, 0.5f);
        rightRect.sizeDelta = new Vector2(80, 80);
        rightRect.anchoredPosition = new Vector2(-100, 0);
    }

    void OpenBook()
    {
        if (bookPages.Length == 0 || bookPages[0] == null)
        {
            Debug.LogWarning($"No se puede abrir '{bookName}': no hay páginas asignadas!");
            return;
        }

        isReadingBook = true;
        currentPageIndex = 0; // Siempre empezar en la primera página

        // Mostrar UI
        if (bookCanvas != null)
        {
            bookCanvas.gameObject.SetActive(true);
        }

        // Actualizar a la primera página
        UpdateCurrentPage();

        // Desactivar controles del jugador
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Mostrar cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ocultar mensaje de interacción
        if (playerController != null)
        {
            playerController.HideInteractionMessage();
        }

        // Sonido de abrir
        PlayOpenBookSound();

        Debug.Log($"Abriendo libro: {bookName} (Página 1/{bookPages.Length})");
    }

    void CloseBook()
    {
        isReadingBook = false;

        // Ocultar UI
        HideBookPage();

        // Reactivar controles del jugador
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // Ocultar cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Sonido de cerrar
        PlayCloseBookSound();

        Debug.Log($"Cerrando libro: {bookName}");
    }

    void HideBookPage()
    {
        if (bookCanvas != null)
        {
            bookCanvas.gameObject.SetActive(false);
        }
    }

    // Métodos de sonido
    void PlayPageFlipSound()
    {
        if (pageFlipSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pageFlipSound, 0.7f);
        }
    }

    void PlayOpenBookSound()
    {
        if (openBookSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(openBookSound);
        }
    }

    void PlayCloseBookSound()
    {
        if (closeBookSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(closeBookSound);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar esfera de interacción
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        // Dibujar línea hacia arriba
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);

        // Dibujar icono de libro con info
#if UNITY_EDITOR
        int validPages = 0;
        foreach (var page in bookPages)
        {
            if (page != null) validPages++;
        }

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"📖 {bookName}\n{validPages}/{bookPages.Length} páginas\nInteraction: {interactionDistance}m"
        );
#endif
    }

    void OnDestroy()
    {
        // Limpiar UI temporal
        if (tempUIHolder != null)
        {
            Destroy(tempUIHolder);
        }

        // Asegurar que los controles estén activos
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}