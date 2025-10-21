using UnityEngine;
using UnityEngine.UI;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    public string requiredKeyID = "DefaultKey";
    public bool isLocked = true;
    public bool isOpen = false;

    [Header("Door Animation")]
    public Transform doorPivot;
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public bool openInward = false;

    [Header("Manual Pivot Setup")]
    public Vector3 pivotOffset = new Vector3(-0.5f, 0, 0);

    [Header("Interaction")]
    public float interactionRange = 2f;
    public float triggerSize = 3f;
    public string lockedMessage = "La puerta está cerrada. Necesitas una llave.";
    public string openMessage = "Presiona E para abrir/cerrar";
    public string unlockMessage = "¡Puerta desbloqueada!";
    public string wrongKeyMessage = "Esta llave no abre esta puerta";

    [Header("Audio")]
    public AudioSource audioSource;
    [Space(5)]
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;
    public AudioClip doorUnlockSound;
    public AudioClip doorLockedSound;
    [Space(5)]
    public float doorSoundVolume = 0.7f;
    public float unlockSoundVolume = 0.8f;

    [Header("UI Message")]
    public Text feedbackText;
    public float messageDuration = 2f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 pivotWorldPosition;
    private bool isAnimating = false;
    private PlayerController nearbyPlayer;
    private Transform playerTransform;
    private bool doorUnlocked = false; // Para que la puerta permanezca desbloqueada

    void Start()
    {
        if (doorPivot == null)
            doorPivot = transform;

        originalPosition = doorPivot.position;
        originalRotation = doorPivot.rotation;

        AutoDetectPivot();
        pivotWorldPosition = doorPivot.TransformPoint(pivotOffset);

        SetupTrigger();
        SetupAudio();

        // Verificar si la puerta ya fue desbloqueada anteriormente
        CheckIfUnlocked();

        Debug.Log($"Puerta configurada - ID requerido: {requiredKeyID}, Distancia: {interactionRange}m");
    }

    void CheckIfUnlocked()
    {
        string saveKey = "Door_Unlocked_" + requiredKeyID + "_" + transform.position.ToString();
        if (PlayerPrefs.GetInt(saveKey, 0) == 1)
        {
            isLocked = false;
            doorUnlocked = true;
            Debug.Log("Puerta permanece desbloqueada: " + requiredKeyID);
        }
    }

    void SaveUnlockedState()
    {
        string saveKey = "Door_Unlocked_" + requiredKeyID + "_" + transform.position.ToString();
        PlayerPrefs.SetInt(saveKey, 1);
        PlayerPrefs.Save();
    }

    void SetupAudio()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 15f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        if (nearbyPlayer != null && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (distance > interactionRange)
            {
                nearbyPlayer.HideInteractionMessage();
            }
            else
            {
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
                Vector3 localMin = doorPivot.InverseTransformPoint(bounds.min);
                pivotOffset = new Vector3(localMin.x, 0, 0);
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
            triggerCol.size = new Vector3(triggerSize, 2f, triggerSize);

            DoorTrigger doorTrigger = trigger.AddComponent<DoorTrigger>();
            doorTrigger.parentDoor = this;
        }
    }

    public void OnPlayerEnter(PlayerController player)
    {
        nearbyPlayer = player;
        playerTransform = player.transform;

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
        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance > interactionRange)
        {
            Debug.Log($"Demasiado lejos. Distancia: {distance:F2}m");
            return;
        }

        if (isLocked)
        {
            // Verificar si el jugador tiene alguna llave
            InventoryManager inventory = InventoryManager.Instance;
            if (inventory == null)
            {
                ShowFeedback("Error: No se encontró el inventario");
                return;
            }

            // Verificar si tiene la llave correcta en el inventario
            if (inventory.HasKey(requiredKeyID))
            {
                // Tiene la llave correcta, verificar si la tiene equipada
                InventoryManager.InventoryItem selectedItem = inventory.GetSelectedItem();

                if (selectedItem == null || selectedItem.type != InventoryManager.ItemType.Key)
                {
                    // Tiene la llave pero no la tiene equipada
                    ShowFeedback("Equipa la llave correcta (presiona su número de slot)");
                    PlaySound(doorLockedSound, doorSoundVolume);
                    Debug.Log("Tienes la llave pero no está equipada");
                    return;
                }

                // Verificar que la llave equipada es la correcta
                if (selectedItem.itemID == requiredKeyID)
                {
                    UnlockDoor();
                }
                else
                {
                    ShowFeedback(wrongKeyMessage);
                    PlaySound(doorLockedSound, doorSoundVolume);
                    Debug.Log("Llave equipada incorrecta");
                }
            }
            else
            {
                // No tiene la llave correcta
                // Verificar si tiene alguna otra llave
                var collectedKeys = inventory.GetCollectedKeyIDs();
                if (collectedKeys.Count > 0)
                {
                    // Verificar si tiene alguna equipada
                    InventoryManager.InventoryItem selectedItem = inventory.GetSelectedItem();
                    if (selectedItem != null && selectedItem.type == InventoryManager.ItemType.Key)
                    {
                        ShowFeedback(wrongKeyMessage);
                    }
                    else
                    {
                        ShowFeedback("Equipa una llave para intentar abrir");
                    }
                }
                else
                {
                    ShowFeedback("Necesitas encontrar una llave para abrir esta puerta");
                }

                PlaySound(doorLockedSound, doorSoundVolume);
                Debug.Log("No tienes la llave correcta. Necesitas: " + requiredKeyID);
            }
        }
        else
        {
            ToggleDoor();
        }
    }

    void UnlockDoor()
    {
        isLocked = false;
        doorUnlocked = true;

        // Usar la llave (la consume del inventario)
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory != null)
        {
            inventory.UseKey(requiredKeyID);
        }

        // Guardar estado desbloqueado
        SaveUnlockedState();

        ShowFeedback(unlockMessage);
        PlaySound(doorUnlockSound, unlockSoundVolume);

        Debug.Log("¡Puerta desbloqueada con " + requiredKeyID + "!");

        Invoke("ToggleDoor", 0.5f);
    }

    void ToggleDoor()
    {
        if (!isAnimating)
        {
            isOpen = !isOpen;

            if (isOpen)
            {
                PlaySound(doorOpenSound, doorSoundVolume);
            }
            else
            {
                PlaySound(doorCloseSound, doorSoundVolume);
            }

            StartCoroutine(AnimateDoor());
        }
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    void ShowFeedback(string message)
    {
        // Intentar usar el Text local primero
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
            CancelInvoke("HideFeedback");
            Invoke("HideFeedback", messageDuration);
        }
        // Si no hay Text local, usar el sistema global
        else if (FeedbackUIManager.Instance != null)
        {
            FeedbackUIManager.Instance.ShowMessage(message, messageDuration);
        }
        else
        {
            Debug.LogWarning("No hay sistema de feedback disponible. Mensaje: " + message);
        }
    }

    void HideFeedback()
    {
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
    }

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
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
            ApplyDoorRotation(currentAngle);
            yield return null;
        }

        ApplyDoorRotation(endAngle);
        isAnimating = false;
    }

    void ApplyDoorRotation(float angle)
    {
        doorPivot.position = originalPosition;
        doorPivot.rotation = originalRotation;
        doorPivot.RotateAround(pivotWorldPosition, Vector3.up, angle);
    }

    void OnDrawGizmosSelected()
    {
        Transform pivot = doorPivot != null ? doorPivot : transform;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(triggerSize, 2f, triggerSize));

        Vector3 pivotPos = Application.isPlaying ? pivotWorldPosition : pivot.TransformPoint(pivotOffset);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(pivotPos, 0.15f);

        Gizmos.color = isOpen ? Color.red : Color.blue;
        Vector3 direction = pivot.right * (openInward ? -1 : 1);
        Gizmos.DrawRay(pivot.position, direction * 2f);
    }
}