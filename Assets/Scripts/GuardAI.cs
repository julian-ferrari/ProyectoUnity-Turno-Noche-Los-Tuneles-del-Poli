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
    public float fieldOfViewAngle = 60f;

    [Header("Chasing")]
    public float chaseSpeed = 4f;
    public float investigateTime = 10f;
    public float maxChaseDistance = 20f;
    public float minChaseDistance = 2f;
    public float losePlayerTime = 5f;

    [Header("AI Behavior")]
    public bool canOpenDoors = true;
    public bool canHearFootsteps = true;
    public bool canSeeFlashlight = true;
    public float suspicionDecayTime = 5f;
    public float alertnessLevel = 0f;

    [Header("Animation & Audio")]
    public Animator guardAnimator;
    public AudioSource audioSource;
    public AudioClip[] alertSounds;
    public AudioClip[] patrolSounds;

    [Header("Audio - Nuevo Sistema")]
    public AudioSource coughAudioSource; // AudioSource separado para tos
    public AudioSource chaseAudioSource; // AudioSource separado para música de persecución
    [Space(5)]
    public AudioClip[] coughSounds; // Sonidos de tos
    public AudioClip chaseMusic; // Música de persecución
    public AudioClip captureSound; // Sonido al atrapar jugador
    [Space(5)]
    public float minCoughInterval = 5f;
    public float maxCoughInterval = 10f;
    public float coughDetectionRange = 10f; // Distancia a la que el jugador puede oír la tos
    [Space(5)]
    public float chaseMusicVolume = 0.6f;
    public float chaseMusicFadeTime = 1f;

    [Header("Patrol Improvements")]
    public bool loopPatrol = true;
    public bool randomPatrol = false;
    public float pointReachedDistance = 0.5f;

    [Header("Debug & Testing")]
    public bool debugMode = true;
    public KeyCode testDetectionKey = KeyCode.T;
    public KeyCode resetGuardKey = KeyCode.R;
    public KeyCode togglePatrolKey = KeyCode.P;

    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private float investigateTimer = 0f;
    private float losePlayerTimer = 0f;
    private Transform player;
    private PlayerController playerController;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 investigatePosition;
    private bool hasSeenPlayer = false;
    public bool patrolEnabled = true;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private int initialPatrolIndex;

    // Variables de audio
    private float nextCoughTime = 0f;
    private bool isChasingWithMusic = false;

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

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialPatrolIndex = 0;

        if (patrolPoints.Length == 0)
        {
            CreateBasicPatrolPoints();
        }

        SetupAudio();
        StartCoroutine(SuspicionDecay());
        ScheduleNextCough();
    }

    void SetupAudio()
    {
        // Configurar AudioSource para tos (3D espacial)
        if (coughAudioSource == null)
        {
            GameObject coughObj = new GameObject("CoughAudioSource");
            coughObj.transform.SetParent(transform);
            coughObj.transform.localPosition = Vector3.zero;

            coughAudioSource = coughObj.AddComponent<AudioSource>();
            coughAudioSource.spatialBlend = 1f; // 3D sound
            coughAudioSource.minDistance = 5f;
            coughAudioSource.maxDistance = coughDetectionRange;
            coughAudioSource.rolloffMode = AudioRolloffMode.Linear;
            coughAudioSource.playOnAwake = false;
        }

        // Configurar AudioSource para música de persecución (2D)
        if (chaseAudioSource == null)
        {
            GameObject chaseObj = new GameObject("ChaseAudioSource");
            chaseObj.transform.SetParent(transform);
            chaseObj.transform.localPosition = Vector3.zero;

            chaseAudioSource = chaseObj.AddComponent<AudioSource>();
            chaseAudioSource.spatialBlend = 0f; // 2D sound (música ambiental)
            chaseAudioSource.loop = true;
            chaseAudioSource.volume = 0f; // Empezar en silencio
            chaseAudioSource.playOnAwake = false;
        }

        // Asignar música de persecución
        if (chaseMusic != null)
        {
            chaseAudioSource.clip = chaseMusic;
        }

        Debug.Log("Sistema de audio del guardia configurado");
    }

    void Update()
    {
#if UNITY_EDITOR
        TestInputs();
#endif

        UpdateAlertness();
        UpdateCoughSystem();

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

        UpdateAnimations();
        UpdateChaseMusic();
    }

    void TestInputs()
    {
        if (!debugMode) return;

        if (Input.GetKeyDown(testDetectionKey))
        {
            Debug.Log("TEST: Forzando detección del jugador");
            StartChasing();
        }

        if (Input.GetKeyDown(resetGuardKey))
        {
            Debug.Log("TEST: Reseteando guardia");
            ResetGuard();
        }

        if (Input.GetKeyDown(togglePatrolKey))
        {
            patrolEnabled = !patrolEnabled;
            Debug.Log("TEST: Patrulla " + (patrolEnabled ? "activada" : "desactivada"));
        }
    }

    // ===================== SISTEMA DE AUDIO =====================

    void UpdateCoughSystem()
    {
        // Solo toser si el jugador está cerca
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= coughDetectionRange && Time.time >= nextCoughTime)
        {
            PlayCough();
            ScheduleNextCough();
        }
    }

    void PlayCough()
    {
        if (coughSounds == null || coughSounds.Length == 0 || coughAudioSource == null) return;

        AudioClip cough = coughSounds[Random.Range(0, coughSounds.Length)];
        coughAudioSource.PlayOneShot(cough);

        Debug.Log("Guardia tosió - Jugador puede oírlo");
    }

    void ScheduleNextCough()
    {
        float interval = Random.Range(minCoughInterval, maxCoughInterval);
        nextCoughTime = Time.time + interval;
    }

    void UpdateChaseMusic()
    {
        // Activar música durante persecución
        if (currentState == GuardState.Chasing && !isChasingWithMusic)
        {
            StartCoroutine(FadeInChaseMusic());
            isChasingWithMusic = true;
        }
        // Desactivar música cuando deja de perseguir
        else if (currentState != GuardState.Chasing && isChasingWithMusic)
        {
            StartCoroutine(FadeOutChaseMusic());
            isChasingWithMusic = false;
        }
    }

    IEnumerator FadeInChaseMusic()
    {
        if (chaseAudioSource == null || chaseMusic == null) yield break;

        if (!chaseAudioSource.isPlaying)
        {
            chaseAudioSource.Play();
        }

        float elapsed = 0f;
        float startVolume = chaseAudioSource.volume;

        while (elapsed < chaseMusicFadeTime)
        {
            elapsed += Time.deltaTime;
            chaseAudioSource.volume = Mathf.Lerp(startVolume, chaseMusicVolume, elapsed / chaseMusicFadeTime);
            yield return null;
        }

        chaseAudioSource.volume = chaseMusicVolume;
    }

    IEnumerator FadeOutChaseMusic()
    {
        if (chaseAudioSource == null) yield break;

        float elapsed = 0f;
        float startVolume = chaseAudioSource.volume;

        while (elapsed < chaseMusicFadeTime)
        {
            elapsed += Time.deltaTime;
            chaseAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / chaseMusicFadeTime);
            yield return null;
        }

        chaseAudioSource.volume = 0f;
        chaseAudioSource.Stop();
    }

    // ===================== FIN SISTEMA DE AUDIO =====================

    void UpdateAlertness()
    {
        float alertMultiplier = 1f + (alertnessLevel / 100f);
        patrolSpeed = 2f * alertMultiplier;
        detectionRange = 5f * alertMultiplier;
    }

    void Patrol()
    {
        if (!patrolEnabled || patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        float distance = Vector3.Distance(transform.position, targetPoint.position);

        if (distance < pointReachedDistance)
        {
            waitTimer += Time.deltaTime;

            if (guardAnimator != null)
                guardAnimator.SetBool("IsWaiting", true);

            if (waitTimer >= waitTime)
            {
                waitTimer = 0f;

                if (guardAnimator != null)
                    guardAnimator.SetBool("IsWaiting", false);

                if (randomPatrol)
                {
                    int newIndex;
                    do
                    {
                        newIndex = Random.Range(0, patrolPoints.Length);
                    } while (newIndex == currentPatrolIndex && patrolPoints.Length > 1);

                    currentPatrolIndex = newIndex;
                }
                else
                {
                    currentPatrolIndex++;

                    if (currentPatrolIndex >= patrolPoints.Length)
                    {
                        currentPatrolIndex = loopPatrol ? 0 : patrolPoints.Length - 1;
                    }
                }

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

        if (distanceToPlayer <= detectionRange)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            if (angleToPlayer < fieldOfViewAngle / 2f)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out hit, detectionRange))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        float detectionChance = CalculateDetectionChance();
                        if (Random.value < detectionChance)
                        {
                            playerDetected = true;
                        }
                    }
                }
            }
        }

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

        if (canHearFootsteps && distanceToPlayer <= hearingRange)
        {
            float noiseThreshold = 0.5f;
            if (alertnessLevel > 50f) noiseThreshold = 0.2f;

            if (playerController.currentNoiseLevel > noiseThreshold)
            {
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

        if (playerController.currentNoiseLevel < 1f) baseChance -= 0.3f;
        if (alertnessLevel > 50f) baseChance += 0.2f;
        if (playerController.currentNoiseLevel > 2f) baseChance += 0.3f;

        return Mathf.Clamp01(baseChance);
    }

    void StartInvestigating(Vector3 position)
    {
        if (currentState == GuardState.Chasing) return;

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
            investigateTimer += Time.deltaTime;
            transform.Rotate(0, 45f * Time.deltaTime, 0);

            if (investigateTimer >= investigateTime)
            {
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

        CheckForPlayer();
    }

    void ChasePlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        hasSeenPlayer = true;
        alertnessLevel = 100f;

        if (distanceToPlayer > minChaseDistance + 1f)
        {
            lastKnownPlayerPosition = player.position;
            MoveTowards(player.position, chaseSpeed);
            losePlayerTimer = 0f;
        }
        else if (distanceToPlayer > 1.5f)
        {
            MoveTowards(player.position, chaseSpeed * 0.5f);
            losePlayerTimer = 0f;
        }
        else
        {
            CapturePlayer();
            return;
        }

        if (!HasLineOfSight(player.position))
        {
            losePlayerTimer += Time.deltaTime;

            float loseTimeModifier = distanceToPlayer > 10f ? 2f : 1f;

            if (losePlayerTimer >= (losePlayerTime / loseTimeModifier) || distanceToPlayer > maxChaseDistance)
            {
                currentState = GuardState.Searching;
                losePlayerTimer = 0f;
                Debug.Log("Lo perdí de vista... pero sé dónde estaba.");
            }
        }
        else
        {
            losePlayerTimer = 0f;
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
            investigateTimer += Time.deltaTime;
            transform.Rotate(0, 60f * Time.deltaTime, 0);

            if (investigateTimer >= investigateTime * 1.5f)
            {
                currentState = GuardState.Returning;
                investigateTimer = 0f;
            }
        }

        CheckForPlayer();
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

    public void StartChasing()
    {
        currentState = GuardState.Chasing;
        alertnessLevel = 100f;
        hasSeenPlayer = true;
        losePlayerTimer = 0f;

        if (alertSounds.Length > 0 && audioSource != null)
        {
            audioSource.PlayOneShot(alertSounds[Random.Range(0, alertSounds.Length)]);
        }

        Debug.Log("¡Te vi! ¡No puedes escapar!");
    }

    void CapturePlayer()
    {
        Debug.Log("¡Te atrapé! Reiniciando...");

        // Reproducir sonido de captura
        if (captureSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(captureSound);
        }

        // Detener música de persecución inmediatamente
        if (chaseAudioSource != null && chaseAudioSource.isPlaying)
        {
            chaseAudioSource.Stop();
            chaseAudioSource.volume = 0f;
        }
        isChasingWithMusic = false;

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            // Pequeño delay para que se escuche el sonido de captura
            StartCoroutine(RestartAfterCapture(gameManager));
        }
    }

    IEnumerator RestartAfterCapture(GameManager gameManager)
    {
        yield return new WaitForSeconds(1.5f);
        gameManager.RestartNight();
    }

    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position).normalized;

        Rigidbody guardRb = GetComponent<Rigidbody>();
        if (guardRb != null)
        {
            guardRb.freezeRotation = true;
            Vector3 movement = direction * speed * Time.fixedDeltaTime;
            Vector3 newPosition = guardRb.position + movement;
            guardRb.MovePosition(newPosition);

            if (guardRb.linearVelocity.magnitude > speed)
            {
                guardRb.linearVelocity = guardRb.linearVelocity.normalized * speed;
            }
        }
        else
        {
            transform.position += direction * speed * Time.deltaTime;
        }

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

    public void ResetGuard()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        currentState = GuardState.Patrolling;
        previousState = GuardState.Patrolling;
        currentPatrolIndex = initialPatrolIndex;

        waitTimer = 0f;
        investigateTimer = 0f;
        losePlayerTimer = 0f;
        hasSeenPlayer = false;
        alertnessLevel = 0f;

        // Detener música de persecución
        if (chaseAudioSource != null && chaseAudioSource.isPlaying)
        {
            chaseAudioSource.Stop();
            chaseAudioSource.volume = 0f;
        }
        isChasingWithMusic = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("Guardia reseteado a posición inicial");
    }

    public void AddPatrolPoint(Vector3 position)
    {
        GameObject newPoint = new GameObject("PatrolPoint_" + patrolPoints.Length);
        newPoint.transform.position = position;

        System.Array.Resize(ref patrolPoints, patrolPoints.Length + 1);
        patrolPoints[patrolPoints.Length - 1] = newPoint.transform;

        Debug.Log("Nuevo punto de patrulla agregado: " + position);
    }

    public void ClearPatrolPoints()
    {
        foreach (Transform point in patrolPoints)
        {
            if (point != null && point.gameObject.name.StartsWith("PatrolPoint_"))
            {
                Destroy(point.gameObject);
            }
        }

        patrolPoints = new Transform[0];
        Debug.Log("Todos los puntos de patrulla eliminados");
    }

    void OnDrawGizmosSelected()
    {
        if (!debugMode) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minChaseDistance);

        // Mostrar rango de tos
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, coughDetectionRange);

        Gizmos.color = new Color(0, 1, 1, 0.2f);
        Vector3 leftBoundary = Quaternion.AngleAxis(-fieldOfViewAngle / 2, Vector3.up) * transform.forward * detectionRange;
        Vector3 rightBoundary = Quaternion.AngleAxis(fieldOfViewAngle / 2, Vector3.up) * transform.forward * detectionRange;

        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);
        Gizmos.DrawLine(transform.position + rightBoundary, transform.position + leftBoundary);

        switch (currentState)
        {
            case GuardState.Patrolling:
                if (patrolPoints.Length > 0)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, patrolPoints[currentPatrolIndex].position);
                    Gizmos.DrawWireSphere(patrolPoints[currentPatrolIndex].position, 0.5f);
                }
                break;

            case GuardState.Investigating:
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, investigatePosition);
                Gizmos.DrawWireSphere(investigatePosition, 0.7f);
                break;

            case GuardState.Chasing:
                Gizmos.color = Color.red;
                if (player != null)
                    Gizmos.DrawLine(transform.position, player.position);
                break;

            case GuardState.Searching:
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
                Gizmos.DrawWireSphere(lastKnownPlayerPosition, 1f);
                break;
        }
    }
}