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

    // Variable estática para comunicación entre escenas
    public static bool shouldShowPauseMenuOnLoad = false;

    // Referencias del juego para pausar correctamente
    private AudioListener audioListener;
    private GameObject player;

    void Start()
    {
        // Guardar configuración original
        originalTimeScale = Time.timeScale;
        audioListener = FindFirstObjectByType<AudioListener>();

        // Encontrar referencia del jugador
        player = GameObject.FindGameObjectWithTag("Player");

        // DEBUG: Verificar referencias
        Debug.Log("=== PAUSE MENU SETUP ===");
        Debug.Log($"pauseMenuCanvas: {(pauseMenuCanvas != null ? pauseMenuCanvas.name : "NULL")}");
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

        // Verificar si debe mostrar el menú al cargar (regresando de logros/créditos)
        if (shouldShowPauseMenuOnLoad)
        {
            shouldShowPauseMenuOnLoad = false; // Resetear flag
            Debug.Log("Mostrando menú de pausa porque regresó de otra escena");
            Invoke(nameof(PauseGame), 0.1f);
        }
    }

    void Update()
    {
        HandlePauseInput();
    }

    void HandlePauseInput()
    {
        if (!canPause) return;

        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
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

        isPaused = true;
        Time.timeScale = 0f;

        // Mostrar menú de pausa
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(true);
        }

        // Configurar cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Pausar audio del juego
        if (audioListener != null)
        {
            AudioListener.pause = true;
        }

        // Reproducir sonido de pausa
        PlayPauseSound();

        // Seleccionar el primer botón
        if (resumeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }

        Debug.Log("PoliNights - Juego pausado");
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = originalTimeScale;

        // Ocultar menú de pausa
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        // Restaurar cursor (ajusta según necesites)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Reanudar audio
        if (audioListener != null)
        {
            AudioListener.pause = false;
        }

        // Reproducir sonido de reanudación
        PlayResumeSound();

        Debug.Log("PoliNights - Juego reanudado");
    }

    public void SaveGame()
    {
        Debug.Log("Guardar juego - Funcionalidad pendiente");
        // TODO: Implementar sistema de guardado
    }

    public void OpenAchievements()
    {
        Debug.Log("Abrir menú de logros desde pausa");

        // Marcar que viene del menú de pausa
        AchievementsManager.cameFromPauseMenu = true;

        // Restaurar timeScale antes de cambiar escena
        Time.timeScale = originalTimeScale;
        AudioListener.pause = false;

        // Cargar escena de logros
        SceneManager.LoadScene("AchievementsMenu");
    }

    public void OpenCredits()
    {
        Debug.Log("Abrir menú de créditos desde pausa");

        // Marcar que viene del menú de pausa
        CreditsManager.cameFromPauseMenu = true;

        // Restaurar timeScale antes de cambiar escena
        Time.timeScale = originalTimeScale;
        AudioListener.pause = false;

        // Cargar escena de créditos
        SceneManager.LoadScene("CreditsMenu");
    }

    public void SurrenderGame()
    {
        Debug.Log("Rendirse - Funcionalidad pendiente");
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

    #region Public Properties
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
    #endregion
}