using UnityEngine;
using System.Collections;

public class GuardAI : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float waitTime = 2f;

    [Header("Detection")]
    public float detectionRange = 5f;
    public float hearingRange = 8f;
    public float fieldOfViewAngle = 60f; // Ángulo de visión más realista

    [Header("Chasing")]
    public float chaseSpeed = 4f;
    public float investigateTime = 10f;
    public float maxChaseDistance = 20f;
    public float minChaseDistance = 2f; // NUEVO: distancia mínima para evitar pegarse
    public float losePlayerTime = 5f; // NUEVO: tiempo antes de perder al jugador

    [Header("AI Behavior")]
    public bool canOpenDoors = true;
    public bool canHearFootsteps = true;
    public bool canSeeFlashlight = true;
    public float suspicionDecayTime = 5f;
    public float alertnessLevel = 0f; // 0 = calm, 100 = maximum alert

    [Header("Animation & Audio")]
    public Animator guardAnimator;
    public AudioSource audioSource;
    public AudioClip[] alertSounds;
    public AudioClip[] patrolSounds;

    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private float investigateTimer = 0f;
    private float losePlayerTimer = 0f; // NUEVO: contador para perder jugador
    private Transform player;
    private PlayerController playerController;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 investigatePosition;
    private bool hasSeenPlayer = false;

    // NUEVO: Variables para posición inicial
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private int initialPatrolIndex;

    public enum GuardState { Patrolling, Investigating, Chasing, Searching, Returning }
    public GuardState currentState = GuardState.Patrolling;
    private GuardState previousState;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>().transform;
        playerController = player.GetComponent<PlayerController>();

        if (guardAnimator == null)
            guardAnimator = GetComponent<Animator>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // NUEVO: Guardar posición inicial
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialPatrolIndex = 0;

        // Si no hay patrol points, crear algunos básicos
        if (patrolPoints.Length == 0)
        {
            CreateBasicPatrolPoints();
        }

        StartCoroutine(SuspicionDecay());
    }

    // NUEVO: Método público para resetear el guardia
    public void ResetGuard()
    {
        // Resetear posición y rotación
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // Resetear estado
        currentState = GuardState.Patrolling;
        previousState = GuardState.Patrolling;
        currentPatrolIndex = initialPatrolIndex;

        // Resetear timers y flags
        waitTimer = 0f;
        investigateTimer = 0f;
        losePlayerTimer = 0f;
        hasSeenPlayer = false;
        alertnessLevel = 0f;

        // Resetear velocidad del Rigidbody si existe
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("Guardia reseteado a posición inicial");
    }

    void Update()
    {
        UpdateAlertness();

        switch (currentState)
        {
            case GuardState.Patrolling:
                Patrol();
                CheckForPlayer();
                break;

            case GuardState.Investigating:
                Investigate();
                break;

            case GuardState.Chasing:
                ChasePlayer();
                break;

            case GuardState.Searching:
                SearchLastKnownPosition();
                break;

            case GuardState.Returning:
                ReturnToPatrol();
                break;
        }

        // Actualizar animaciones
        UpdateAnimations();
    }

    void UpdateAlertness()
    {
        // La alerta afecta la velocidad de detección y movimiento
        float alertMultiplier = 1f + (alertnessLevel / 100f);
        patrolSpeed = 2f * alertMultiplier;
        detectionRange = 5f * alertMultiplier;
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        float distance = Vector3.Distance(transform.position, targetPoint.position);

        if (distance < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                waitTimer = 0f;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;

                // Ocasionalmente hacer sonidos de patrulla
                if (Random.value < 0.3f && patrolSounds.Length > 0 && audioSource != null)
                {
                    audioSource.PlayOneShot(patrolSounds[Random.Range(0, patrolSounds.Length)]);
                }
            }
        }
        else
        {
            MoveTowards(targetPoint.position, patrolSpeed);
        }
    }

    void CheckForPlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool playerDetected = false;

        // Detección visual mejorada
        if (distanceToPlayer <= detectionRange)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            if (angleToPlayer < fieldOfViewAngle / 2f) // Campo de visión
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out hit, detectionRange))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        // Factor de detección basado en si el jugador está agachado, corriendo, etc.
                        float detectionChance = CalculateDetectionChance();
                        if (Random.value < detectionChance)
                        {
                            playerDetected = true;
                        }
                    }
                }
            }
        }

        // Detección de linterna
        if (canSeeFlashlight && distanceToPlayer <= detectionRange * 1.5f)
        {
            Light flashlight = player.GetComponentInChildren<Light>();
            if (flashlight != null && flashlight.enabled)
            {
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                if (Vector3.Dot(transform.forward, directionToPlayer) > 0.3f)
                {
                    playerDetected = true;
                }
            }
        }

        // Detección auditiva mejorada
        if (canHearFootsteps && distanceToPlayer <= hearingRange)
        {
            float noiseThreshold = 0.5f;
            if (alertnessLevel > 50f) noiseThreshold = 0.2f; // Más sensible cuando está alerta

            if (playerController.currentNoiseLevel > noiseThreshold)
            {
                // No detecta directamente, pero va a investigar
                StartInvestigating(player.position);
                return;
            }
        }

        if (playerDetected)
        {
            StartChasing();
        }
    }

    float CalculateDetectionChance()
    {
        float baseChance = 0.8f;

        // Factores que reducen la detección
        if (playerController.currentNoiseLevel < 1f) baseChance -= 0.3f; // Jugador sigiloso

        // Factores que aumentan la detección
        if (alertnessLevel > 50f) baseChance += 0.2f; // Guardia alerta
        if (playerController.currentNoiseLevel > 2f) baseChance += 0.3f; // Jugador corriendo

        return Mathf.Clamp01(baseChance);
    }

    void StartInvestigating(Vector3 position)
    {
        if (currentState == GuardState.Chasing) return; // No interrumpir persecución

        previousState = currentState;
        currentState = GuardState.Investigating;
        investigatePosition = position;
        investigateTimer = 0f;
        alertnessLevel = Mathf.Min(100f, alertnessLevel + 25f);

        Debug.Log("¿Qué fue eso? Voy a investigar...");
    }

    void Investigate()
    {
        float distance = Vector3.Distance(transform.position, investigatePosition);

        if (distance > 1f)
        {
            MoveTowards(investigatePosition, patrolSpeed * 1.2f);
        }
        else
        {
            // Llegó al punto de investigación, buscar alrededor
            investigateTimer += Time.deltaTime;

            // Rotar mirando alrededor
            transform.Rotate(0, 45f * Time.deltaTime, 0);

            if (investigateTimer >= investigateTime)
            {
                // Terminar investigación
                if (hasSeenPlayer)
                {
                    currentState = GuardState.Searching;
                }
                else
                {
                    currentState = previousState;
                }
            }
        }

        // Seguir buscando al jugador mientras investiga
        CheckForPlayer();
    }

    void ChasePlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        hasSeenPlayer = true;
        alertnessLevel = 100f;

        // MEJORADO: Lógica de persecución con distancia mínima
        if (distanceToPlayer > minChaseDistance)
        {
            // Solo moverse si está lejos
            lastKnownPlayerPosition = player.position;
            MoveTowards(player.position, chaseSpeed);
            losePlayerTimer = 0f; // Resetear timer de pérdida
        }
        else
        {
            // Si está muy cerca, no moverse tanto y capturar más fácil
            if (distanceToPlayer < 1.5f)
            {
                CapturePlayer();
                return;
            }

            // Moverse más lento cuando está cerca para no "pegarse"
            MoveTowards(player.position, chaseSpeed * 0.3f);
        }

        // MEJORADO: Sistema de pérdida de jugador más realista
        if (!HasLineOfSight(player.position))
        {
            losePlayerTimer += Time.deltaTime;

            if (losePlayerTimer >= losePlayerTime || distanceToPlayer > maxChaseDistance)
            {
                currentState = GuardState.Searching;
                losePlayerTimer = 0f;
                Debug.Log("Lo perdí de vista... pero sé dónde estaba.");
            }
        }
        else
        {
            losePlayerTimer = 0f; // Reiniciar timer si lo ve
        }
    }

    void SearchLastKnownPosition()
    {
        float distance = Vector3.Distance(transform.position, lastKnownPlayerPosition);

        if (distance > 2f)
        {
            MoveTowards(lastKnownPlayerPosition, chaseSpeed * 0.8f);
        }
        else
        {
            // Buscar en área
            investigateTimer += Time.deltaTime;
            transform.Rotate(0, 60f * Time.deltaTime, 0);

            if (investigateTimer >= investigateTime * 1.5f)
            {
                currentState = GuardState.Returning;
                investigateTimer = 0f;
            }
        }

        CheckForPlayer(); // Puede volver a detectar
    }

    void ReturnToPatrol()
    {
        Transform nearestPatrolPoint = GetNearestPatrolPoint();
        float distance = Vector3.Distance(transform.position, nearestPatrolPoint.position);

        if (distance > 1f)
        {
            MoveTowards(nearestPatrolPoint.position, patrolSpeed);
        }
        else
        {
            currentState = GuardState.Patrolling;
            // Encontrar el índice del patrol point más cercano
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == nearestPatrolPoint)
                {
                    currentPatrolIndex = i;
                    break;
                }
            }
        }
    }

    void StartChasing()
    {
        currentState = GuardState.Chasing;
        alertnessLevel = 100f;
        hasSeenPlayer = true;
        losePlayerTimer = 0f; // NUEVO: Resetear timer

        // Sonido de alerta
        if (alertSounds.Length > 0 && audioSource != null)
        {
            audioSource.PlayOneShot(alertSounds[Random.Range(0, alertSounds.Length)]);
        }

        Debug.Log("¡Te vi! ¡No puedes escapar!");
    }

    void CapturePlayer()
    {
        Debug.Log("¡Te atrapé! Reiniciando...");
        // Aquí llamarías al GameManager para reiniciar
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.RestartNight();
        }
    }

    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position).normalized;

        // Usar Rigidbody para movimiento con colisiones físicas
        Rigidbody guardRb = GetComponent<Rigidbody>();
        if (guardRb != null)
        {
            // MEJORADO: Movimiento más controlado con límite de velocidad
            Vector3 movement = direction * speed * Time.fixedDeltaTime;
            Vector3 newPosition = guardRb.position + movement;
            guardRb.MovePosition(newPosition);

            // Limitar velocidad para evitar comportamientos extraños
            if (guardRb.linearVelocity.magnitude > speed)
            {
                guardRb.linearVelocity = guardRb.linearVelocity.normalized * speed;
            }
        }
        else
        {
            // Fallback al movimiento simple
            transform.position += direction * speed * Time.deltaTime;
        }

        // Rotación suave hacia el objetivo
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    bool HasLineOfSight(Vector3 target)
    {
        Vector3 direction = (target - (transform.position + Vector3.up)).normalized;
        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up, direction, out hit, detectionRange))
        {
            return hit.collider.CompareTag("Player");
        }
        return false;
    }

    Transform GetNearestPatrolPoint()
    {
        Transform nearest = patrolPoints[0];
        float nearestDistance = Vector3.Distance(transform.position, nearest.position);

        foreach (Transform point in patrolPoints)
        {
            float distance = Vector3.Distance(transform.position, point.position);
            if (distance < nearestDistance)
            {
                nearest = point;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    IEnumerator SuspicionDecay()
    {
        while (true)
        {
            yield return new WaitForSeconds(suspicionDecayTime);
            if (currentState != GuardState.Chasing && currentState != GuardState.Investigating)
            {
                alertnessLevel = Mathf.Max(0f, alertnessLevel - 10f);
            }
        }
    }

    void UpdateAnimations()
    {
        if (guardAnimator == null) return;

        guardAnimator.SetFloat("Speed", GetCurrentSpeed());
        guardAnimator.SetBool("IsChasing", currentState == GuardState.Chasing);
        guardAnimator.SetBool("IsInvestigating", currentState == GuardState.Investigating);
        guardAnimator.SetFloat("Alertness", alertnessLevel / 100f);
    }

    float GetCurrentSpeed()
    {
        switch (currentState)
        {
            case GuardState.Patrolling: return patrolSpeed / 4f;
            case GuardState.Chasing: return 1f;
            case GuardState.Investigating: return patrolSpeed / 3f;
            default: return 0f;
        }
    }

    void CreateBasicPatrolPoints()
    {
        GameObject patrolParent = new GameObject("PatrolPoints");
        patrolPoints = new Transform[4];

        Vector3[] positions = {
            transform.position + new Vector3(5, 0, 0),
            transform.position + new Vector3(5, 0, 5),
            transform.position + new Vector3(-5, 0, 5),
            transform.position + new Vector3(-5, 0, 0)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject point = new GameObject("PatrolPoint_" + i);
            point.transform.parent = patrolParent.transform;
            point.transform.position = positions[i];
            patrolPoints[i] = point.transform;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Rango de detección
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Rango auditivo
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        // NUEVO: Distancia mínima de persecución
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minChaseDistance);

        // Campo de visión
        Gizmos.color = Color.cyan;
        Vector3 leftBoundary = Quaternion.AngleAxis(-fieldOfViewAngle / 2f, Vector3.up) * transform.forward * detectionRange;
        Vector3 rightBoundary = Quaternion.AngleAxis(fieldOfViewAngle / 2f, Vector3.up) * transform.forward * detectionRange;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
    }
}