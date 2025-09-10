using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class AutoTextureAssigner : MonoBehaviour
{
    [Header("Configuración")]
    public string textureFolderPath = "Assets/TEXTURAS POLI"; // Ruta de tu carpeta de texturas
    public bool includeSubfolders = true;
    public bool overwriteExistingTextures = true;

    [Header("Opciones de Búsqueda")]
    [Tooltip("Buscar por nombre exacto o similar")]
    public bool useExactNameMatch = false;
    [Tooltip("Ignorar mayúsculas/minúsculas")]
    public bool ignoreCase = true;

    [Header("Debug")]
    public bool showDebugLog = true;

    [ContextMenu("Asignar Texturas Automáticamente")]
    public void AssignTexturesAutomatically()
    {
        Debug.Log("Iniciando asignación automática de texturas...");

        // Obtener todas las texturas de la carpeta
        Dictionary<string, Texture2D> textureDict = LoadAllTextures();

        if (textureDict.Count == 0)
        {
            Debug.LogError($"No se encontraron texturas en: {textureFolderPath}");
            return;
        }

        Debug.Log($"Texturas encontradas: {textureDict.Count}");

        // Obtener todos los materiales del mapa
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        int materialsProcessed = 0;
        int texturesAssigned = 0;

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material == null) continue;

                materialsProcessed++;

                // Intentar encontrar textura que coincida
                Texture2D matchingTexture = FindMatchingTexture(material.name, textureDict);

                if (matchingTexture != null)
                {
                    if (overwriteExistingTextures || material.mainTexture == null)
                    {
                        material.mainTexture = matchingTexture;
                        texturesAssigned++;

                        if (showDebugLog)
                        {
                            Debug.Log($"✅ {material.name} → {matchingTexture.name}");
                        }
                    }
                    else if (showDebugLog)
                    {
                        Debug.Log($"⏭️ Saltado (ya tiene textura): {material.name}");
                    }
                }
                else if (showDebugLog)
                {
                    Debug.LogWarning($"❌ No se encontró textura para: {material.name}");
                }
            }
        }

        Debug.Log($"Proceso completado: {texturesAssigned}/{materialsProcessed} texturas asignadas");

        // Mostrar texturas no utilizadas
        ShowUnusedTextures(textureDict, renderers);
    }

    Dictionary<string, Texture2D> LoadAllTextures()
    {
        Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();

#if UNITY_EDITOR
        // Buscar en la carpeta especificada
        string searchPattern = includeSubfolders ? "t:Texture2D" : "t:Texture2D";
        string[] textureGUIDs = AssetDatabase.FindAssets(searchPattern, new[] { textureFolderPath });

        foreach (string guid in textureGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            if (texture != null)
            {
                string key = ignoreCase ? texture.name.ToLower() : texture.name;

                if (!textureDict.ContainsKey(key))
                {
                    textureDict.Add(key, texture);
                }
                else if (showDebugLog)
                {
                    Debug.LogWarning($"Textura duplicada encontrada: {texture.name}");
                }
            }
        }
#endif

        return textureDict;
    }

    Texture2D FindMatchingTexture(string materialName, Dictionary<string, Texture2D> textureDict)
    {
        string searchName = ignoreCase ? materialName.ToLower() : materialName;

        // Limpiar nombre del material (remover sufijos como "(Instance)")
        searchName = CleanMaterialName(searchName);

        // Búsqueda exacta
        if (textureDict.ContainsKey(searchName))
        {
            return textureDict[searchName];
        }

        // Búsqueda flexible si no se requiere coincidencia exacta
        if (!useExactNameMatch)
        {
            // Buscar por substring
            foreach (var kvp in textureDict)
            {
                string textureName = kvp.Key;

                // Si el nombre del material contiene el nombre de la textura o viceversa
                if (textureName.Contains(searchName) || searchName.Contains(textureName))
                {
                    return kvp.Value;
                }
            }

            // Búsqueda por palabras clave
            string[] materialWords = searchName.Split('_', ' ', '-');
            foreach (var kvp in textureDict)
            {
                string textureName = kvp.Key;

                foreach (string word in materialWords)
                {
                    if (word.Length > 2 && textureName.Contains(word))
                    {
                        return kvp.Value;
                    }
                }
            }
        }

        return null;
    }

    string CleanMaterialName(string materialName)
    {
        // Remover sufijos comunes
        string cleaned = materialName;

        // Patrones a remover
        string[] patterns = { " (Instance)", "(Instance)", "_mat", "_material", ".001", ".002", ".003" };

        foreach (string pattern in patterns)
        {
            string searchPattern = ignoreCase ? pattern.ToLower() : pattern;
            cleaned = cleaned.Replace(searchPattern, "");
        }

        return cleaned.Trim();
    }

    void ShowUnusedTextures(Dictionary<string, Texture2D> textureDict, Renderer[] renderers)
    {
        List<string> usedTextures = new List<string>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                if (material != null && material.mainTexture != null)
                {
                    string textureName = ignoreCase ? material.mainTexture.name.ToLower() : material.mainTexture.name;
                    if (!usedTextures.Contains(textureName))
                    {
                        usedTextures.Add(textureName);
                    }
                }
            }
        }

        Debug.Log("=== TEXTURAS NO UTILIZADAS ===");
        foreach (var kvp in textureDict)
        {
            if (!usedTextures.Contains(kvp.Key))
            {
                Debug.Log($"📁 Textura no utilizada: {kvp.Value.name}");
            }
        }
    }

    [ContextMenu("Listar Materiales")]
    public void ListAllMaterials()
    {
        Debug.Log("=== MATERIALES EN EL MAPA ===");

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        HashSet<string> uniqueMaterials = new HashSet<string>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                if (material != null && !uniqueMaterials.Contains(material.name))
                {
                    uniqueMaterials.Add(material.name);
                    string textureStatus = material.mainTexture != null ? "✅ Con textura" : "❌ Sin textura";
                    Debug.Log($"{material.name} - {textureStatus}");
                }
            }
        }

        Debug.Log($"Total de materiales únicos: {uniqueMaterials.Count}");
    }

    [ContextMenu("Listar Texturas Disponibles")]
    public void ListAvailableTextures()
    {
        Dictionary<string, Texture2D> textures = LoadAllTextures();

        Debug.Log("=== TEXTURAS DISPONIBLES ===");
        foreach (var kvp in textures)
        {
            Debug.Log($"📎 {kvp.Value.name} (Resolución: {kvp.Value.width}x{kvp.Value.height})");
        }

        Debug.Log($"Total de texturas: {textures.Count}");
    }
}
