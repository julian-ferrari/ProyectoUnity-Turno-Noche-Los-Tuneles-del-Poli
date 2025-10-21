using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [System.Serializable]
    public class InventoryItem
    {
        public string itemName;
        public string itemID; // ID único (para llaves)
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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            inventorySlots.Add(null);
        }

        hasFlashlight = true;
        inventorySlots[0] = flashlightItem;

        UpdateUI();

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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
        {
            DeselectItem();
        }
    }

    // =================== SISTEMA DE LLAVES ===================

    public void AddKey(KeyItem keyScript)
    {
        string keyID = keyScript.keyID;

        // Verificar si ya tenemos esta llave
        if (collectedKeys.ContainsKey(keyID))
        {
            Debug.Log("Ya tienes esta llave: " + keyScript.keyName);
            return;
        }

        // Crear item de inventario para la llave
        InventoryItem keyItem = new InventoryItem
        {
            itemName = keyScript.keyName,
            itemID = keyScript.keyID,
            itemIcon = keyScript.keyIcon,
            itemModelPrefab = keyScript.keyModelPrefab,
            type = ItemType.Key
        };

        // Añadir a la colección de llaves
        collectedKeys.Add(keyID, keyItem);

        // Añadir al primer slot vacío
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

        // Remover del inventario
        RemoveItemByID(keyID);

        // Remover del diccionario
        collectedKeys.Remove(keyID);

        Debug.Log("Llave usada y consumida: " + keyItem.itemName);
    }

    private void RemoveItemByID(string itemID)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] != null &&
                inventorySlots[i].itemID == itemID)
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
        // No consumir ganzúa
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

        selectedSlotIndex = index;
        UpdateUI();
        EquipItem(inventorySlots[index]);
        Debug.Log("Equipado: " + inventorySlots[index].itemName);
    }

    public void DeselectItem()
    {
        selectedSlotIndex = -1;
        UpdateUI();
        UnequipCurrentItem();
    }

    private void EquipItem(InventoryItem item)
    {
        UnequipCurrentItem();

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
            currentFPItem = Instantiate(item.itemModelPrefab, fpItemHolder);
        }
    }

    private void UnequipCurrentItem()
    {
        if (flashlightController != null)
        {
            flashlightController.SetFlashlightVisibility(false);
        }

        if (currentFPItem != null)
        {
            Destroy(currentFPItem);
            currentFPItem = null;
        }
    }

    void UpdateUI()
    {
        if (slotImages == null || slotImages.Length != 5) return;

        for (int i = 0; i < 5; i++)
        {
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

    // Para scripts antiguos que usan HasKey() sin parámetro
    public bool HasKey()
    {
        return collectedKeys.Count > 0;
    }

    // Métodos para sistema de guardado
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
        // Este método es legacy, ahora usamos el diccionario de llaves
        // Lo dejamos vacío para compatibilidad
    }

    public void SetHasFlashlight(bool value)
    {
        hasFlashlight = value;
    }

    public void ForceUpdateUI()
    {
        UpdateUI();
    }

    // Variable pública para acceso desde GameStateManager
    public InventoryItem keyItem
    {
        get
        {
            // Retornar el primer item de tipo Key que encontremos
            foreach (var item in inventorySlots)
            {
                if (item != null && item.type == ItemType.Key)
                    return item;
            }
            return null;
        }
    }
}