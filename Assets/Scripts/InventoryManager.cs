using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    [System.Serializable]
    public class InventoryItem
    {
        public string itemName;
        public Sprite itemIcon;
        public GameObject itemModelPrefab; // El prefab del holder (hijo de cámara)
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
    public InventoryItem keyItem;
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
    public Transform fpItemHolder; // Main Camera
    private GameObject currentFPItem;

    [Header("Referencias")]
    public FlashlightFPSController flashlightController;

    // Contadores
    private int lockpickCount = 0;
    private bool hasKey = false;
    private bool hasFlashlight = false;

    void Start()
    {
        // Inicializar slots
        for (int i = 0; i < 5; i++)
        {
            inventorySlots.Add(null);
        }

        hasFlashlight = true;
        inventorySlots[0] = flashlightItem;

        UpdateUI();

        // Buscar Main Camera
        if (fpItemHolder == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                fpItemHolder = mainCam.transform;
                Debug.Log("? Main Camera encontrada como fpItemHolder");
            }
        }

        // Buscar FlashlightFPSController
        if (flashlightController == null)
        {
            flashlightController = FindFirstObjectByType<FlashlightFPSController>();
        }
    }

    void Update()
    {
        // Selección con teclas numéricas
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);

        // Deseleccionar
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
        {
            DeselectItem();
        }
    }

    public void AddFlashlight()
    {
        if (hasFlashlight) return;
        hasFlashlight = true;
        AddItemToFirstEmptySlot(flashlightItem);
    }

    public void AddKey(KeyItem keyScript)
    {
        if (hasKey) return;
        hasKey = true;
        AddItemToFirstEmptySlot(keyItem);
        Debug.Log("Llave agregada: " + keyScript.keyName);
    }

    public void AddLockpick()
    {
        lockpickCount++;
        if (lockpickCount == 1)
        {
            AddItemToFirstEmptySlot(lockpickItem);
        }
        UpdateUI();
    }

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

        if (item == null)
        {
            Debug.LogError("? EquipItem: item es NULL");
            return;
        }

        Debug.Log("=== EQUIP: " + item.itemName + " ===");
        Debug.Log("Type: " + item.type);
        Debug.Log("Prefab: " + (item.itemModelPrefab != null ? item.itemModelPrefab.name : "NULL"));

        // Linterna: usar el controlador especial
        if (item.type == ItemType.Flashlight)
        {
            Debug.Log("Es LINTERNA");
            if (flashlightController != null)
            {
                flashlightController.SetFlashlightVisibility(true);
                Debug.Log("? Linterna visible");
            }
            else
            {
                Debug.LogError("? flashlightController es NULL");
            }
            return;
        }

        // Otros items: instanciar prefab como hijo de Main Camera
        if (item.itemModelPrefab == null)
        {
            Debug.LogError("? itemModelPrefab es NULL para: " + item.itemName);
            return;
        }

        if (fpItemHolder == null)
        {
            Debug.LogError("? fpItemHolder es NULL");
            return;
        }

        Debug.Log("Instanciando " + item.itemModelPrefab.name + " en " + fpItemHolder.name);

        // Instanciar directamente como hijo de Main Camera
        currentFPItem = Instantiate(item.itemModelPrefab, fpItemHolder);

        if (currentFPItem != null)
        {
            Debug.Log("? " + item.itemName + " instanciada correctamente");
            Debug.Log("   Nombre: " + currentFPItem.name);
            Debug.Log("   Posición local: " + currentFPItem.transform.localPosition);
            Debug.Log("   Activo: " + currentFPItem.activeInHierarchy);
        }
        else
        {
            Debug.LogError("? Fallo al instanciar " + item.itemName);
        }
    }

    private void UnequipCurrentItem()
    {
        // Ocultar linterna
        if (flashlightController != null)
        {
            flashlightController.SetFlashlightVisibility(false);
        }

        // Destruir item actual
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

    // ================ MÉTODOS LEGACY ================

    public bool HasLockpick() => lockpickCount > 0;

    public void UseLockpick()
    {
        if (lockpickCount > 0)
        {
            lockpickCount--;
            if (lockpickCount == 0)
            {
                RemoveItemByType(ItemType.Lockpick);
            }
        }
    }

    public int GetLockpickCount() => lockpickCount;

    public bool HasKey() => hasKey;

    private void RemoveItemByType(ItemType type)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] != null && inventorySlots[i].type == type)
            {
                inventorySlots[i] = null;
                if (selectedSlotIndex == i)
                {
                    DeselectItem();
                }
                UpdateUI();
                return;
            }
        }
    }

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
}