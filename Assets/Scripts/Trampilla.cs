using UnityEngine;

public class Trampilla : MonoBehaviour
{
    [Header("Configuración de Teletransporte")]
    [Tooltip("Punto de destino donde aparecerá el jugador")]
    public Transform puntoDestino;

    [Header("Configuración de Interacción")]
    [Tooltip("Distancia a la que el jugador puede interactuar")]
    public float distanciaInteraccion = 3f;

    [Tooltip("Tecla para interactuar")]
    public KeyCode teclaInteraccion = KeyCode.E;

    [Header("UI (Opcional)")]
    [Tooltip("Texto o imagen que se muestra cuando se puede interactuar")]
    public GameObject indicadorUI;

    private Transform jugador;
    private bool jugadorCerca = false;

    void Start()
    {
        // Buscar al jugador por tag
        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null)
        {
            jugador = jugadorObj.transform;
        }

        // Ocultar indicador al inicio
        if (indicadorUI != null)
        {
            indicadorUI.SetActive(false);
        }

        // Verificar que se haya asignado el punto de destino
        if (puntoDestino == null)
        {
            Debug.LogError("¡No se ha asignado un punto de destino en la trampilla!");
        }
    }

    void Update()
    {
        if (jugador == null) return;

        // Calcular distancia entre jugador y trampilla
        float distancia = Vector3.Distance(transform.position, jugador.position);

        // Verificar si el jugador está cerca
        jugadorCerca = distancia <= distanciaInteraccion;

        // Mostrar/ocultar indicador
        if (indicadorUI != null)
        {
            indicadorUI.SetActive(jugadorCerca);
        }

        // Detectar interacción
        if (jugadorCerca && Input.GetKeyDown(teclaInteraccion))
        {
            TeletransportarJugador();
        }
    }

    void TeletransportarJugador()
    {
        if (puntoDestino == null)
        {
            Debug.LogError("No se puede teletransportar: punto de destino no asignado");
            return;
        }

        // Desactivar el CharacterController temporalmente si existe
        CharacterController controller = jugador.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        // Teletransportar al jugador
        jugador.position = puntoDestino.position;
        jugador.rotation = puntoDestino.rotation;

        // Reactivar el CharacterController
        if (controller != null)
        {
            controller.enabled = true;
        }

        Debug.Log("Jugador teletransportado a: " + puntoDestino.position);
    }

    // Visualizar el rango de interacción en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaInteraccion);

        if (puntoDestino != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(puntoDestino.position, 0.5f);
            Gizmos.DrawLine(transform.position, puntoDestino.position);
        }
    }
}