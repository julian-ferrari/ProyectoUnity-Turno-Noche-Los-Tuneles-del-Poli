using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [System.Serializable]
    public class InventoryItem
    {
        public string itemName;
        public string itemID;
        public Sprite itemIcon;
        public GameObject itemModelPrefab;
        public ItemType type;
    }

    public enum ItemType
    {
        None,
        Flashlight,
        Key,
        Lockpick
    }

    [Header("Items disponibles")]
    public InventoryItem flashlightItem;
    public InventoryItem lockpickItem;

    [Header("Inventario actual (5 slots)")]
    public List<InventoryItem> inventorySlots = new List<InventoryItem>(5);

    [Header("UI Slots")]
    public Image[] slotImages;
    public GameObject[] slotHighlights;
    public Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Header("Selección")]
    public int selectedSlotIndex = -1;

    [Header("Primera Persona")]
    public Transform fpItemHolder;
    private GameObject currentFPItem;

    [Header("Referencias")]
    public FlashlightFPSController flashlightController;

    // Contadores y colecciones
    private int lockpickCount = 0;
    private bool hasFlashlight = false;
    private Dictionary<string, InventoryItem> collectedKeys = new Dictionary<string, InventoryItem>();

    // Flag para saber si ya fue inicializado
    private bool isInitialized = false;
    private string currentScene = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("InventoryManager: Nueva instancia creada");
        }
        else
        {
            Debug.Log("InventoryManager: Instancia duplicada destruida");
            Destroy(gameObject);
            return;
        }

        // Suscribirse al evento de cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"InventoryManager: Escena cargada: {scene.name}");

        // Si cargamos el menú principal, destruir la instancia para empezar limpio
        if (scene.name == "MainMenu")
        {
            Debug.Log("InventoryManager: Retornando al MainMenu - Destruyendo instancia");
            if (Instance == this)
            {
                Instance = null;
            }
            Destroy(gameObject);
            return;
        }

        // Si es la escena del juego y no estamos cargando desde save, resetear
        if (scene.name == "PoliNights" && !GameStateManager.isLoadingFromSave)
        {
            Debug.Log("InventoryManager: Nueva partida detectada - Reseteando inventario");
            ResetInventory();
        }

        // Actualizar referencias de UI
        currentScene = scene.name;
        StartCoroutine(InitializeAfterSceneLoad());
    }

    System.Collections.IEnumerator InitializeAfterSceneLoad()
    {
        // Esperar un frame para que todo se cargue
        yield return new WaitForEndOfFrame();

        // Buscar referencias de UI en la nueva escena
        FindUIReferences();

        // Si no está inicializado, hacerlo ahora
        if (!isInitialized && currentScene == "PoliNights")
        {
            InitializeInventory();
        }

        // Actualizar UI
        UpdateUI();
    }

    void Start()
    {
        if (currentScene == "PoliNights" && !isInitialized)
        {
            InitializeInventory();
        }
    }

    void InitializeInventory()
    {
        Debug.Log("InventoryManager: Inicializando inventario...");

        // Asegurar que tenemos 5 slots
        inventorySlots.Clear();
        for (int i = 0; i < 5; i++)
        {
            inventorySlots.Add(null);
        }

        // Agregar linterna por defecto
        hasFlashlight = true;
        inventorySlots[0] = flashlightItem;

        // Seleccionar automáticamente el slot 0 (linterna) al inicio
        selectedSlotIndex = 0;

        // Buscar referencias
        FindReferences();

        // Actualizar UI
        UpdateUI();

        // Equipar la linterna al inicio
        if (flashlightController != null)
        {
            flashlightController.SetFlashlightVisibility(true);
        }

        isInitialized = true;
        Debug.Log("InventoryManager: Inventario inicializado correctamente");
    }

    void FindReferences()
    {
        if (fpItemHolder == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                fpItemHolder = mainCam.transform;
            }
        }

        if (flashlightController == null)
        {
            flashlightController = FindFirstObjectByType<FlashlightFPSController>();
        }
    }

    void FindUIReferences()
    {
        // Buscar los slots de UI en la escena actual
        GameObject inventoryUI = GameObject.Find("InventoryUI");
        if (inventoryUI != null)
        {
            Transform slotsParent = inventoryUI.transform.Find("Slots");
            if (slotsParent != null)
            {
                // Reinicializar arrays
                slotImages = new Image[5];
                slotHighlights = new GameObject[5];

                for (int i = 0; i < 5; i++)
                {
                    Transform slot = slotsParent.Find($"Slot{i + 1}");
                    if (slot != null)
                    {
                        slotImages[i] = slot.Find("ItemIcon")?.GetComponent<Image>();
                        slotHighlights[i] = slot.Find("Highlight")?.gameObject;
                    }
                }

                Debug.Log("InventoryManager: Referencias de UI encontradas");
            }
        }
    }

    /// <summary>
    /// Resetea completamente el inventario (para nueva partida)
    /// </summary>
    public void ResetInventory()
    {
        Debug.Log("InventoryManager: RESET COMPLETO del inventario");

        // Limpiar todo
        inventorySlots.Clear();
        collectedKeys.Clear();
        lockpickCount = 0;
        hasFlashlight = false;
        selectedSlotIndex = -1;

        // Destruir item equipado
        if (currentFPItem != null)
        {
            Destroy(currentFPItem);
            currentFPItem = null;
        }

        // Marcar como no inicializado para que Start() lo inicialice
        isInitialized = false;

        Debug.Log("InventoryManager: Reset completado");
    }

    void Update()
    {
        if (!isInitialized) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
    }

    // =================== SISTEMA DE LLAVES ===================

    public void AddKey(KeyItem keyScript)
    {
        string keyID = keyScript.keyID;

        if (collectedKeys.ContainsKey(keyID))
        {
            Debug.Log("Ya tienes esta llave: " + keyScript.keyName);
            return;
        }

        InventoryItem keyItem = new InventoryItem
        {
            itemName = keyScript.keyName,
            itemID = keyScript.keyID,
            itemIcon = keyScript.keyIcon,
            itemModelPrefab = keyScript.keyModelPrefab,
            type = ItemType.Key
        };

        collectedKeys.Add(keyID, keyItem);
        AddItemToFirstEmptySlot(keyItem);

        Debug.Log("Llave agregada al inventario: " + keyScript.keyName + " (ID: " + keyID + ")");
    }

    public bool HasKey(string keyID)
    {
        return collectedKeys.ContainsKey(keyID);
    }

    public void UseKey(string keyID)
    {
        if (!collectedKeys.ContainsKey(keyID))
        {
            Debug.LogWarning("Intentando usar llave que no existe: " + keyID);
            return;
        }

        InventoryItem keyItem = collectedKeys[keyID];
        RemoveItemByID(keyID);
        collectedKeys.Remove(keyID);

        Debug.Log("Llave usada y consumida: " + keyItem.itemName);
    }

    private void RemoveItemByID(string itemID)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] != null && inventorySlots[i].itemID == itemID)
            {
                inventorySlots[i] = null;

                if (selectedSlotIndex == i)
                {
                    DeselectItem();
                }

                UpdateUI();
                Debug.Log("Item removido del slot " + i);
                return;
            }
        }
    }

    // =================== SISTEMA DE GANZÚAS ===================

    public void AddLockpick()
    {
        if (lockpickCount == 0)
        {
            lockpickCount = 1;
            AddItemToFirstEmptySlot(lockpickItem);
            Debug.Log("Ganzúa agregada al inventario (permanente)");
        }
        UpdateUI();
    }

    public bool HasLockpick() => lockpickCount > 0;

    public void UseLockpick()
    {
        Debug.Log("Usando ganzúa (no se consume)");
    }

    public int GetLockpickCount() => lockpickCount;

    // =================== SISTEMA DE LINTERNA ===================

    public void AddFlashlight()
    {
        if (hasFlashlight) return;
        hasFlashlight = true;
        AddItemToFirstEmptySlot(flashlightItem);
    }

    public bool HasFlashlight() => hasFlashlight;

    // =================== GESTIÓN DE SLOTS ===================

    private void AddItemToFirstEmptySlot(InventoryItem item)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] == null)
            {
                inventorySlots[i] = item;
                UpdateUI();
                return;
            }
        }
        Debug.LogWarning("Inventario lleno!");
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= inventorySlots.Count) return;

        if (inventorySlots[index] == null)
        {
            DeselectItem();
            return;
        }

        if (selectedSlotIndex == index)
        {
            DeselectItem();
            return;
        }

        // Si estamos cambiando DESDE la linterna A otro item
        if (selectedSlotIndex >= 0 &&
            inventorySlots[selectedSlotIndex] != null &&
            inventorySlots[selectedSlotIndex].type == ItemType.Flashlight)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null && player.flashlight != null)
            {
                player.flashlight.enabled = false;
            }

            if (flashlightController != null)
            {
                flashlightController.SetFlashlightVisibility(false);
            }
        }

        selectedSlotIndex = index;
        UpdateUI();
        EquipItem(inventorySlots[index]);
        Debug.Log("Equipado: " + inventorySlots[index].itemName);
    }

    public void DeselectItem()
    {
        InventoryItem previousItem = null;
        if (selectedSlotIndex >= 0 && selectedSlotIndex < inventorySlots.Count)
        {
            previousItem = inventorySlots[selectedSlotIndex];
        }

        selectedSlotIndex = -1;
        UpdateUI();

        if (previousItem != null && previousItem.type == ItemType.Flashlight)
        {
            if (flashlightController != null)
            {
                flashlightController.SetFlashlightVisibility(false);
            }

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null && player.flashlight != null)
            {
                player.flashlight.enabled = false;
            }
        }

        UnequipCurrentItem();
    }

    private void EquipItem(InventoryItem item)
    {
        if (currentFPItem != null)
        {
            currentFPItem.SetActive(false);
            currentFPItem = null;
        }

        if (item == null) return;

        if (item.type == ItemType.Flashlight)
        {
            if (flashlightController != null)
            {
                flashlightController.SetFlashlightVisibility(true);
            }
            return;
        }

        if (item.itemModelPrefab != null && fpItemHolder != null)
        {
            Transform existingModel = fpItemHolder.Find(item.itemModelPrefab.name);

            if (existingModel != null)
            {
                currentFPItem = existingModel.gameObject;
                currentFPItem.SetActive(true);
                Debug.Log($"Modelo encontrado y activado: {item.itemName}");
            }
            else
            {
                currentFPItem = Instantiate(item.itemModelPrefab, fpItemHolder);
                Debug.Log($"Modelo instanciado: {item.itemName}");
            }
        }
    }

    private void UnequipCurrentItem()
    {
        if (currentFPItem != null)
        {
            currentFPItem.SetActive(false);
            currentFPItem = null;
        }
    }

    void UpdateUI()
    {
        if (slotImages == null || slotImages.Length != 5)
        {
            Debug.LogWarning("InventoryManager: slotImages no está configurado correctamente");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            if (slotImages[i] == null) continue;

            InventoryItem item = inventorySlots[i];

            if (item != null && item.itemIcon != null)
            {
                slotImages[i].sprite = item.itemIcon;
                slotImages[i].color = Color.white;
            }
            else
            {
                slotImages[i].sprite = null;
                slotImages[i].color = emptySlotColor;
            }

            if (slotHighlights != null && i < slotHighlights.Length && slotHighlights[i] != null)
            {
                slotHighlights[i].SetActive(i == selectedSlotIndex);
            }
        }
    }

    // =================== GETTERS ===================

    public InventoryItem GetSelectedItem()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < inventorySlots.Count)
        {
            return inventorySlots[selectedSlotIndex];
        }
        return null;
    }

    public ItemType GetSelectedItemType()
    {
        InventoryItem item = GetSelectedItem();
        return item != null ? item.type : ItemType.None;
    }

    // =================== GUARDADO ===================

    public void ClearInventory()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            inventorySlots[i] = null;
        }
        DeselectItem();
        lockpickCount = 0;
        hasFlashlight = false;
        collectedKeys.Clear();
        UpdateUI();
    }

    public List<string> GetCollectedKeyIDs()
    {
        return new List<string>(collectedKeys.Keys);
    }

    // =================== MÉTODOS DE COMPATIBILIDAD ===================

    public bool HasKey()
    {
        return collectedKeys.Count > 0;
    }

    public void SetItemAtSlot(int slotIndex, InventoryItem item)
    {
        if (slotIndex >= 0 && slotIndex < inventorySlots.Count)
        {
            inventorySlots[slotIndex] = item;
        }
    }

    public void SetLockpickCount(int count)
    {
        lockpickCount = count;
    }

    public void SetHasKey(bool value)
    {
        // Legacy - no hace nada
    }

    public void SetHasFlashlight(bool value)
    {
        hasFlashlight = value;
    }

    public void ForceUpdateUI()
    {
        UpdateUI();
    }

    public InventoryItem keyItem
    {
        get
        {
            foreach (var item in inventorySlots)
            {
                if (item != null && item.type == ItemType.Key)
                    return item;
            }
            return null;
        }
    }
}