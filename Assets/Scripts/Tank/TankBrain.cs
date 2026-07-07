using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum TankState { Patrol, Lock, Shoot, Dead }
public class TankBrain : MonoBehaviour
{

    [Header("References")]
    public TankSensor sensor;
    public TankController controller;
    public NavMeshAgent agent;
    public Transform[] waypoints;

    [Header("Configuration")]
    public float lockTime = 2.5f;
    public float targetLossGraceTime = 0.5f; //Time to wait before continue patrolling
    public float shootDuration = 0.5f; // Tiempo que el tanque se queda en estado 'Shoot' tras disparar

    [Header("Audio")]
    public AudioSource tankAudioSource;
    public AudioClip tankClip;
    public float tankLoopStartTime = 21f;
    public float tankLoopEndTime = 35f;
    public AudioClip explosionImpactClip;

    [Header("Visual Effects")]
    public GameObject explosionPrefab;

    [Header("Explosion Settings")]
    public float explosionDamage = 150f;
    public float explosionRadius = 10f;

    [Header("Camera Shake Settings")]
    public float shakeRadius = 40f;
    public float shakeDuration = 0.8f;
    public float shakeIntensity = 0.6f;

    private TankState currentState = TankState.Patrol;
    public TankState CurrentState => currentState;
    private int currentWaypointIndex = 0;
    private float lockTimer = 0f;
    private Transform currentTarget;
    private float lostTargetTimer = 0f;
    private float shootTimer = 0f;
    private HealthSystem health;
    private FactionIdentity myFaction;

    // Start is called before the first frame update
    void Start()
    {
        myFaction = GetComponent<FactionIdentity>();
        if (tankAudioSource == null) tankAudioSource = GetComponent<AudioSource>();
        if (tankAudioSource == null) tankAudioSource = gameObject.AddComponent<AudioSource>();

        // Evitar que el tanque salga volando por los aires al recibir disparos físicos
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            rb.mass = 50000f; // Peso hiperrealista
            rb.isKinematic = true; // Como se mueve por NavMesh, se bloquean las fuerzas físicas externas
        }
        
        Rigidbody parentRb = GetComponentInParent<Rigidbody>();
        if (parentRb != null)
        {
            parentRb.mass = 50000f;
            parentRb.isKinematic = true;
        }

        controller.SetPatrolAnimation(true);

