using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightFPSController : MonoBehaviour
{
    [Header("DEBUG - Flashlight Model")]
    public GameObject flashlightModelPrefab; // Arrastra tu LINTERNAFBX aquí
    private GameObject flashlightInstance;

    [Header("DEBUG - Position & Scale")]
    public Vector3 testPosition = new Vector3(0.5f, -0.3f, 0.8f);
    public Vector3 testRotation = new Vector3(0f, 0f, 0f); // ✅ Rotación ajustable
    public Vector3 testScale = new Vector3(0.1f, 0.1f, 0.1f); // ✅ Escala ajustable (modelos de 3DS suelen ser grandes)
    public bool forceVisible = true;

    [Header("Light")]
    public Light flashlightLight;
    public bool isLightOn = false;

    // Referencias
    private Camera playerCamera;
    private bool flashlightVisible = false;

    void Start()
    {
        Debug.Log("=== DEBUG FLASHLIGHT START ===");

        // Obtener cámara
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("❌ NO HAY CÁMARA EN ESTE GAMEOBJECT!");
            return;
        }
        Debug.Log("✅ Cámara encontrada: " + playerCamera.name);

        // Test inmediato del prefab
        TestPrefabCreation();
    }

    void TestPrefabCreation()
    {
        Debug.Log("=== TESTING PREFAB CREATION ===");

        if (flashlightModelPrefab == null)
        {
            Debug.LogError("❌ FLASHLIGHT MODEL PREFAB ES NULL! Asegúrate de arrastrarlo en el inspector");
            return;
        }

        Debug.Log($"✅ Prefab asignado: {flashlightModelPrefab.name}");

        try
        {
            // Crear instancia
            Debug.Log("Intentando crear instancia...");
            flashlightInstance = Instantiate(flashlightModelPrefab);

            if (flashlightInstance == null)
            {
                Debug.LogError("❌ FALLO AL CREAR INSTANCIA!");
                return;
            }

            Debug.Log($"✅ Instancia creada: {flashlightInstance.name}");

            // Configurar como hijo de cámara
            flashlightInstance.transform.SetParent(playerCamera.transform);
            Debug.Log("✅ Configurado como hijo de cámara");

            // Posición, rotación y escala ajustables
            flashlightInstance.transform.localPosition = testPosition;
            flashlightInstance.transform.localRotation = Quaternion.Euler(testRotation);
            flashlightInstance.transform.localScale = testScale; // ✅ Escala ajustable

            Debug.Log($"✅ Posición local: {flashlightInstance.transform.localPosition}");
            Debug.Log($"✅ Rotación local: {flashlightInstance.transform.localRotation.eulerAngles}");
            Debug.Log($"✅ Escala local: {flashlightInstance.transform.localScale}");

            // ✅ Configurar luz automáticamente
            SetupLight();

            // Debug de renderers
            CheckRenderers();

            // Activar si forceVisible está activado
            if (forceVisible)
            {
                flashlightInstance.SetActive(true);
                flashlightVisible = true;
                Debug.Log("✅ LINTERNA FORZADA A VISIBLE");
            }
            else
            {
                flashlightInstance.SetActive(false);
                Debug.Log("⚠️ Linterna inicialmente oculta");
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ERROR AL CREAR LINTERNA: {e.Message}");
        }
    }

    void SetupLight()
    {
        Debug.Log("=== SETUP LIGHT ===");

        if (flashlightInstance == null)
        {
            Debug.LogError("❌ No hay instancia de linterna para configurar luz");
            return;
        }

        // Buscar luz existente en el modelo
        flashlightLight = flashlightInstance.GetComponentInChildren<Light>();

        if (flashlightLight == null)
        {
            Debug.Log("No se encontró luz en el modelo, creando una nueva...");

            // Crear nueva luz
            GameObject lightObj = new GameObject("FlashlightLight");
            lightObj.transform.SetParent(flashlightInstance.transform);

            // Posicionar la luz en la punta de la linterna
            lightObj.transform.localPosition = new Vector3(0f, 0f, 1f); // Hacia adelante
            lightObj.transform.localRotation = Quaternion.identity;

            flashlightLight = lightObj.AddComponent<Light>();
            Debug.Log("✅ Luz creada manualmente");
        }
        else
        {
            Debug.Log("✅ Luz encontrada en el modelo");
        }

        // Configurar propiedades de la luz
        flashlightLight.type = LightType.Spot;
        flashlightLight.intensity = 3f; // Más intensa para que se note
        flashlightLight.range = 15f;
        flashlightLight.spotAngle = 60f;
        flashlightLight.color = Color.white;
        flashlightLight.enabled = false; // Inicialmente apagada

        Debug.Log($"✅ Luz configurada - Intensity: {flashlightLight.intensity}, Range: {flashlightLight.range}");
    }

    void CheckRenderers()
    {
        if (flashlightInstance == null) return;

        Debug.Log("=== CHECKING RENDERERS ===");

        // Buscar todos los renderers
        Renderer[] renderers = flashlightInstance.GetComponentsInChildren<Renderer>();
        Debug.Log($"Renderers encontrados: {renderers.Length}");

        if (renderers.Length == 0)
        {
            Debug.LogWarning("⚠️ NO SE ENCONTRARON RENDERERS! El modelo puede no ser visible");
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            Debug.Log($"  Renderer {i}: {r.name}");
            Debug.Log($"    - Enabled: {r.enabled}");
            Debug.Log($"    - GameObject Active: {r.gameObject.activeInHierarchy}");
            Debug.Log($"    - Material: {(r.material != null ? r.material.name : "NULL")}");
            Debug.Log($"    - Bounds: {r.bounds}");

            // Forzar activación
            r.enabled = true;
            r.gameObject.SetActive(true);

            // Si no tiene material, asignar uno básico
            if (r.material == null)
            {
                r.material = Resources.GetBuiltinResource<Material>("Default-Material.mat");
                Debug.Log("    - ✅ Material básico asignado");
            }
        }

        // Información de la jerarquía
        Debug.Log("=== HIERARCHY INFO ===");
        PrintHierarchy(flashlightInstance.transform, 0);
    }

    void PrintHierarchy(Transform t, int level)
    {
        string indent = new string(' ', level * 2);
        Debug.Log($"{indent}- {t.name} (Active: {t.gameObject.activeInHierarchy})");

        for (int i = 0; i < t.childCount; i++)
        {
            PrintHierarchy(t.GetChild(i), level + 1);
        }
    }

    void Update()
    {
        // Input test
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("=== F KEY PRESSED ===");
            ToggleFlashlight();
        }

        // Debug continuo cada 5 segundos
        if (Time.time % 5f < Time.deltaTime)
        {
            DebugStatus();
        }
    }

    void DebugStatus()
    {
        if (flashlightInstance != null)
        {
            Debug.Log($"Status: Visible={flashlightVisible}, Instance Active={flashlightInstance.activeInHierarchy}, Pos={flashlightInstance.transform.localPosition}");
        }
    }

    public void ToggleFlashlight()
    {
        InventoryManager inventory = FindFirstObjectByType<InventoryManager>();
        if (inventory != null && inventory.GetSelectedItemType() != InventoryManager.ItemType.Flashlight)
        {
            Debug.Log("Necesitás tener la linterna seleccionada");
            return;
        }
        Debug.Log("=== TOGGLE FLASHLIGHT ===");

        if (flashlightInstance == null)
        {
            Debug.LogError("❌ flashlightInstance ES NULL! No se puede hacer toggle");
            return;
        }

        // MODIFICADO: Solo cambiar el estado de la LUZ, no del modelo
        isLightOn = !isLightOn;

        // El modelo siempre visible cuando está equipado
        // Solo cambiamos la luz
        if (flashlightLight != null)
        {
            flashlightLight.enabled = isLightOn;
            Debug.Log($"💡 Luz: {flashlightLight.enabled}");
        }
        else
        {
            Debug.Log("⚠️ No hay luz configurada");
        }
    }

    // Método manual para testing en el inspector
    [ContextMenu("Test Create Flashlight")]
    public void TestCreateFlashlight()
    {
        TestPrefabCreation();
    }

    [ContextMenu("Force Toggle")]
    public void ForceToggle()
    {
        ToggleFlashlight();
    }

    [ContextMenu("Reset Position")]
    public void ResetPosition()
    {
        if (flashlightInstance != null)
        {
            flashlightInstance.transform.localPosition = testPosition;
            flashlightInstance.transform.localRotation = Quaternion.Euler(testRotation);
            flashlightInstance.transform.localScale = testScale;
            Debug.Log($"Transform resetteado - Pos: {testPosition}, Rot: {testRotation}, Scale: {testScale}");
        }
    }

    [ContextMenu("Refresh Light Setup")]
    public void RefreshLightSetup()
    {
        SetupLight();
    }

    // ✅ Métodos para compatibilidad con PlayerController
    public void SetFlashlightVisibility(bool visible)
    {
        if (flashlightInstance != null)
        {
            flashlightVisible = visible;
            flashlightInstance.SetActive(visible);

            // Si se oculta la linterna, también apagar la luz
            if (!visible && flashlightLight != null)
            {
                flashlightLight.enabled = false;
                isLightOn = false;
            }

            Debug.Log($"SetFlashlightVisibility: {visible}");
        }
    }

    public void SetLightIntensity(float intensity)
    {
        if (flashlightLight != null)
        {
            flashlightLight.intensity = intensity;
            Debug.Log($"SetLightIntensity: {intensity}");
        }
    }
}