using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // Información general
    public string sceneName;
    public string saveDateTime;
    public bool hasSavedGame;

    // Jugador
    public Vector3 playerPosition;
    public Quaternion playerRotation;

    // Inventario
    public List<string> inventoryItemNames = new List<string>();
    public int selectedSlotIndex = -1;
    public int lockpickCount = 0;
    public bool hasKey = false;
    public bool hasFlashlight = false;

    // Guardias (puede haber múltiples)
    public List<GuardSaveData> guards = new List<GuardSaveData>();

    // Items recogidos (solo llaves, la ganzúa es única y permanente)
    public List<string> collectedKeyIDs = new List<string>();
    public bool hasCollectedLockpick = false;
}

[System.Serializable]
public class GuardSaveData
{
    public string guardName;
    public Vector3 position;
    public Quaternion rotation;
    public string currentState; // "Patrolling", "Chasing", etc.
    public int currentPatrolIndex;
    public float alertnessLevel;
    public bool hasSeenPlayer;
}

public static class SaveSystem
{
    private static string savePath => Application.persistentDataPath + "/polinights_savegame.json";

    public static void SaveGame()
    {
        SaveData data = new SaveData();

        // Guardar información general
        data.sceneName = SceneManager.GetActiveScene().name;
        data.saveDateTime = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        data.hasSavedGame = true;

        // Guardar jugador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            data.playerPosition = player.transform.position;
            data.playerRotation = player.transform.rotation;
            Debug.Log($"Guardando jugador - Posición: {data.playerPosition}");
        }
        else
        {
            Debug.LogWarning("No se encontró jugador con tag 'Player'");
        }

        // Guardar inventario
        InventoryManager inventory = Object.FindFirstObjectByType<InventoryManager>();
        if (inventory != null)
        {
            SaveInventory(data, inventory);
        }
        else
        {
            Debug.LogWarning("No se encontró InventoryManager");
        }

        // Guardar todos los guardias
        GuardAI[] guards = Object.FindObjectsByType<GuardAI>(FindObjectsSortMode.None);
        foreach (GuardAI guard in guards)
        {
            GuardSaveData guardData = new GuardSaveData();
            guardData.guardName = guard.gameObject.name;
            guardData.position = guard.transform.position;
            guardData.rotation = guard.transform.rotation;
            guardData.currentState = guard.currentState.ToString();
            guardData.currentPatrolIndex = guard.GetCurrentPatrolIndex();
            guardData.alertnessLevel = guard.alertnessLevel;
            guardData.hasSeenPlayer = guard.HasSeenPlayer();

            data.guards.Add(guardData);
            Debug.Log($"Guardando guardia: {guardData.guardName} - Estado: {guardData.currentState}");
        }

        // Guardar items recogidos
        SaveCollectedItems(data);

        // Guardar a archivo
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"✓ Juego guardado exitosamente en: {savePath}");
            Debug.Log($"  Escena: {data.sceneName}");
            Debug.Log($"  Items en inventario: {data.inventoryItemNames.Count}");
            Debug.Log($"  Guardias guardados: {data.guards.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al guardar: {e.Message}");
        }
    }

    private static void SaveCollectedItems(SaveData data)
    {
        data.collectedKeyIDs.Clear();

        // Guardar solo las llaves que ya NO están activas (fueron recogidas)
        KeyItem[] allKeys = Object.FindObjectsByType<KeyItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (KeyItem key in allKeys)
        {
            if (!key.canPickup || !key.gameObject.activeInHierarchy)
            {
                data.collectedKeyIDs.Add(key.keyID);
                Debug.Log($"Llave recogida guardada: {key.keyID}");
            }
        }

        // Guardar si ya recogió la ganzúa (solo hay una y es permanente)
        InventoryManager inventory = Object.FindFirstObjectByType<InventoryManager>();
        if (inventory != null)
        {
            data.hasCollectedLockpick = inventory.HasLockpick();
        }

        Debug.Log($"Total llaves recogidas: {data.collectedKeyIDs.Count}, Ganzúa recogida: {data.hasCollectedLockpick}");
    }

    private static void SaveInventory(SaveData data, InventoryManager inventory)
    {
        // Guardar items del inventario
        data.inventoryItemNames.Clear();
        for (int i = 0; i < inventory.inventorySlots.Count; i++)
        {
            if (inventory.inventorySlots[i] != null)
            {
                data.inventoryItemNames.Add(inventory.inventorySlots[i].itemName);
            }
            else
            {
                data.inventoryItemNames.Add(""); // Slot vacío
            }
        }

        // Guardar estado del inventario
        data.selectedSlotIndex = inventory.selectedSlotIndex;
        data.lockpickCount = inventory.GetLockpickCount();
        data.hasKey = inventory.HasKey();
        data.hasFlashlight = inventory.HasFlashlight();

        Debug.Log($"Inventario guardado - Lockpicks: {data.lockpickCount}, Key: {data.hasKey}, Flashlight: {data.hasFlashlight}");
    }

    public static SaveData LoadGame()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"✓ Juego cargado - Escena: {data.sceneName} | Fecha: {data.saveDateTime}");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error al cargar el guardado: {e.Message}");
                return null;
            }
        }
        else
        {
            Debug.Log("No se encontró archivo de guardado");
            return null;
        }
    }

    public static bool HasSavedGame()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                return data.hasSavedGame;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    public static void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Guardado eliminado");
        }
    }

    public static string GetSavePath()
    {
        return savePath;
    }
}