using UnityEngine;
using UnityEngine.UI;

public class SimpleDoor : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private Transform teleportDestination;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Text promptText;

    private Transform player;
    private bool playerInRange = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionDistance;

        if (playerInRange)
        {
            ShowPrompt("Presiona E para abrir");

            if (Input.GetKeyDown(interactionKey))
            {
                TeleportPlayer();
            }
        }
        else
        {
            HidePrompt();
        }
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

            Debug.Log("Jugador teletransportado a través de la reja");
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        if (teleportDestination != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(teleportDestination.position, 0.5f);
            Gizmos.DrawLine(transform.position, teleportDestination.position);
        }
    }
}