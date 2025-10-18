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
        // Habilitar/deshabilitar el botón Continuar según haya guardado
        UpdateContinueButton();
    }

    void UpdateContinueButton()
    {
        if (continueButton != null)
        {
            bool hasSave = SaveSystem.HasSavedGame();
            continueButton.interactable = hasSave;

            // Opcional: Cambiar el color del botón para indicar visualmente
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
        // Opcional: Eliminar guardado anterior al iniciar nueva partida
        // SaveSystem.DeleteSave();

        Debug.Log("Iniciando nueva partida");
        SceneManager.LoadScene("PoliNights");
    }

    public void ContinueGame()
    {
        SaveData data = SaveSystem.LoadGame();

        if (data != null && data.hasSavedGame)
        {
            Debug.Log($"Continuando partida guardada - Escena: {data.sceneName}");

            // Marcar que venimos de "Continuar" para restaurar posición
            GameStateManager.isLoadingFromSave = true;

            // Cargar la escena guardada
            SceneManager.LoadScene(data.sceneName);
        }
        else
        {
            Debug.LogWarning("No hay partida guardada para continuar");
            // Fallback: cargar escena normal
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

    // Método auxiliar para probar el sistema de guardado en el editor
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