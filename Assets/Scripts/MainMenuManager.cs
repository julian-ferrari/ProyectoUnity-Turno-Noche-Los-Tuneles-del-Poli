using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Button References (Optional)")]
    public Button continueButton;
    public Button newGameButton;

    void Start()
    {
        UpdateContinueButton();
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
        Debug.Log("=== NUEVA PARTIDA - INICIANDO SISTEMA DE NOCHES ===");

        // MODIFICADO: Usar el sistema de noches
        NightSystem.StartNewGame();
    }

    public void ContinueGame()
    {
        SaveData data = SaveSystem.LoadGame();

        if (data != null && data.hasSavedGame)
        {
            Debug.Log($"Continuando partida guardada - Escena: {data.sceneName}");

            // Marcar que venimos de continuar
            GameStateManager.isLoadingFromSave = true;

            // Cargar la escena guardada
            SceneManager.LoadScene(data.sceneName);
        }
        else
        {
            Debug.LogWarning("No hay partida guardada para continuar");
            SceneManager.LoadScene("PoliNights");
        }
    }

    public void ShowAchievements()
    {
        SceneManager.LoadScene("AchievementsMenu");
    }

    public void ShowCredits()
    {
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
        }
    }
}