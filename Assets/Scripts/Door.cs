using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    public string requiredKeyID = "DefaultKey"; // ID de la llave necesaria
    public bool isLocked = true;
    public bool isOpen = false;

    [Header("Door Animation")]
    public Transform doorPivot; // El objeto que rotará (puede ser la misma puerta)
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public bool openInward = false; // Si se abre hacia adentro o afuera

    [Header("Interaction")]
    public float interactionRange = 3f;
    public string lockedMessage = "La puerta está cerrada. Necesitas una llave.";
    public string openMessage = "Presiona E para abrir/cerrar";
    public string unlockMessage = "¡Puerta desbloqueada!";

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isAnimating = false;
    private PlayerController nearbyPlayer;

    void Start()
    {
        // Configurar rotaciones
        if (doorPivot == null)
            doorPivot = transform;

        closedRotation = doorPivot.rotation;

        float finalAngle = openInward ? -openAngle : openAngle;
        openRotation = closedRotation * Quaternion.Euler(0, 0, finalAngle);

        // Configurar trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            // Crear un trigger adicional para detección
            GameObject trigger = new GameObject("DoorTrigger");
            trigger.transform.SetParent(transform);
            trigger.transform.localPosition = Vector3.zero;

            BoxCollider triggerCol = trigger.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(interactionRange, 2f, interactionRange);

            DoorTrigger doorTrigger = trigger.AddComponent<DoorTrigger>();
            doorTrigger.parentDoor = this;
        }
    }

    void Update()
    {
        // Animar puerta
        if (isAnimating)
        {
            Quaternion targetRotation = isOpen ? openRotation : closedRotation;
            doorPivot.rotation = Quaternion.Slerp(doorPivot.rotation, targetRotation, openSpeed * Time.deltaTime);

            if (Quaternion.Angle(doorPivot.rotation, targetRotation) < 1f)
            {
                doorPivot.rotation = targetRotation;
                isAnimating = false;
            }
        }
    }

    public void OnPlayerEnter(PlayerController player)
    {
        nearbyPlayer = player;

        if (isLocked)
        {
            player.ShowInteractionMessage(lockedMessage);
        }
        else
        {
            player.ShowInteractionMessage(openMessage);
        }
    }

    public void OnPlayerExit(PlayerController player)
    {
        nearbyPlayer = null;
        player.HideInteractionMessage();
    }

    public void TryInteract(PlayerController player)
    {
        if (isLocked)
        {
            // Intentar usar llave
            if (player.HasKey(requiredKeyID))
            {
                UnlockDoor(player);
            }
            else
            {
                Debug.Log("No tienes la llave correcta!");
                player.ShowInteractionMessage("No tienes la llave correcta!");
            }
        }
        else
        {
            // Abrir/cerrar puerta
            ToggleDoor();
        }
    }

    void UnlockDoor(PlayerController player)
    {
        isLocked = false;
        player.UseKey(requiredKeyID); // Opcional: consumir la llave
        player.ShowInteractionMessage(unlockMessage);

        Debug.Log("¡Puerta desbloqueada con " + requiredKeyID + "!");

        // Auto-abrir después de desbloquear
        Invoke("ToggleDoor", 0.5f);
    }

    void ToggleDoor()
    {
        if (!isAnimating)
        {
            isOpen = !isOpen;
            isAnimating = true;

            Debug.Log("Puerta " + (isOpen ? "abierta" : "cerrada"));
        }
    }

    void OnDrawGizmosSelected()
    {
        // Mostrar rango de interacción
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(interactionRange, 2f, interactionRange));

        // Mostrar dirección de apertura
        Gizmos.color = isOpen ? Color.red : Color.blue;
        Vector3 direction = transform.right * (openInward ? -1 : 1);
        Gizmos.DrawRay(transform.position, direction * 2f);
    }
}