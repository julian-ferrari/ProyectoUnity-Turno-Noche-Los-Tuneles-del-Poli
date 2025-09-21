using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void NewGame()
    {
        SceneManager.LoadScene("PoliNights");
    }

    public void ContinueGame()
    {
        // Aquí puedes agregar lógica para cargar una partida guardada
        // Por ahora, simplemente carga la escena del juego
        SceneManager.LoadScene("PoliNights");
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
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}