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
    [Tooltip("Distancia máxima para interactuar con la puerta")]
    public float interactionRange = 2f;
    [Tooltip("Tamaño del trigger de detección (recomendado: un poco más grande que interactionRange)")]
    public float triggerSize = 3f;
    public string lockedMessage = "La puerta está cerrada. Necesitas una llave.";
    public string openMessage = "Presiona E para abrir/cerrar";
    public string unlockMessage = "¡Puerta desbloqueada!";

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 pivotWorldPosition;
    private bool isAnimating = false;
    private PlayerController nearbyPlayer;
    private Transform playerTransform;

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

        Debug.Log($"Puerta configurada - Pivot en: {pivotWorldPosition}, Distancia interacción: {interactionRange}m");
    }

    void Update()
    {
        // Verificar distancia constantemente si hay un jugador cerca
        if (nearbyPlayer != null && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            // Si el jugador se aleja más de la distancia de interacción
            if (distance > interactionRange)
            {
                nearbyPlayer.HideInteractionMessage();
            }
            else
            {
                // Mostrar mensaje apropiado según el estado
                if (isLocked)
                {
                    nearbyPlayer.ShowInteractionMessage(lockedMessage);
                }
                else
                {
                    nearbyPlayer.ShowInteractionMessage(openMessage);
                }
            }
        }
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
            // Usar triggerSize en lugar de interactionRange para el área de detección
            triggerCol.size = new Vector3(triggerSize, 2f, triggerSize);

            DoorTrigger doorTrigger = trigger.AddComponent<DoorTrigger>();
            doorTrigger.parentDoor = this;
        }
    }

    public void OnPlayerEnter(PlayerController player)
    {
        nearbyPlayer = player;
        playerTransform = player.transform;

        // Verificar distancia real antes de mostrar mensaje
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= interactionRange)
        {
            if (isLocked)
            {
                player.ShowInteractionMessage(lockedMessage);
            }
            else
            {
                player.ShowInteractionMessage(openMessage);
            }
        }
    }

    public void OnPlayerExit(PlayerController player)
    {
        nearbyPlayer = null;
        playerTransform = null;
        player.HideInteractionMessage();
    }

    public void TryInteract(PlayerController player)
    {
        // Verificar distancia real antes de permitir interacción
        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance > interactionRange)
        {
            Debug.Log($"Demasiado lejos para interactuar. Distancia: {distance:F2}m, Máximo: {interactionRange}m");
            return;
        }

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

        // Mostrar rango REAL de interacción (verde)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        // Mostrar área del trigger (amarillo transparente)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(triggerSize, 2f, triggerSize));

        // Mostrar punto de pivot
        Vector3 pivotPos = Application.isPlaying ? pivotWorldPosition : pivot.TransformPoint(pivotOffset);
        Gizmos.color = Color.red;
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
        Debug.Log($"Trigger reconfigurado - Trigger size: {triggerSize}, Interaction range: {interactionRange}");
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
        Debug.Log($"Interaction Range: {interactionRange}m, Trigger Size: {triggerSize}m");
    }
}