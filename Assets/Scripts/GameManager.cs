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
            // Usar el nuevo método ResetGuard()
            guard.ResetGuard();
        }
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
}