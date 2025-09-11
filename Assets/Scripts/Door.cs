using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    public string requiredKeyID = "DefaultKey";
    public bool isLocked = true;
    public bool isOpen = false;

    [Header("Door Animation")]
    public Transform doorPivot; // Arrastra aquí la puerta
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public bool openInward = false;

    [Header("Manual Pivot Setup")]
    [Tooltip("Ajusta estos valores si la puerta se posiciona mal")]
    public Vector3 pivotOffset = new Vector3(-0.5f, 0, 0); // Donde están las bisagras

    [Header("Interaction")]
    public float interactionRange = 1.5f;
    public string lockedMessage = "La puerta está cerrada. Necesitas una llave.";
    public string openMessage = "Presiona E para abrir/cerrar";
    public string unlockMessage = "¡Puerta desbloqueada!";

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 pivotWorldPosition;
    private bool isAnimating = false;
    private PlayerController nearbyPlayer;

    void Start()
    {
        if (doorPivot == null)
            doorPivot = transform;

        // Guardar estado original
        originalPosition = doorPivot.position;
        originalRotation = doorPivot.rotation;

        // Auto-detectar pivot basado en el bounds del objeto
        AutoDetectPivot();

        // Calcular posición del pivot en coordenadas del mundo
        pivotWorldPosition = doorPivot.TransformPoint(pivotOffset);

        SetupTrigger();

        Debug.Log($"Puerta configurada - Pivot en: {pivotWorldPosition}");
    }

    void AutoDetectPivot()
    {
        if (doorPivot != null)
        {
            Renderer rend = doorPivot.GetComponent<Renderer>();
            if (rend != null)
            {
                Bounds bounds = rend.bounds;

                // Convertir bounds a espacio local
                Vector3 localMin = doorPivot.InverseTransformPoint(bounds.min);
                Vector3 localMax = doorPivot.InverseTransformPoint(bounds.max);

                // Determinar qué lado es más cercano al centro para el pivot
                // Por defecto usamos el lado izquierdo (menor X)
                pivotOffset = new Vector3(localMin.x, 0, 0);

                Debug.Log($"Pivot auto-detectado: {pivotOffset}");
            }
        }
    }

    void SetupTrigger()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
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
            ToggleDoor();
        }
    }

    void UnlockDoor(PlayerController player)
    {
        isLocked = false;
        player.UseKey(requiredKeyID);
        player.ShowInteractionMessage(unlockMessage);

        Debug.Log("¡Puerta desbloqueada con " + requiredKeyID + "!");

        Invoke("ToggleDoor", 0.5f);
    }

    void ToggleDoor()
    {
        if (!isAnimating)
        {
            isOpen = !isOpen;
            StartCoroutine(AnimateDoor());
        }
    }

    // MÉTODO CORREGIDO - SIN ACUMULACIÓN DE ROTACIONES
    System.Collections.IEnumerator AnimateDoor()
    {
        isAnimating = true;

        float startAngle = isOpen ? 0f : openAngle * (openInward ? -1f : 1f);
        float endAngle = isOpen ? openAngle * (openInward ? -1f : 1f) : 0f;

        float elapsedTime = 0f;
        float duration = 1f / openSpeed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);

            // Calcular el ángulo actual sin acumulación
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);

            // Aplicar la rotación desde la posición original
            ApplyDoorRotation(currentAngle);

            yield return null;
        }

        // Aplicar rotación final
        ApplyDoorRotation(endAngle);

        isAnimating = false;
        Debug.Log("Puerta " + (isOpen ? "abierta" : "cerrada") + " - Ángulo: " + endAngle);
    }

    void ApplyDoorRotation(float angle)
    {
        // Resetear a posición y rotación original
        doorPivot.position = originalPosition;
        doorPivot.rotation = originalRotation;

        // Aplicar rotación alrededor del pivot
        doorPivot.RotateAround(pivotWorldPosition, Vector3.up, angle);
    }

    void OnDrawGizmosSelected()
    {
        Transform pivot = doorPivot != null ? doorPivot : transform;

        // Mostrar rango de interacción
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(interactionRange, 2f, interactionRange));

        // Mostrar punto de pivot
        Vector3 pivotPos = Application.isPlaying ? pivotWorldPosition : pivot.TransformPoint(pivotOffset);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(pivotPos, 0.15f);

        // Mostrar dirección de apertura
        Gizmos.color = isOpen ? Color.red : Color.blue;
        Vector3 direction = pivot.right * (openInward ? -1 : 1);
        Gizmos.DrawRay(pivot.position, direction * 2f);

        // Mostrar línea del pivot
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pivot.position, pivotPos);
    }

    // MÉTODOS PARA CONFIGURAR PIVOT FÁCILMENTE
    [ContextMenu("Auto-detectar Pivot Izquierdo")]
    void SetPivotLeft()
    {
        if (doorPivot != null)
        {
            Renderer rend = doorPivot.GetComponent<Renderer>();
            if (rend != null)
            {
                Bounds bounds = rend.bounds;
                Vector3 localBounds = doorPivot.InverseTransformPoint(bounds.min);
                pivotOffset = new Vector3(localBounds.x, 0, 0);
                Debug.Log("Pivot configurado a la izquierda: " + pivotOffset);

                // Recalcular pivot world position
                if (Application.isPlaying)
                {
                    pivotWorldPosition = doorPivot.TransformPoint(pivotOffset);
                }
            }
        }
    }

    [ContextMenu("Auto-detectar Pivot Derecho")]
    void SetPivotRight()
    {
        if (doorPivot != null)
        {
            Renderer rend = doorPivot.GetComponent<Renderer>();
            if (rend != null)
            {
                Bounds bounds = rend.bounds;
                Vector3 localBounds = doorPivot.InverseTransformPoint(bounds.max);
                pivotOffset = new Vector3(localBounds.x, 0, 0);
                Debug.Log("Pivot configurado a la derecha: " + pivotOffset);

                // Recalcular pivot world position
                if (Application.isPlaying)
                {
                    pivotWorldPosition = doorPivot.TransformPoint(pivotOffset);
                }
            }
        }
    }

    [ContextMenu("Resetear Puerta")]
    void ResetDoor()
    {
        if (Application.isPlaying)
        {
            doorPivot.position = originalPosition;
            doorPivot.rotation = originalRotation;
            isOpen = false;
            isAnimating = false;
            Debug.Log("Puerta reseteada a posición original");
        }
    }

    [ContextMenu("Reconfigurar Trigger")]
    void ReconfigureTrigger()
    {
        SetupTrigger();
        Debug.Log($"Trigger reconfigurado con rango: {interactionRange}");
    }

    [ContextMenu("Debug Info")]
    void DebugInfo()
    {
        Transform triggerChild = transform.Find("DoorTrigger");
        if (triggerChild != null)
        {
            BoxCollider triggerCol = triggerChild.GetComponent<BoxCollider>();
            if (triggerCol != null)
            {
                Debug.Log($"Trigger actual - Size: {triggerCol.size}, IsTrigger: {triggerCol.isTrigger}");
            }
        }
        else
        {
            Debug.Log("No se encontró DoorTrigger child object");
        }
        Debug.Log($"InteractionRange configurado: {interactionRange}");
    }
}