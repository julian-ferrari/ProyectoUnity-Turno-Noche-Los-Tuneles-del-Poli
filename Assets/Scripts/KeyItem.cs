using UnityEngine;
using UnityEngine.UI;

public class KeyItem : MonoBehaviour
{
    [Header("Key Settings")]
    public string keyID = "DefaultKey"; // ID único para cada llave
    public string keyName = "Llave Misteriosa";
    public Sprite keyIcon; // Imagen del icono de la llave para el inventario
    public GameObject keyModelPrefab; // Modelo 3D de la llave (opcional, para primera persona)
    public bool canPickup = true;

    [Header("Interaction Settings")]
    [SerializeField] private float pickupDistance = 2f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;

    [Header("Visual Effects")]
    public float floatHeight = 0.5f;
    public float floatSpeed = 2f;
    public bool rotateKey = true;
    public float rotationSpeed = 50f;

    [Header("UI")]
    [SerializeField] private GameObject pickupPrompt;
    [SerializeField] private Text promptText;

    private Vector3 startPosition;
    private bool isFloating = true;
    private Transform player;
    private bool playerInRange = false;
    private bool alreadyCollected = false;

    void Start()
    {
        startPosition = transform.position;
        player = GameObject.FindGameObjectWithTag("Player").transform;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (!gameObject.CompareTag("Key"))
        {
            gameObject.tag = "Key";
        }

        if (pickupPrompt != null)
            pickupPrompt.SetActive(false);

        // Verificar si ya fue recogida
        CheckIfAlreadyCollected();
    }

    void CheckIfAlreadyCollected()
    {
        string saveKey = "Key_Collected_" + keyID;
        if (PlayerPrefs.GetInt(saveKey, 0) == 1)
        {
            alreadyCollected = true;
            gameObject.SetActive(false);
            Debug.Log("Llave ya fue recogida: " + keyID);
        }
    }

    void Update()
    {
        if (alreadyCollected) return;

        if (isFloating && canPickup)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);

            if (rotateKey)
            {
                transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            }
        }

        if (player != null && canPickup)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            playerInRange = distance <= pickupDistance;

            if (playerInRange)
            {
                ShowPrompt("Presiona E para recoger " + keyName);

                if (Input.GetKeyDown(pickupKey))
                {
                    TryPickup();
                }
            }
            else
            {
                HidePrompt();
            }
        }
    }

    void TryPickup()
    {
        if (player != null)
        {
            InventoryManager inventory = FindFirstObjectByType<InventoryManager>();
            if (inventory != null)
            {
                inventory.AddKey(this);
                PickupEffect();
            }
            else
            {
                Debug.LogWarning("No se encontró InventoryManager");
            }
        }
    }

    public void PickupEffect()
    {
        Debug.Log("Llave recogida: " + keyName + " (ID: " + keyID + ")");

        // Marcar como recogida permanentemente
        string saveKey = "Key_Collected_" + keyID;
        PlayerPrefs.SetInt(saveKey, 1);
        PlayerPrefs.Save();

        canPickup = false;
        isFloating = false;
        alreadyCollected = true;
        HidePrompt();
        gameObject.SetActive(false);
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupDistance);
    }

    // Método público para resetear la llave (útil para debugging)
    public void ResetKey()
    {
        canPickup = true;
        isFloating = true;
        alreadyCollected = false;
        gameObject.SetActive(true);

        // Borrar PlayerPrefs
        string saveKey = "Key_Collected_" + keyID;
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();

        Debug.Log($"Llave reseteada: {keyName} ({keyID})");
    }
}