using UnityEngine;
using UnityEngine.SceneManagement;

public class AchievementsManager : MonoBehaviour
{
    // Static variable para comunicación entre escenas
    public static bool cameFromPauseMenu = false;

    void Start()
    {
        Debug.Log($"=== ACHIEVEMENTS START ===");
        Debug.Log($"cameFromPauseMenu = {cameFromPauseMenu}");
        Debug.Log($"=========================");
    }

    void Update()
    {
        // Solo manejar ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("=== ESC PRESIONADO EN ACHIEVEMENTS ===");
            HandleBackNavigation();
        }
    }

    public void HandleBackNavigation()
    {
        Debug.Log($"HandleBackNavigation called. cameFromPauseMenu = {cameFromPauseMenu}");

        if (cameFromPauseMenu)
        {
            Debug.Log("✅ Regresando al menú de pausa en PoliNights");
            // Resetear el flag antes de cambiar de escena
            cameFromPauseMenu = false;
            // Volver a la escena del juego con el menú de pausa abierto
            GoBackToPauseMenu();
        }
        else
        {
            Debug.Log("❌ Yendo al menú principal (no vino del menú de pausa)");
            BackToMainMenu();
        }
    }

    void GoBackToPauseMenu()
    {
        Debug.Log("🔄 Marcando shouldShowPauseMenuOnLoad = true");
        // Marcar que debe mostrar el menú de pausa al cargar
        PoliNightsPauseMenu.shouldShowPauseMenuOnLoad = true;

        Debug.Log("🔄 Cargando escena PoliNights...");
        SceneManager.LoadScene("PoliNights");
    }

    public void BackToMainMenu()
    {
        Debug.Log("🏠 Yendo al menú principal");
        // Asegurar que el flag esté limpio
        cameFromPauseMenu = false;
        PoliNightsPauseMenu.shouldShowPauseMenuOnLoad = false;
        SceneManager.LoadScene("MainMenu");
    }
}