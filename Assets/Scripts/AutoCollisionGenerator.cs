using UnityEngine;

public class AutoCollisionGenerator : MonoBehaviour
{
    [Header("Collision Settings")]
    public bool generateOnStart = true;
    public bool removeExistingColliders = true;
    public LayerMask collisionLayer = 1;

    [Header("Debug")]
    public bool showDebugInfo = true;

    void Start()
    {
        if (generateOnStart)
        {
            GenerateCollisions();
        }
    }

    [ContextMenu("Generate Collisions")]
    public void GenerateCollisions()
    {
        Debug.Log("Iniciando generación de colisiones...");

        int collidersCreated = 0;
        int collidersSkipped = 0;

        // Buscar todos los mesh renderers en hijos
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();

        Debug.Log($"Encontrados {renderers.Length} mesh renderers");

        foreach (MeshRenderer renderer in renderers)
        {
            GameObject obj = renderer.gameObject;

            // Verificar si ya tiene collider
            Collider existingCollider = obj.GetComponent<Collider>();

            if (existingCollider != null && !removeExistingColliders)
            {
                collidersSkipped++;
                continue;
            }

            // Eliminar collider existente si es necesario
            if (existingCollider != null && removeExistingColliders)
            {
                if (Application.isPlaying)
                    Destroy(existingCollider);
                else
                    DestroyImmediate(existingCollider);
            }

            // Obtener mesh filter
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            // Crear mesh collider
            MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false; // Para geometría estática compleja

            // Asignar layer
            obj.layer = (int)Mathf.Log(collisionLayer.value, 2);

            collidersCreated++;

            if (showDebugInfo)
            {
                Debug.Log($"Collider creado para: {obj.name}");
            }
        }

        Debug.Log($"Generación completa: {collidersCreated} colliders creados, {collidersSkipped} omitidos");
    }

    [ContextMenu("Remove All Colliders")]
    public void RemoveAllColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }

        Debug.Log($"Eliminados {colliders.Length} colliders");
    }

    [ContextMenu("Test Collisions")]
    public void TestCollisions()
    {
        // Contar colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        Debug.Log($"Total de colliders en el mapa: {colliders.Length}");

        // Verificar si el player puede hacer raycast
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            Vector3 playerPos = player.transform.position;

            // Raycast en múltiples direcciones
            Vector3[] directions = {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                Vector3.forward + Vector3.right, Vector3.forward + Vector3.left,
                Vector3.back + Vector3.right, Vector3.back + Vector3.left
            };

            int hits = 0;
            foreach (Vector3 dir in directions)
            {
                if (Physics.Raycast(playerPos, dir.normalized, 10f))
                {
                    hits++;
                    Debug.DrawRay(playerPos, dir.normalized * 10f, Color.green, 5f);
                }
                else
                {
                    Debug.DrawRay(playerPos, dir.normalized * 10f, Color.red, 5f);
                }
            }

            Debug.Log($"Test de colisiones: {hits}/{directions.Length} direcciones detectan colisión");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar bounds de todos los colliders hijos
        Collider[] colliders = GetComponentsInChildren<Collider>();

        Gizmos.color = Color.green;
        foreach (Collider col in colliders)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