        if (waypoints != null && waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position); //Start patrolling routes
        }
        else
        {
            SetLongDistancePatrolPoint();
        }

        health = GetComponent<HealthSystem>();
        if (health != null)
        {
            health.onDeath.AddListener(HandleDeath);
        }

        StartTankAudio();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTankAudio();

        if (currentState == TankState.Dead) return;

        switch (currentState)
        {
            case TankState.Patrol:
                Debug.Log("TankState: Patrol");
                UpdatePatrol();
                break;
            case TankState.Lock:
                Debug.Log("TankState: Lock");
                UpdateLock();
                break;
            case TankState.Shoot:
                Debug.Log("TankState: Shoot");
                UpdateShoot();
                break;
        }
    }

    private void StartTankAudio()
    {
        if (tankAudioSource == null || tankClip == null) return;

        tankAudioSource.clip = tankClip;
        tankAudioSource.loop = true;
        tankAudioSource.time = Mathf.Clamp(tankLoopStartTime, 0f, Mathf.Max(0f, tankClip.length - 0.01f));
        tankAudioSource.Play();
    }

    private void UpdateTankAudio()
    {
        if (tankAudioSource == null || tankClip == null) return;

        if (currentState == TankState.Dead)
        {
            if (tankAudioSource.isPlaying) tankAudioSource.Stop();
            return;
        }

        if (!tankAudioSource.isPlaying)
        {
            StartTankAudio();
        }

        float loopStart = Mathf.Clamp(tankLoopStartTime, 0f, Mathf.Max(0f, tankClip.length - 0.01f));
        float loopEnd = Mathf.Clamp(tankLoopEndTime, loopStart, tankClip.length);
        if (tankAudioSource.time >= loopEnd)
        {
            tankAudioSource.time = loopStart;
        }
    }


    private void UpdatePatrol()
    {
        
        //1. Follow NavMesh route
        bool shouldSetNewDestination = false;

        if (!agent.pathPending)
        {
            // Llegó al destino
            if (agent.remainingDistance < 1.0f) 
            {
                shouldSetNewDestination = true;
            }
            // Si el camino es inválido o se queda atascado
            else if (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                shouldSetNewDestination = true;
            }
        }

        if (shouldSetNewDestination)
        { 
            if (waypoints != null && waypoints.Length > 0)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length; //Loop
                agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
            else
            {
                SetLongDistancePatrolPoint();
            }
        }

        //2. Detect Enemies
        currentTarget = sensor.GetDetectedEnemy();
        if (currentTarget != null)
        {
            agent.isStopped = true; //Tank stops to aim
            agent.ResetPath();
            agent.velocity = Vector3.zero;

            agent.updateRotation = false;

            controller.SetPatrolAnimation(false);
            lockTimer = 0f;
            currentState = TankState.Lock;
        }
        
    }

    private void UpdateLock()
    {
        controller.RotateTowards(currentTarget);

        // En lugar de usar el cono de visión, se comprueba si el objetivo sigue visible directamente
        bool stillVisible = sensor.IsTargetVisible(currentTarget);

        //Still in sight verification
        if (stillVisible)
        {
            lostTargetTimer = 0f;
            lockTimer += Time.deltaTime;

            if (lockTimer >= lockTime)
            {
                shootTimer = 0f; // Reset del temporizador de disparo
                currentState = TankState.Shoot;
            }
        }
        else
        {
            lostTargetTimer += Time.deltaTime;
            if (lostTargetTimer >= targetLossGraceTime)
            {
                ResumePatrol(); // Lost target, reset
            }
        }
    }

    private void UpdateShoot()
    {
        // Dispara solo en el primer instante del estado Shoot
        if (shootTimer == 0f)
        {
            controller.Fire(currentTarget);
        }

        // Permanece en este estado durante 'shootDuration' segundos
        shootTimer += Time.deltaTime;

        if (shootTimer >= shootDuration)
        {
            lockTimer = 0f; // Reset del tiempo de lock para el próximo disparo
            currentState = TankState.Lock; // Vuelve a fijar el objetivo
        }
    }
    private void ResumePatrol()
    {
        agent.isStopped = false;
        agent.updateRotation = true;

        if (waypoints != null && waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
        else
        {
            SetLongDistancePatrolPoint();
        }

        controller.SetPatrolAnimation(true);
        currentState = TankState.Patrol;
    }

    private void SetLongDistancePatrolPoint()
    {
        bool pointFound = false;
        int attempts = 0;
        
        while (!pointFound && attempts < 15)
        {
            attempts++;
            // Punto aleatorio pero muy lejano (ej. entre 50 y 150 metros)
            Vector2 randomDir = Random.insideUnitCircle.normalized * Random.Range(50f, 150f);
            Vector3 targetPos = transform.position + new Vector3(randomDir.x, 0, randomDir.y);

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 20f, NavMesh.AllAreas))
            {
                // Se calcula una ruta completa para asegurar que puede llegar físicamente sin atascarse
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(hit.position);
                    pointFound = true;
                }
            }
        }
        
        if (!pointFound)
        {
            // Fallback si tras 15 intentos no encuentra nada tan lejos (mapa pequeño o aislado)
            Vector2 fallbackDir = Random.insideUnitCircle.normalized * 20f;
            Vector3 fallbackPos = transform.position + new Vector3(fallbackDir.x, 0, fallbackDir.y);
            if (NavMesh.SamplePosition(fallbackPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    private void HandleDeath()
    {
        currentState = TankState.Dead;

        if (tankAudioSource != null) tankAudioSource.Stop();
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        
        controller.SetPatrolAnimation(false);
        
        if (sensor != null) sensor.enabled = false;

        // Daño en área (Explosión del tanque)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        System.Collections.Generic.HashSet<HealthSystem> damagedTargets = new System.Collections.Generic.HashSet<HealthSystem>();

        foreach (Collider hit in hitColliders)
        {
            FactionIdentity hitFaction = hit.GetComponentInParent<FactionIdentity>();
            
            // Fuego amigo: el tanque soviético no explota a otros soviéticos (opcional)
            if (hitFaction != null && myFaction != null && !myFaction.IsEnemy(hitFaction.myFaction)) continue;

            HealthSystem targetHealth = hit.GetComponentInParent<HealthSystem>();
            if (targetHealth != null && !targetHealth.IsDead && !damagedTargets.Contains(targetHealth))
            {
                targetHealth.TakeDamage(explosionDamage);
                damagedTargets.Add(targetHealth);
            }
        }

        if (explosionImpactClip != null)
        {
            AudioSource.PlayClipAtPoint(explosionImpactClip, transform.position);
        }

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        if (CameraController.instance != null)
        {
            float distToCamera = Vector3.Distance(transform.position, CameraController.instance.transform.position);
            if (distToCamera <= shakeRadius)
            {
                CameraController.instance.ShakeCamera(shakeDuration, shakeIntensity);
            }
        }

        // El tanque se destruye al instante si explota
        gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (tankAudioSource != null) tankAudioSource.Stop();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.onDeath.RemoveListener(HandleDeath);
        }
    }
}
