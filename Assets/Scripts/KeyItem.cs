using UnityEngine;
using UnityEngine.UI;

public class KeyItem : MonoBehaviour
{
    [Header("Key Settings")]
    public string keyID = "DefaultKey"; // ID único para cada llave
    public string keyName = "Llave Misteriosa";
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

    void Start()
    {
        startPosition = transform.position;
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Configurar collider como trigger (opcional, ya no es necesario para la interacción)
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
    }

    void Update()
    {
        if (isFloating && canPickup)
        {
            // Efecto de flotación
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);

            // Rotación
            if (rotateKey)
            {
                transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            }
        }

        // Sistema de interacción con distancia
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
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.PickupKey(this);
                PickupEffect();
            }
            else
            {
                Debug.LogWarning("No se encontró PlayerController en el jugador");
            }
        }
    }

    public void PickupEffect()
    {
        // Efecto visual/sonoro al recoger
        Debug.Log("Llave recogida: " + keyName);

        // Desactivar la llave
        canPickup = false;
        isFloating = false;
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
}