using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Button References (Optional)")]
    public Button continueButton;
    public Button newGameButton;

    private bool isInitialized = false;

    void Awake()
    {
        // CRÍTICO: Resetear el tiempo al cargar el menú
        Time.timeScale = 1f;
        Debug.Log("MainMenu: Time.timeScale reseteado a 1");
    }

    void Start()
    {
        // Asegurar que el cursor esté visible y desbloqueado en el menú
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Esperar un frame antes de inicializar para asegurar que todo esté listo
        StartCoroutine(InitializeAfterFrame());
    }

    IEnumerator InitializeAfterFrame()
    {
        // Esperar hasta el final del frame
        yield return new WaitForEndOfFrame();

        // Inicializar
        UpdateContinueButton();
        isInitialized = true;

        Debug.Log("MainMenuManager inicializado correctamente");
    }

    void UpdateContinueButton()
    {
        if (continueButton != null)
        {
            bool hasSave = SaveSystem.HasSavedGame();
            continueButton.interactable = hasSave;

            if (hasSave)
            {
                Debug.Log("Partida guardada detectada - Botón Continuar habilitado");
            }
            else
            {
                Debug.Log("No hay partida guardada - Botón Continuar deshabilitado");
            }
        }
    }

    public void NewGame()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("MainMenu no está inicializado, esperando...");
            StartCoroutine(WaitAndExecute(() => NewGame()));
            return;
        }

        Debug.Log("=== NUEVA PARTIDA - INICIANDO SISTEMA DE NOCHES ===");

        // CRÍTICO: Asegurar que el tiempo esté normal
        Time.timeScale = 1f;

        // Limpiar cualquier estado previo
        GameStateManager.isLoadingFromSave = false;

        // Resetear PlayerPrefs de puertas y llaves (para nueva partida limpia)
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // Usar el sistema de noches
        NightSystem.StartNewGame();
    }

    public void ContinueGame()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("MainMenu no está inicializado, esperando...");
            StartCoroutine(WaitAndExecute(() => ContinueGame()));
            return;
        }

        SaveData data = SaveSystem.LoadGame();
        if (data != null && data.hasSavedGame)
        {
            Debug.Log($"=== CONTINUANDO PARTIDA GUARDADA ===");
            Debug.Log($"  Escena: {data.sceneName}");
            Debug.Log($"  Fecha: {data.saveDateTime}");
            Debug.Log($"  Llaves guardadas: {data.collectedKeyIDs.Count}");

            // CRÍTICO: Asegurar que el tiempo esté normal
            Time.timeScale = 1f;

            // Marcar que venimos de continuar
            GameStateManager.isLoadingFromSave = true;

            // Cargar la escena guardada
            SceneManager.LoadScene(data.sceneName);
        }
        else
        {
            Debug.LogWarning("No hay partida guardada para continuar");
            Time.timeScale = 1f;
            SceneManager.LoadScene("PoliNights");
        }
    }

    IEnumerator WaitAndExecute(System.Action action)
    {
        yield return new WaitUntil(() => isInitialized);
        action?.Invoke();
    }

    public void ShowAchievements()
    {
        Debug.Log("Navegando a Achievements");
        Time.timeScale = 1f;
        SceneManager.LoadScene("AchievementsMenu");
    }

    public void ShowCredits()
    {
        Debug.Log("Navegando a Credits");
        Time.timeScale = 1f;
        SceneManager.LoadScene("CreditsMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void OnEnable()
    {
        // Asegurar cursor visible cuando se habilita el menú
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Asegurar que el tiempo esté normal
        Time.timeScale = 1f;
    }

    [ContextMenu("Test - Delete Save File")]
    void TestDeleteSave()
    {
        SaveSystem.DeleteSave();
        UpdateContinueButton();
        Debug.Log("Save file deleted");
    }

    [ContextMenu("Test - Check Save Status")]
    void TestCheckSave()
    {
        bool hasSave = SaveSystem.HasSavedGame();
        Debug.Log($"Has saved game: {hasSave}");
        if (hasSave)
        {
            SaveData data = SaveSystem.LoadGame();
            Debug.Log($"Save details - Scene: {data.sceneName}, Date: {data.saveDateTime}");
            Debug.Log($"Keys saved: {data.collectedKeyIDs.Count}");
        }
    }
}