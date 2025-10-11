using UnityEngine;

// ==================== AUDIO ZONE ====================
// Este script va en CADA ZONA del mapa (con un Collider trigger)
[RequireComponent(typeof(Collider))]
public class AudioZone : MonoBehaviour
{
    [Header("Identificaci�n")]
    public string zoneName = "Zona Principal";

    [Header("M�sica")]
    public AudioClip music; // Arrastra aqu� tu archivo MP3 de m�sica
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    [Header("Sonidos Ambientales")]
    public AudioClip ambience1; // Arrastra aqu�: grillos, gotas de agua, etc
    [Range(0f, 1f)]
    public float ambience1Volume = 0.4f;

    public AudioClip ambience2; // Arrastra aqu�: perros, viento, etc
    [Range(0f, 1f)]
    public float ambience2Volume = 0.3f;

    [Header("Configuraci�n")]
    public bool activateOnStart = false;
    public Color gizmoColor = Color.green;

    void Start()
    {
        // Asegurar que el collider es trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError($"AudioZone '{zoneName}' no tiene Collider!");
        }

        if (activateOnStart)
        {
            AudioZoneManager.Instance?.EnterZone(this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AudioZoneManager.Instance?.EnterZone(this);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AudioZoneManager.Instance?.ExitZone(this);
        }
    }

    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);

            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                // Aproximaci�n simple para c�psula
                Gizmos.DrawWireSphere(transform.position + capsule.center, capsule.radius);
            }
        }

        // Etiqueta
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position, zoneName);
#endif
    }
}