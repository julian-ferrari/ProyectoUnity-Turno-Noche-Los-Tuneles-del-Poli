using UnityEngine;
using UnityEngine.UI;

public class LockpickDoor : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private Transform teleportDestination;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float lockpickTime = 5f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Text promptText;
    [SerializeField] private Slider progressBar;

    private Transform player;
    private bool playerInRange = false;
    private bool isLockpicking = false;
    private float lockpickProgress = 0f;
    private InventoryManager inventoryManager;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        inventoryManager = FindFirstObjectByType<InventoryManager>();

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

        float distance = Vector3.Distance(transform.position, player.position);
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
    }

    void CancelLockpicking()
    {
        isLockpicking = false;
        lockpickProgress = 0f;

        if (progressBar != null)
            progressBar.gameObject.SetActive(false);

        ShowPrompt("Mantén E para forzar la cerradura");
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        if (teleportDestination != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(teleportDestination.position, 0.5f);
            Gizmos.DrawLine(transform.position, teleportDestination.position);
        }
    }
}