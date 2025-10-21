using UnityEngine;

/// <summary>
/// Puerta final que requiere la llave específica para terminar el juego
/// </summary>
public class FinalDoor : MonoBehaviour
{
    [Header("Required Key")]
    [Tooltip("ID de la llave necesaria (debe coincidir exactamente)")]
    public string requiredKeyID = "Llave_Final";

    [Header("Interaction")]
    [Tooltip("Distancia para interactuar")]
    public float interactionDistance = 3f;

    [Tooltip("Tecla de interacción")]
    public KeyCode interactionKey = KeyCode.E;

    [Header("Audio")]
    [Tooltip("Sonido de cierre/cerradura al usar la llave")]
    public AudioClip lockSound;

    [Tooltip("Volumen del sonido")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Visual")]
    [Tooltip("Color del gizmo en el editor")]
    public Color gizmoColor = Color.magenta;

    private Transform player;
    private PlayerController playerController;
    private InventoryManager inventoryManager;
    private bool playerInRange = false;
    private bool doorUsed = false;
    private AudioSource audioSource;

    void Start()
    {
        // Buscar jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }

        // Buscar inventario usando el singleton
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("FinalDoor: No se encontró InventoryManager!");
        }

        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 3D
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        Debug.Log($"Puerta Final configurada - Llave requerida: '{requiredKeyID}'");
    }

    void Update()
    {
        if (player == null || doorUsed) return;

        // Verificar distancia
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionDistance;

        if (playerInRange)
        {
            // Verificar si tiene la llave correcta
            bool hasCorrectKey = CheckIfPlayerHasKey();

            if (hasCorrectKey)
            {
                // Mostrar mensaje de que puede usar la puerta
                if (playerController != null)
                {
                    playerController.ShowInteractionMessage($"Presiona E para usar la llave final");
                }

                // Intentar usar la llave
                if (Input.GetKeyDown(interactionKey))
                {
                    UseFinalDoor();
                }
            }
            else
            {
                // Mostrar mensaje específico según el problema
                string message = GetInteractionMessage();

                if (playerController != null)
                {
                    playerController.ShowInteractionMessage(message);
                }
            }
        }
        else
        {
            if (playerController != null && !doorUsed)
            {
                playerController.HideInteractionMessage();
            }
        }
    }

    string GetInteractionMessage()
    {
        if (inventoryManager == null)
            return "Error: No se encontró inventario";

        // Verificar si tiene la llave correcta en el inventario
        if (!inventoryManager.HasKey(requiredKeyID))
        {
            // Verificar si tiene alguna otra llave
            var allKeys = inventoryManager.GetCollectedKeyIDs();
            if (allKeys.Count > 0)
            {
                return $"Esta llave no sirve. Necesitas '{requiredKeyID}'";
            }
            else
            {
                return $"Necesitas conseguir '{requiredKeyID}'";
            }
        }

        // Tiene la llave correcta pero no la tiene equipada
        return $"Equipa '{requiredKeyID}' para abrir (presiona número del slot)";
    }

    bool CheckIfPlayerHasKey()
    {
        if (inventoryManager == null) return false;

        // Verificar si tiene la llave correcta en el inventario
        if (!inventoryManager.HasKey(requiredKeyID))
        {
            return false;
        }

        // Obtener el item seleccionado actualmente
        InventoryManager.InventoryItem selectedItem = inventoryManager.GetSelectedItem();

        if (selectedItem == null)
        {
            Debug.Log("FinalDoor: No hay item seleccionado");
            return false;
        }

        // Verificar que sea una llave
        if (selectedItem.type != InventoryManager.ItemType.Key)
        {
            Debug.Log($"FinalDoor: Item seleccionado no es llave, es: {selectedItem.type}");
            return false;
        }

        // Verificar si el ID de la llave coincide
        if (selectedItem.itemID == requiredKeyID)
        {
            Debug.Log($"FinalDoor: ¡Llave correcta equipada! {selectedItem.itemName}");
            return true;
        }

        Debug.Log($"FinalDoor: Llave incorrecta. Equipada: '{selectedItem.itemID}', Requerida: '{requiredKeyID}'");
        return false;
    }

    void UseFinalDoor()
    {
        if (doorUsed) return;

        doorUsed = true;

        Debug.Log("¡Puerta final usada! Iniciando secuencia de final...");

        // Ocultar mensaje de interacción
        if (playerController != null)
        {
            playerController.HideInteractionMessage();
        }

        // Desactivar controles del jugador
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Reproducir sonido de cerradura
        if (lockSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(lockSound, soundVolume);
            Debug.Log("Reproduciendo sonido de cerradura");
        }

        // Iniciar secuencia de final
        GameEndSequence endSequence = FindFirstObjectByType<GameEndSequence>();
        if (endSequence != null)
        {
            endSequence.TriggerGameEnd();
        }
        else
        {
            Debug.LogError("No se encontró GameEndSequence en la escena!");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar esfera de interacción
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        // Dibujar icono especial para puerta final
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 3f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 3.5f,
            $"🚪 PUERTA FINAL\nLlave: {requiredKeyID}\nInteracción: {interactionDistance}m"
        );
#endif
    }
}