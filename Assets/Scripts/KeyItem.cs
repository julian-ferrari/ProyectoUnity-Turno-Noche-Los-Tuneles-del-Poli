using UnityEngine;

public class KeyItem : MonoBehaviour
{
    [Header("Key Settings")]
    public string keyID = "DefaultKey"; // ID único para cada llave
    public string keyName = "Llave Misteriosa";
    public bool canPickup = true;

    [Header("Visual Effects")]
    public float floatHeight = 0.5f;
    public float floatSpeed = 2f;
    public bool rotateKey = true;
    public float rotationSpeed = 50f;

    private Vector3 startPosition;
    private bool isFloating = true;

    void Start()
    {
        startPosition = transform.position;

        // Configurar para que sea trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (!gameObject.CompareTag("Key"))
        {
            gameObject.tag = "Key";
        }
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
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && canPickup)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.PickupKey(this);
                PickupEffect();
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
        gameObject.SetActive(false);
    }
}