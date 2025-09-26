using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CreditsManager : MonoBehaviour
{
    [Header("Navigation")]
    public Button backToMenuButton;

    // Static variable para comunicación entre escenas
    public static bool cameFromPauseMenu = false;

    [Header("Credits Animation (Optional)")]
    public RectTransform creditsContent;
    public float scrollSpeed = 50f;
    public bool autoScroll = true;

    void Start()
    {
        // Detectar de dónde viene el jugador
        Debug.Log($"CreditsMenu: cameFromPauseMenu = {cameFromPauseMenu}");

        // Configurar botón de volver si existe
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(HandleBackNavigation);
        }
    }

    void Update()
    {
        // Volver con ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackNavigation();
        }

        // También Enter o Space
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            HandleBackNavigation();
        }

        // Scroll automático de créditos (opcional)
        if (autoScroll && creditsContent != null)
        {
            creditsContent.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
        }

        // Control manual del scroll con flechas
        if (creditsContent != null)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                creditsContent.anchoredPosition += Vector2.up * scrollSpeed * 2 * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                creditsContent.anchoredPosition += Vector2.down * scrollSpeed * 2 * Time.deltaTime;
            }
        }
    }

    public void HandleBackNavigation()
    {
        if (cameFromPauseMenu)
        {
            Debug.Log("Regresando al menú de pausa en PoliNights");
            // Resetear el flag antes de cambiar de escena
            cameFromPauseMenu = false;
            // Volver a la escena del juego con el menú de pausa abierto
            GoBackToPauseMenu();
        }
        else
        {
            Debug.Log("Regresando al menú principal");
            BackToMainMenu();
        }
    }

    void GoBackToPauseMenu()
    {
        // Marcar que debe mostrar el menú de pausa al cargar
        PoliNightsPauseMenu.shouldShowPauseMenuOnLoad = true;
        SceneManager.LoadScene("PoliNights");
    }

    public void BackToMainMenu()
    {
        // Asegurar que el flag esté limpio
        cameFromPauseMenu = false;
        SceneManager.LoadScene("MainMenu");
    }

    public void ToggleAutoScroll()
    {
        autoScroll = !autoScroll;
    }
}