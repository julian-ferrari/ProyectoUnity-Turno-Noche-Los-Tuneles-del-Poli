using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventario")]
    [SerializeField] private int lockpickCount = 0;

    [Header("UI (Opcional)")]
    [SerializeField] private Text lockpickCountText;

    void Start()
    {
        UpdateUI();
    }

    public void AddLockpick()
    {
        lockpickCount++;
        UpdateUI();
        Debug.Log("Ganz�as en inventario: " + lockpickCount);
    }

    public bool HasLockpick()
    {
        return lockpickCount > 0;
    }

    public void UseLockpick()
    {
        if (lockpickCount > 0)
        {
            lockpickCount--;
            UpdateUI();
            Debug.Log("Ganz�a usada. Restantes: " + lockpickCount);
        }
    }

    void UpdateUI()
    {
        if (lockpickCountText != null)
        {
            lockpickCountText.text = "Ganz�as: " + lockpickCount;
        }
    }

    public int GetLockpickCount()
    {
        return lockpickCount;
    }
}