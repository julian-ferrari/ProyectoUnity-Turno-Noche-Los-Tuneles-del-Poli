using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigationManager : MonoBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] private string targetSceneName = "MainMenu";
    [SerializeField] private bool useEscapeKey = true;
    [SerializeField] private bool useBackspaceKey = false;
    [SerializeField] private bool useBackButton = false;

    [Header("Back Button (Optional)")]
    [SerializeField] private UnityEngine.UI.Button backButton;

    [Header("Confirmation (Optional)")]
    [SerializeField] private bool requireConfirmation = false;
    [SerializeField] private GameObject confirmationPanel;

    private bool isConfirmationActive = false;

    void Start()
    {
        // Configurar botón de volver si existe
        if (backButton != null && useBackButton)
        {
            backButton.onClick.AddListener(HandleBackNavigation);
        }

        // Ocultar panel de confirmación al inicio
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
    }

    void Update()
    {
        HandleKeyboardInput();
    }

    void HandleKeyboardInput()
    {
        // Si hay confirmación activa, manejar eso primero
        if (isConfirmationActive)
        {
            if (Input.GetKeyDown(KeyCode.Y) || Input.GetKeyDown(KeyCode.Return))
            {
                ConfirmNavigation();
            }
            else if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelNavigation();
            }
            return;
        }

        // Navegación normal
        bool shouldNavigateBack = false;

        if (useEscapeKey && Input.GetKeyDown(KeyCode.Escape))
        {
            shouldNavigateBack = true;
        }

        if (useBackspaceKey && Input.GetKeyDown(KeyCode.Backspace))
        {
            shouldNavigateBack = true;
        }

        if (shouldNavigateBack)
        {
            HandleBackNavigation();
        }
    }

    public void HandleBackNavigation()
    {
        if (requireConfirmation)
        {
            ShowConfirmation();
        }
        else
        {
            GoToTargetScene();
        }
    }

    void ShowConfirmation()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
            isConfirmationActive = true;

            // Pausar el juego si es necesario
            Time.timeScale = 0f;
        }
        else
        {
            // Si no hay panel, ir directamente
            GoToTargetScene();
        }
    }

    public void ConfirmNavigation()
    {
        Time.timeScale = 1f;
        isConfirmationActive = false;

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        GoToTargetScene();
    }

    public void CancelNavigation()
    {
        Time.timeScale = 1f;
        isConfirmationActive = false;

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    void GoToTargetScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    // Métodos públicos para llamar desde botones
    public void GoToMainMenu()
    {
        targetSceneName = "MainMenu";
        GoToTargetScene();
    }

    public void GoToScene(string sceneName)
    {
        targetSceneName = sceneName;
        GoToTargetScene();
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}