using UnityEngine;

public enum DroneState { Patrol, Locking, LostTarget, Kamikaze, Dead, Paralyzed }

public class DroneBrain : MonoBehaviour
{
    [Header("References")]
    public DroneSensor sensor;
    public DroneController controller;
    private HealthSystem health;
    private FactionIdentity myFaction;

    [Header("Audio")]
    public AudioSource droneAudioSource;
    public AudioClip droneClip;
    public float droneLoopEndTime = 10f;
    public AudioClip explosionImpactClip;

    [Header("Visual Effects")]
    public GameObject explosionPrefab;

    [Header("Flight Settings")]
    public float patrolRadius = 30f;
    public float patrolSpeed = 6f;
    public float diveSpeed = 35f;

    [Header("Combat Settings")]
    public float lockTime = 2.5f;
    public float memoryTime = 3.0f;
    public float explosionDamage = 80f;
    public float explosionRadius = 6f;

    private DroneState currentState = DroneState.Patrol;
    public DroneState CurrentState => currentState;

    private Transform currentTarget;
    private Vector3 lastKnownTargetPos;
    private float stateTimer = 0f;
    private float paralyzedTimer = 0f;
    private DroneState preParalyzedState;

    private Vector3 currentPatrolPoint;

    private void Start()
    {
        if (sensor == null) sensor = GetComponentInChildren<DroneSensor>();
        if (controller == null) controller = GetComponentInChildren<DroneController>();

        
        if (droneAudioSource == null) droneAudioSource = GetComponent<AudioSource>();
        if (droneAudioSource == null) droneAudioSource = gameObject.AddComponent<AudioSource>();

        
        droneAudioSource.spatialBlend = 1f;
        droneAudioSource.minDistance = 10f; // Hasta 10 metros se oye al máximo
        droneAudioSource.maxDistance = 60f; // Se deja de oír a los 60 metros
        droneAudioSource.rolloffMode = AudioRolloffMode.Linear;

        myFaction = GetComponent<FactionIdentity>();
        health = GetComponent<HealthSystem>();

        if (health != null)
        {
            health.onDeath.AddListener(HandleDeath);
        }

        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        SetRandomPatrolDestination();
        StartDroneAudio();
    }

    private void Update()
    {
        UpdateDroneAudio();

        if (currentState == DroneState.Dead) return;

        bool isPlayer = false;
        Transform visibleTarget = null;

        if (sensor != null)
        {
            visibleTarget = sensor.GetBestTarget(out isPlayer);
        }

        switch (currentState)
        {
            case DroneState.Patrol:
                UpdatePatrol(visibleTarget);
                break;
            case DroneState.Locking:
                UpdateLocking(visibleTarget);
                break;
            case DroneState.LostTarget:
                UpdateLostTarget(visibleTarget);
                break;
            case DroneState.Kamikaze:
                UpdateKamikaze();
                break;
            case DroneState.Paralyzed:
                UpdateParalyzed();
                break;
        }
    }

    private void StartDroneAudio()
    {
        if (droneAudioSource == null || droneClip == null) return;

        droneAudioSource.clip = droneClip;
        
        droneAudioSource.loop = false;
        droneAudioSource.Play();
    }

    private void UpdateDroneAudio()
    {
        if (droneAudioSource == null || droneClip == null) return;

        if (currentState == DroneState.Dead)
        {
            if (droneAudioSource.isPlaying) droneAudioSource.Stop();
            return;
        }

        
        float loopEnd = Mathf.Min(droneLoopEndTime, droneClip.length);

        if (droneAudioSource.isPlaying && droneAudioSource.time >= loopEnd)
        {
            droneAudioSource.Stop();
            droneAudioSource.Play(); 
        }
        else if (!droneAudioSource.isPlaying)
        {
            StartDroneAudio();
        }
    }

