using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    public int currentNight = 1;
    public bool gameOver = false;

    [Header("Respawn Settings")]
    public Transform playerSpawnPoint;
    public Transform guardSpawnPoint;

    private Vector3 playerStartPosition;
    private Vector3 guardStartPosition;
    private Quaternion playerStartRotation;
    private Quaternion guardStartRotation;

    void Start()
    {
        // Guardar posiciones iniciales
        PlayerController player = FindFirstObjectByType<PlayerController>();
        GuardAI guard = FindFirstObjectByType<GuardAI>();

        if (player != null)
        {
            playerStartPosition = playerSpawnPoint != null ? playerSpawnPoint.position : player.transform.position;
            playerStartRotation = playerSpawnPoint != null ? playerSpawnPoint.rotation : player.transform.rotation;
        }

        if (guard != null)
        {
            guardStartPosition = guardSpawnPoint != null ? guardSpawnPoint.position : guard.transform.position;
            guardStartRotation = guardSpawnPoint != null ? guardSpawnPoint.rotation : guard.transform.rotation;
        }
    }

    public void RestartNight()
    {
        Debug.Log("¡Te atraparon! Reiniciando noche " + currentNight);

        // Resetear posiciones
        PlayerController player = FindFirstObjectByType<PlayerController>();
        GuardAI guard = FindFirstObjectByType<GuardAI>();

        if (player != null)
        {
            // Resetear posición y rotación del jugador
            player.transform.position = playerStartPosition;
            player.transform.rotation = playerStartRotation;

            // Resetear velocidad del Rigidbody
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }

            // Resetear cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (guard != null)
        {
            // MEJORADO: Usar el nuevo método ResetGuard()
            guard.ResetGuard();
        }

        // Pequeña pausa dramática
        Time.timeScale = 0.1f;
        Invoke("ResetTimeScale", 0.5f);
    }

    void ResetTimeScale()
    {
        Time.timeScale = 1f;
    }

    public void NextNight()
    {
        currentNight++;
        Debug.Log("¡Sobreviviste! Avanzando a la noche " + currentNight);
        // Aquí podrías hacer la escena más difícil, más guardias, etc.
    }

    public void GameOver()
    {
        gameOver = true;
        Debug.Log("Game Over! Noches completadas: " + (currentNight - 1));
        // Aquí mostrarías pantalla de game over
    }

    void OnGUI()
    {
        // UI básica simplificada
        GUI.Box(new Rect(10, 10, 200, 60), "");

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;

        GUI.Label(new Rect(15, 15, 180, 20), "Noche: " + currentNight, labelStyle);

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            string noiseIcon = player.currentNoiseLevel > 2f ? "Alto" : player.currentNoiseLevel > 1f ? "Medio" : "Bajo";
            GUI.Label(new Rect(15, 35, 180, 20), "Ruido: " + noiseIcon, labelStyle);
        }

        // Mostrar estado del guardia para debug
        GuardAI guard = FindFirstObjectByType<GuardAI>();
        if (guard != null)
        {
            GUI.Label(new Rect(15, 55, 180, 20), "Guardia: " + guard.currentState.ToString(), labelStyle);
        }

        // Botón de reinicio manual (útil para testing)
        if (GUI.Button(new Rect(10, 90, 100, 30), "Reiniciar (R)"))
        {
            RestartNight();
        }

        // Atajo de teclado
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartNight();
        }
    }
}