using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static bool isLoadingFromSave = false;

    [Header("Settings")]
    public float delayBeforeRestore = 0.2f;

    void Start()
    {
        if (isLoadingFromSave)
        {
            Debug.Log("=== CARGANDO PARTIDA GUARDADA ===");
            Invoke(nameof(RestoreGameState), delayBeforeRestore);
            isLoadingFromSave = false;
        }
    }

    void RestoreGameState()
    {
        SaveData data = SaveSystem.LoadGame();

        if (data == null || !data.hasSavedGame)
        {
            Debug.LogWarning("No hay datos de guardado válidos para restaurar");
            return;
        }

        // Verificar escena correcta
        if (SceneManager.GetActiveScene().name != data.sceneName)
        {
            Debug.LogWarning($"Escena incorrecta. Actual: {SceneManager.GetActiveScene().name}, Guardada: {data.sceneName}");
            return;
        }

        Debug.Log($"Restaurando estado del juego - Fecha guardado: {data.saveDateTime}");

        // 1. Restaurar jugador
        RestorePlayer(data);

        // 2. Restaurar inventario
        RestoreInventory(data);

        // 3. Restaurar guardias
        RestoreGuards(data);

        // 4. Eliminar items ya recogidos
        RemoveCollectedItems(data);

        Debug.Log("=== ESTADO DEL JUEGO RESTAURADO COMPLETAMENTE ===");
    }

    void RestorePlayer(SaveData data)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Restaurar posición y rotación
            player.transform.position = data.playerPosition;
            player.transform.rotation = data.playerRotation;

            Debug.Log($"✓ Jugador restaurado:");
            Debug.Log($"  Posición: {data.playerPosition}");
            Debug.Log($"  Rotación: {data.playerRotation.eulerAngles}");

            // Resetear física si tiene
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                controller.enabled = true;
            }

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            Debug.LogError("No se encontró el jugador para restaurar");
        }
    }

    void RestoreInventory(SaveData data)
    {
        InventoryManager inventory = FindFirstObjectByType<InventoryManager>();

        if (inventory == null)
        {
            Debug.LogError("No se encontró InventoryManager");
            return;
        }

        Debug.Log("Restaurando inventario...");

        // Limpiar inventario actual
        inventory.ClearInventory();

        // Restaurar items
        for (int i = 0; i < data.inventoryItemNames.Count && i < 5; i++)
        {
            string itemName = data.inventoryItemNames[i];

            if (!string.IsNullOrEmpty(itemName))
            {
                InventoryManager.InventoryItem item = GetItemByName(inventory, itemName);
                if (item != null)
                {
                    inventory.SetItemAtSlot(i, item);
                    Debug.Log($"  Slot {i}: {itemName}");
                }
            }
        }

        // Restaurar contadores y estado
        inventory.SetLockpickCount(data.lockpickCount);
        inventory.SetHasKey(data.hasKey);
        inventory.SetHasFlashlight(data.hasFlashlight);

        // Restaurar selección (sin equipar todavía)
        if (data.selectedSlotIndex >= 0 && data.selectedSlotIndex < 5)
        {
            inventory.SelectSlot(data.selectedSlotIndex);
        }

        // IMPORTANTE: Forzar actualización de UI
        inventory.ForceUpdateUI();

        Debug.Log($"✓ Inventario restaurado:");
        Debug.Log($"  Lockpicks: {data.lockpickCount}");
        Debug.Log($"  Llave: {data.hasKey}");
        Debug.Log($"  Linterna: {data.hasFlashlight}");
        Debug.Log($"  Slot seleccionado: {data.selectedSlotIndex}");
    }

    void RestoreGuards(SaveData data)
    {
        GuardAI[] guards = FindObjectsByType<GuardAI>(FindObjectsSortMode.None);

        Debug.Log($"Encontrados {guards.Length} guardias en escena. Datos guardados: {data.guards.Count}");

        foreach (GuardAI guard in guards)
        {
            // Buscar datos correspondientes a este guardia
            GuardSaveData guardData = data.guards.Find(g => g.guardName == guard.gameObject.name);

            if (guardData != null)
            {
                // Restaurar posición y rotación
                guard.transform.position = guardData.position;
                guard.transform.rotation = guardData.rotation;

                // Restaurar estado
                guard.RestoreState(
                    guardData.currentState,
                    guardData.currentPatrolIndex,
                    guardData.alertnessLevel,
                    guardData.hasSeenPlayer
                );

                Debug.Log($"✓ Guardia '{guard.gameObject.name}' restaurado:");
                Debug.Log($"  Estado: {guardData.currentState}");
                Debug.Log($"  Posición: {guardData.position}");
                Debug.Log($"  Alerta: {guardData.alertnessLevel}%");
            }
            else
            {
                Debug.LogWarning($"No se encontraron datos guardados para el guardia '{guard.gameObject.name}'");
            }
        }
    }

    void RemoveCollectedItems(SaveData data)
    {
        Debug.Log($"Eliminando {data.collectedKeyIDs.Count} llaves ya recogidas...");

        // Eliminar llaves recogidas
        foreach (string keyID in data.collectedKeyIDs)
        {
            KeyItem[] keys = FindObjectsByType<KeyItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (KeyItem key in keys)
            {
                if (key.keyID == keyID)
                {
                    key.gameObject.SetActive(false);
                    Debug.Log($"  Llave eliminada: {keyID}");
                    break;
                }
            }
        }

        // Eliminar ganzúa si ya fue recogida (solo hay una)
        if (data.hasCollectedLockpick)
        {
            LockpickItem[] lockpicks = FindObjectsByType<LockpickItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (lockpicks.Length > 0)
            {
                Destroy(lockpicks[0].gameObject);
                Debug.Log("  Ganzúa eliminada (ya fue recogida)");
            }
        }
    }

    InventoryManager.InventoryItem GetItemByName(InventoryManager inventory, string itemName)
    {
        // Buscar el item correspondiente en el InventoryManager
        if (itemName == inventory.flashlightItem.itemName)
            return inventory.flashlightItem;
        if (itemName == inventory.keyItem.itemName)
            return inventory.keyItem;
        if (itemName == inventory.lockpickItem.itemName)
            return inventory.lockpickItem;

        Debug.LogWarning($"Item no encontrado: {itemName}");
        return null;
    }

    [ContextMenu("Debug - Show Current State")]
    void DebugShowCurrentState()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log($"=== ESTADO ACTUAL DEL JUEGO ===");
            Debug.Log($"Escena: {SceneManager.GetActiveScene().name}");
            Debug.Log($"Jugador: {player.transform.position}");

            InventoryManager inv = FindFirstObjectByType<InventoryManager>();
            if (inv != null)
            {
                Debug.Log($"Inventario: {inv.inventorySlots.Count} slots");
                Debug.Log($"Lockpicks: {inv.GetLockpickCount()}");
            }

            GuardAI[] guards = FindObjectsByType<GuardAI>(FindObjectsSortMode.None);
            Debug.Log($"Guardias: {guards.Length}");
            foreach (var guard in guards)
            {
                Debug.Log($"  {guard.name}: {guard.currentState} | Alerta: {guard.alertnessLevel}%");
            }
        }
    }
}