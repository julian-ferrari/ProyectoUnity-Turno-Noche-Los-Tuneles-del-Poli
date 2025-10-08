using UnityEngine;
using UnityEngine.UI;

public class LockpickItem : MonoBehaviour
{
    [Header("Configuraci�n")]
    [SerializeField] private float pickupDistance = 2f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private float rotationSpeed = 50f;

    [Header("UI")]
    [SerializeField] private GameObject pickupPrompt;
    [SerializeField] private Text promptText;

    private Transform player;
    private bool playerInRange = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (pickupPrompt != null)
            pickupPrompt.SetActive(false);
    }

    void Update()
    {
        // Rotaci�n para hacerlo visible
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= pickupDistance;

        if (playerInRange)
        {
            ShowPrompt("Presiona E para recoger ganz�a");

            if (Input.GetKeyDown(pickupKey))
            {
                PickupLockpick();
            }
        }
        else
        {
            HidePrompt();
        }
    }

    void PickupLockpick()
    {
        InventoryManager inventory = FindFirstObjectByType<InventoryManager>();

        if (inventory != null)
        {
            inventory.AddLockpick();
            Debug.Log("Ganz�a recogida");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("No se encontr� InventoryManager en la escena");
        }
    }

    void ShowPrompt(string message)
    {
        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(true);
            if (promptText != null)
                promptText.text = message;
        }
    }

    void HidePrompt()
    {
        if (pickupPrompt != null)
            pickupPrompt.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupDistance);
    }
}