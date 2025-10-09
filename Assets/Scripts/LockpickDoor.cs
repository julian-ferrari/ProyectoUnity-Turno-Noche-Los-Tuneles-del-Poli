using UnityEngine;
using UnityEngine.UI;

public class LockpickDoor : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private Transform teleportDestination;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float lockpickTime = 5f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Text promptText;
    [SerializeField] private Slider progressBar;

    [Header("Sonido")]
    [SerializeField] private AudioClip lockpickingSound;
    [SerializeField] private float soundVolume = 1f;

    private Transform player;
    private bool playerInRange = false;
    private bool isLockpicking = false;
    private float lockpickProgress = 0f;
    private InventoryManager inventoryManager;
    private AudioSource audioSource;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        inventoryManager = FindFirstObjectByType<InventoryManager>();

        // Configurar AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = soundVolume;
        if (lockpickingSound != null)
            audioSource.clip = lockpickingSound;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            progressBar.minValue = 0;
            progressBar.maxValue = lockpickTime;
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector3 checkPosition = interactionPoint != null ? interactionPoint.position : transform.position;
        float distance = Vector3.Distance(checkPosition, player.position);
        playerInRange = distance <= interactionDistance;

        if (playerInRange)
        {
            if (!isLockpicking)
            {
                if (inventoryManager != null && inventoryManager.HasLockpick())
                {
                    ShowPrompt("Mantén E para forzar la cerradura");

                    if (Input.GetKeyDown(interactionKey))
                    {
                        StartLockpicking();
                    }
                }
                else
                {
                    ShowPrompt("Necesitas una ganzúa");
                }
            }
            else
            {
                if (Input.GetKey(interactionKey))
                {
                    lockpickProgress += Time.deltaTime;
                    UpdateProgressBar();

                    if (lockpickProgress >= lockpickTime)
                    {
                        UnlockDoor();
                    }
                }
                else
                {
                    CancelLockpicking();
                }
            }
        }
        else
        {
            if (isLockpicking)
                CancelLockpicking();
            HidePrompt();
        }
    }

    void StartLockpicking()
    {
        isLockpicking = true;
        lockpickProgress = 0f;

        if (progressBar != null)
            progressBar.gameObject.SetActive(true);

        ShowPrompt("Forzando cerradura...");

        // Reproducir sonido
        if (audioSource != null && lockpickingSound != null)
            audioSource.Play();
    }

    void CancelLockpicking()
    {
        isLockpicking = false;
        lockpickProgress = 0f;

        if (progressBar != null)
            progressBar.gameObject.SetActive(false);

        ShowPrompt("Mantén E para forzar la cerradura");

        // Detener sonido
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    void UpdateProgressBar()
    {
        if (progressBar != null)
            progressBar.value = lockpickProgress;
    }

    void UnlockDoor()
    {
        isLockpicking = false;

        if (progressBar != null)
            progressBar.gameObject.SetActive(false);

        // Detener sonido
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        TeleportPlayer();

        if (inventoryManager != null)
            inventoryManager.UseLockpick();
    }

    void TeleportPlayer()
    {
        if (teleportDestination != null && player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();

            if (cc != null)
            {
                cc.enabled = false;
                player.position = teleportDestination.position;
                player.rotation = teleportDestination.rotation;
                cc.enabled = true;
            }
            else
            {
                player.position = teleportDestination.position;
                player.rotation = teleportDestination.rotation;
            }

            Debug.Log("Puerta desbloqueada - Jugador teletransportado");
        }
    }

    void ShowPrompt(string message)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
            if (promptText != null)
                promptText.text = message;
        }
    }

    void HidePrompt()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 checkPosition = interactionPoint != null ? interactionPoint.position : transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(checkPosition, interactionDistance);

        if (teleportDestination != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(teleportDestination.position, 0.5f);
            Gizmos.DrawLine(checkPosition, teleportDestination.position);
        }
    }
}