    public void Paralyze(float duration)
    {
        if (currentState == DroneState.Dead || currentState == DroneState.Kamikaze) return;

        if (currentState != DroneState.Paralyzed)
        {
            preParalyzedState = currentState;
            currentState = DroneState.Paralyzed;

            if (TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        paralyzedTimer = duration;
    }

    private void UpdateParalyzed()
    {
        paralyzedTimer -= Time.deltaTime;
        if (paralyzedTimer <= 0f)
        {
            currentState = preParalyzedState;
        }
    }

    public void ReceiveAlert(Transform target)
    {
        if (currentState == DroneState.Dead || currentState == DroneState.Kamikaze) return;

        if (target != null)
        {
            currentTarget = target;
            lastKnownTargetPos = target.position;
            currentState = DroneState.Locking;
            stateTimer = 0f;

            AlertNearbyAllies(currentTarget);
        }
    }

    private void UpdatePatrol(Transform visibleTarget)
    {
        if (visibleTarget != null)
        {
            currentTarget = visibleTarget;
            lastKnownTargetPos = visibleTarget.position;
            currentState = DroneState.Locking;
            stateTimer = 0f;

            AlertNearbyAllies(currentTarget);
            return;
        }

        Vector3 dir = (currentPatrolPoint - transform.position).normalized;
        transform.position += dir * patrolSpeed * Time.deltaTime;
        controller.RotateTowardsPoint(currentPatrolPoint);

        if (Vector3.Distance(transform.position, currentPatrolPoint) < 1f)
        {
            SetRandomPatrolDestination();
        }
    }

    private void UpdateLocking(Transform visibleTarget)
    {
        if (currentTarget == null)
        {
            currentState = DroneState.LostTarget;
            stateTimer = 0f;
            return;
        }

        controller.RotateTowardsPoint(currentTarget.position);

        bool stillVisible = sensor != null && sensor.IsTargetVisible(currentTarget);

        if (stillVisible)
        {
            lastKnownTargetPos = currentTarget.position;
            stateTimer += Time.deltaTime;

            if (stateTimer >= lockTime)
            {
                currentState = DroneState.Kamikaze;

                if (TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                }
            }
        }
        else
        {
            currentState = DroneState.LostTarget;
            stateTimer = 0f;
        }
    }

    private void UpdateLostTarget(Transform visibleTarget)
    {
        controller.RotateTowardsPoint(lastKnownTargetPos);

        if (visibleTarget != null)
        {
            currentTarget = visibleTarget;
            currentState = DroneState.Locking;
            return;
        }

        stateTimer += Time.deltaTime;
        if (stateTimer >= memoryTime)
        {
            currentTarget = null;
            currentState = DroneState.Patrol;
            SetRandomPatrolDestination();
        }
    }

    private void UpdateKamikaze()
    {
        Vector3 direction = (lastKnownTargetPos - transform.position).normalized;

        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = direction * diveSpeed;
        }
        else
        {
            transform.position += direction * diveSpeed * Time.deltaTime;
        }

        controller.RotateTowardsPoint(lastKnownTargetPos);

        if (Vector3.Distance(transform.position, lastKnownTargetPos) < 1.5f)
        {
            Explode();
        }
    }

    private void SetRandomPatrolDestination()
    {
        bool validPointFound = false;
        int attempts = 0;

        while (!validPointFound && attempts < 10)
        {
            attempts++;
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
            randomDir.y = Random.Range(-5f, 5f);

            Vector3 potentialPoint = transform.position + randomDir;

            Vector3 dirToPoint = (potentialPoint - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, potentialPoint);

            if (!Physics.Raycast(transform.position, dirToPoint, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                currentPatrolPoint = potentialPoint;
                validPointFound = true;
            }
        }

        if (!validPointFound)
        {
            currentPatrolPoint = transform.position - transform.forward * 5f;
        }
    }

    private void AlertNearbyAllies(Transform target)
    {
        Collider[] allies = Physics.OverlapSphere(transform.position, 50f);
        foreach (Collider col in allies)
        {
            SquadManager squad = col.GetComponentInParent<SquadManager>();
            if (squad != null)
            {
                squad.AlertSquad(target);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == DroneState.Kamikaze)
        {
            Explode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentState == DroneState.Kamikaze)
        {
            Explode();
        }
    }

    public void Explode()
    {
        if (currentState == DroneState.Dead) return;
        HandleDeath();
    }

    private void HandleDeath()
    {
        if (currentState == DroneState.Dead) return;
        currentState = DroneState.Dead;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        System.Collections.Generic.HashSet<HealthSystem> damagedTargets = new System.Collections.Generic.HashSet<HealthSystem>();

        foreach (Collider hit in hitColliders)
        {
            FactionIdentity hitFaction = hit.GetComponentInParent<FactionIdentity>();

            if (hitFaction != null && myFaction != null && !myFaction.IsEnemy(hitFaction.myFaction)) continue;

            HealthSystem targetHealth = hit.GetComponentInParent<HealthSystem>();
            if (targetHealth != null && !targetHealth.IsDead && !damagedTargets.Contains(targetHealth))
            {
                targetHealth.TakeDamage(explosionDamage);
                damagedTargets.Add(targetHealth);
            }
        }

        if (droneAudioSource != null) droneAudioSource.Stop();

        if (explosionImpactClip != null)
        {
            AudioSource.PlayClipAtPoint(explosionImpactClip, transform.position);
        }

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (droneAudioSource != null) droneAudioSource.Stop();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(currentPatrolPoint, 0.5f);
        Gizmos.DrawLine(transform.position, currentPatrolPoint);
    }

    private void OnDestroy()
    {
        if (health != null) health.onDeath.RemoveListener(HandleDeath);
    }
}
