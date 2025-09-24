using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PoliNightsPauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuCanvas;
    public GameObject pauseMenuPanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button saveButton;
    public Button achievementsButton;
    public Button surrenderButton;
    public Button exitButton;

    [Header("Pause Settings")]
    public KeyCode pauseKey = KeyCode.Escape;
    public bool pauseOnFocusLost = true;

    [Header("Audio (Optional)")]
    public AudioSource pauseSound;
    public AudioSource resumeSound;
    public AudioSource buttonClickSound;

    private bool isPaused = false;
    private bool canPause = true;
    private float originalTimeScale;

    // Referencias del juego para pausar correctamente
    private AudioListener audioListener;
    private GameObject player;

    void Start()
    {
        // Guardar configuración original
        originalTimeScale = Time.timeScale;
        audioListener = FindFirstObjectByType<AudioListener>();

        // Encontrar referencia del jugador (ajustar según tu setup)
        player = GameObject.FindGameObjectWithTag("Player");

        // DEBUG: Verificar referencias
        Debug.Log("=== PAUSE MENU SETUP ===");
        Debug.Log($"pauseMenuCanvas: {(pauseMenuCanvas != null ? pauseMenuCanvas.name : "NULL")}");
        Debug.Log($"pauseMenuPanel: {(pauseMenuPanel != null ? pauseMenuPanel.name : "NULL")}");
        Debug.Log($"resumeButton: {(resumeButton != null ? resumeButton.name : "NULL")}");
        Debug.Log($"player found: {(player != null ? player.name : "NULL")}");

        // Si no se asignó pauseMenuCanvas, usar este mismo GameObject
        if (pauseMenuCanvas == null)
        {
            pauseMenuCanvas = this.gameObject;
            Debug.Log("Usando este GameObject como pauseMenuCanvas");
        }

        // Configurar botones
        SetupButtons();

        // Asegurar que el menú esté oculto al inicio
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
            Debug.Log("Menú de pausa DESACTIVADO al inicio");
        }

        Debug.Log("=== FIN PAUSE MENU SETUP ===");
    }

    void Update()
    {
        HandlePauseInput();
    }

    void HandlePauseInput()
    {
        if (!canPause) return;

        // DEBUG adicional
        if (Input.GetKeyDown(pauseKey))
        {
            Debug.Log($"=== ESC PRESIONADO ===");
            Debug.Log($"canPause: {canPause}");
            Debug.Log($"isPaused: {isPaused}");
            Debug.Log($"pauseMenuCanvas: {(pauseMenuCanvas != null ? pauseMenuCanvas.name : "NULL")}");

            if (isPaused)
            {
                Debug.Log("Intentando REANUDAR...");
                ResumeGame();
            }
            else
            {
                Debug.Log("Intentando PAUSAR...");
                PauseGame();
            }
        }
    }

    void SetupButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(() => {
                PlayButtonSound();
                ResumeGame();
            });
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(() => {
                PlayButtonSound();
                SaveGame();
            });
        }

        if (achievementsButton != null)
        {
            achievementsButton.onClick.AddListener(() => {
                PlayButtonSound();
                OpenAchievements();
            });
        }

        if (surrenderButton != null)
        {
            surrenderButton.onClick.AddListener(() => {
                PlayButtonSound();
                SurrenderGame();
            });
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() => {
                PlayButtonSound();
                ExitToMainMenu();
            });
        }
    }

    public void PauseGame()
    {
        if (!canPause || isPaused) return;

        Debug.Log("=== PAUSANDO JUEGO ===");

        isPaused = true;
        Time.timeScale = 0f;
        Debug.Log($"Time.timeScale establecido a: {Time.timeScale}");

        // Mostrar menú de pausa
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(true);
            Debug.Log($"Canvas activado: {pauseMenuCanvas.name}");
        }
        else
        {
            Debug.LogError("❌ pauseMenuCanvas es NULL!");
        }

        // Configurar cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("Cursor configurado: visible=true, lockState=None");

        // Pausar audio del juego (opcional)
        if (audioListener != null)
        {
            AudioListener.pause = true;
        }

        // Reproducir sonido de pausa
        PlayPauseSound();

        // Seleccionar el primer botón para navegación con teclado
        if (resumeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            Debug.Log($"Botón seleccionado: {resumeButton.name}");
        }
        else
        {
            Debug.LogError("❌ resumeButton es NULL!");
        }

        // Pausar scripts específicos del jugador (opcional)
        PausePlayerScripts(true);

        Debug.Log("PoliNights - Juego pausado COMPLETADO");
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = originalTimeScale;

        // Ocultar menú de pausa
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        // Restaurar configuración del cursor (ajustar según tu juego)
        // Descomenta y ajusta según necesites:
        // Cursor.visible = false;
        // Cursor.lockState = CursorLockMode.Locked;

        // Reanudar audio
        if (audioListener != null)
        {
            AudioListener.pause = false;
        }

        // Reproducir sonido de reanudación
        PlayResumeSound();

        // Reanudar scripts del jugador
        PausePlayerScripts(false);

        Debug.Log("PoliNights - Juego reanudado");
    }

    public void SaveGame()
    {
        Debug.Log("Guardar juego - Funcionalidad pendiente");
        // TODO: Implementar sistema de guardado

        // Ejemplo de estructura:
        // PlayerPrefs.SetFloat("PlayerPosX", player.transform.position.x);
        // PlayerPrefs.SetFloat("PlayerPosY", player.transform.position.y);
        // PlayerPrefs.SetFloat("PlayerPosZ", player.transform.position.z);
        // PlayerPrefs.SetString("CurrentLevel", SceneManager.GetActiveScene().name);
        // PlayerPrefs.Save();

        // Mostrar mensaje de confirmación (opcional)
        ShowSaveConfirmation();
    }

    public void OpenAchievements()
    {
        Debug.Log("Abrir menú de logros desde pausa");

        // Restaurar timeScale antes de cambiar escena
        ResumeGame();

        // Cargar escena de logros
        SceneManager.LoadScene("AchievementsMenu");
    }

    public void SurrenderGame()
    {
        Debug.Log("Rendirse - Funcionalidad pendiente");
        // TODO: Implementar lógica de rendición

        // Ejemplo:
        // - Mostrar pantalla de Game Over
        // - Resetear progreso del nivel
        // - Volver al menú principal
        // - Guardar estadísticas de rendición

        // Por ahora, volver al menú principal
        ExitToMainMenu();
    }

    public void ExitToMainMenu()
    {
        Debug.Log("Salir al menú principal");

        // Restaurar timeScale antes de cambiar escena
        Time.timeScale = originalTimeScale;
        AudioListener.pause = false;

        // Cargar escena del menú principal
        SceneManager.LoadScene("MainMenu");
    }

    void PausePlayerScripts(bool pause)
    {
        if (player == null) return;

        // Pausar/reanudar scripts específicos del jugador
        // Ajustar según los componentes que tenga tu jugador

        MonoBehaviour[] playerScripts = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in playerScripts)
        {
            if (script != null)
            {
                script.enabled = !pause;
            }
        }
    }

    void ShowSaveConfirmation()
    {
        // TODO: Mostrar mensaje de "Juego guardado" temporalmente
        Debug.Log("¡Juego guardado exitosamente!");
    }

    #region Audio Methods
    void PlayPauseSound()
    {
        if (pauseSound != null)
        {
            pauseSound.Play();
        }
    }

    void PlayResumeSound()
    {
        if (resumeSound != null)
        {
            resumeSound.Play();
        }
    }

    void PlayButtonSound()
    {
        if (buttonClickSound != null)
        {
            buttonClickSound.Play();
        }
    }
    #endregion

    #region Event Handlers
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && pauseOnFocusLost && !isPaused && canPause)
        {
            PauseGame();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && pauseOnFocusLost && !isPaused && canPause)
        {
            PauseGame();
        }
    }
    #endregion

    #region Public Properties and Methods
    public bool IsPaused => isPaused;
    public bool CanPause => canPause;

    public void SetCanPause(bool canPause)
    {
        this.canPause = canPause;

        if (!canPause && isPaused)
        {
            ResumeGame();
        }
    }

    // Método para pausar desde otros scripts
    public void PauseFromScript()
    {
        if (canPause && !isPaused)
        {
            PauseGame();
        }
    }
    #endregion
}