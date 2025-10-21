using UnityEngine;

public class DebugResetKeys : MonoBehaviour
{
    [Header("Teclas de Debug")]
    [SerializeField] private KeyCode resetKeysKey = KeyCode.F11;
    [SerializeField] private KeyCode resetAllKey = KeyCode.F12;

    void Update()
    {
        if (Input.GetKeyDown(resetKeysKey))
        {
            ResetKeys();
        }

        if (Input.GetKeyDown(resetAllKey))
        {
            ResetAll();
        }
    }

    void ResetKeys()
    {
        Debug.Log("=== RESETEANDO LLAVES ===");

        // IMPORTANTE: Limpiar llaves del inventario primero
        if (InventoryManager.Instance != null)
        {
            // Obtener todas las llaves que tiene
            var keyIDs = InventoryManager.Instance.GetCollectedKeyIDs();

            // Remover cada llave del inventario
            foreach (string keyID in keyIDs)
            {
                InventoryManager.Instance.UseKey(keyID);
            }

            Debug.Log($"Limpiadas {keyIDs.Count} llaves del inventario");
        }

        // Buscar todas las llaves en el mundo (incluso inactivas)
        KeyItem[] allKeys = FindObjectsByType<KeyItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int resetCount = 0;
        foreach (KeyItem key in allKeys)
        {
            // Usar el método público de reseteo
            key.ResetKey();
            resetCount++;
        }

        // Resetear puertas
        Door[] allDoors = FindObjectsByType<Door>(FindObjectsSortMode.None);
        foreach (Door door in allDoors)
        {
            string doorKey = "Door_Unlocked_" + door.requiredKeyID + "_" + door.transform.position.ToString();
            PlayerPrefs.DeleteKey(doorKey);

            // Resetear estado de la puerta
            door.isLocked = true;
            door.isOpen = false;
        }

        PlayerPrefs.Save();

        Debug.Log($"✓ LLAVES RESETEADAS: {resetCount} llaves y {allDoors.Length} puertas");
    }

    void ResetAll()
    {
        Debug.Log("=== RESETEANDO TODO ===");

        // Limpiar inventario completamente
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ClearInventory();
            Debug.Log("Inventario limpiado");
        }

        // Resetear llaves
        KeyItem[] allKeys = FindObjectsByType<KeyItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (KeyItem key in allKeys)
        {
            key.ResetKey();
        }

        // Resetear ganzúas
        LockpickItem[] lockpicks = FindObjectsByType<LockpickItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (LockpickItem lockpick in lockpicks)
        {
            lockpick.gameObject.SetActive(true);
        }

        // Resetear puertas
        Door[] allDoors = FindObjectsByType<Door>(FindObjectsSortMode.None);
        foreach (Door door in allDoors)
        {
            door.isLocked = true;
            door.isOpen = false;
        }

        // Borrar TODO PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("✓ TODO RESETEADO COMPLETAMENTE");
    }

    [ContextMenu("Resetear Llaves")]
    void ResetKeysMenu()
    {
        ResetKeys();
    }

    [ContextMenu("Resetear Todo")]
    void ResetAllMenu()
    {
        ResetAll();
    }
}