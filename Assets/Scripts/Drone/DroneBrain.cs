using UnityEngine;

public enum DroneState { Patrol, Locking, LostTarget, Kamikaze, Dead }

public class DroneBrain : MonoBehaviour
{
    [Header("References")]
    public DroneSensor sensor;
    public DroneController controller;
    private HealthSystem health;
    private FactionIdentity myFaction;

    [Header("Flight Settings")]
    public float patrolRadius = 30f;
    public float patrolSpeed = 6f;
    public float diveSpeed = 35f;

    [Header("Combat Settings")]
    public float lockTime = 2.5f; // Tiempo que tiene el jugador para esconderse
    public float memoryTime = 3.0f; // Tiempo que el dron se queda mirando si te escondes
    public float explosionDamage = 80f;
    public float explosionRadius = 6f;

    private DroneState currentState = DroneState.Patrol;
    public DroneState CurrentState => currentState;

    private Transform currentTarget;
    private Vector3 lastKnownTargetPos;
    private float stateTimer = 0f;

    private Vector3 currentPatrolPoint;

    private void Start()
    {
        if (sensor == null) sensor = GetComponentInChildren<DroneSensor>();
        if (controller == null) controller = GetComponentInChildren<DroneController>();
        
        myFaction = GetComponent<FactionIdentity>();
        health = GetComponent<HealthSystem>();

        if (health != null)
        {
            health.onDeath.AddListener(HandleDeath);
        }

        // Evitar que el dron caiga por gravedad mientras patrulla
        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        SetRandomPatrolDestination();
    }

    private void Update()
    {
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
            
            // Avisar a los soldados soviéticos de la zona si el dron es atacado
            AlertNearbyAllies(currentTarget);
        }
    }

    private void UpdatePatrol(Transform visibleTarget)
    {
        if (visibleTarget != null)
        {
            // ¡Enemigo a la vista! Empezamos a fijar el objetivo
            currentTarget = visibleTarget;
            lastKnownTargetPos = visibleTarget.position;
            currentState = DroneState.Locking;
            stateTimer = 0f;
            
            // Avisar a los soldados soviéticos de la zona si el dron los ve (chivato)
            AlertNearbyAllies(currentTarget);
            return;
        }

        // Patrullar volando de forma autónoma hacia el punto actual
        Vector3 dir = (currentPatrolPoint - transform.position).normalized;
        transform.position += dir * patrolSpeed * Time.deltaTime;
        controller.RotateTowardsPoint(currentPatrolPoint);

        // Si llegamos al destino, buscar otro
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
            // Le seguimos viendo, el lock avanza
            lastKnownTargetPos = currentTarget.position;
            stateTimer += Time.deltaTime;

            if (stateTimer >= lockTime)
            {
                // ¡LOCK COMPLETADO! Empieza el picado mortal
                currentState = DroneState.Kamikaze;
                
                // Quitamos el modo cinemático para que colisione con físicas fuertes al caer
                if (TryGetComponent<Rigidbody>(out Rigidbody rb)) 
                {
                    rb.isKinematic = false;
                }
            }
        }
        else
        {
            // Lo perdimos de vista durante el lock
            currentState = DroneState.LostTarget;
            stateTimer = 0f;
        }
    }

    private void UpdateLostTarget(Transform visibleTarget)
    {
        // Se queda flotando quieto, mirando a la última esquina donde vio al enemigo
        controller.RotateTowardsPoint(lastKnownTargetPos);

        if (visibleTarget != null)
        {
            // ¡Ha vuelto a asomarse! Retomamos el lock
            currentTarget = visibleTarget;
            currentState = DroneState.Locking;
            return;
        }

        stateTimer += Time.deltaTime;
        if (stateTimer >= memoryTime)
        {
            // Se cansó de esperar. Vuelve a patrullar.
            currentTarget = null;
            currentState = DroneState.Patrol;
            SetRandomPatrolDestination();
        }
    }

    private void UpdateKamikaze()
    {
        // Vuelo libre y extremadamente agresivo hacia la última posición conocida
        Vector3 direction = (lastKnownTargetPos - transform.position).normalized;
        
        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.velocity = direction * diveSpeed;
        }
        else
        {
            transform.position += direction * diveSpeed * Time.deltaTime;
        }
        
        controller.RotateTowardsPoint(lastKnownTargetPos);

        // Si estamos súper cerca de la meta y no hemos chocado con nada físico, explotamos de todos modos
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
            // Buscamos un punto aleatorio en 3D
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
            
            // Limitamos un poco la verticalidad para que no se vaya al espacio ni se entierre
            randomDir.y = Random.Range(-5f, 5f); 
            
            Vector3 potentialPoint = transform.position + randomDir;

            // Comprobamos con un rayo si podemos volar hasta ahí en línea recta sin chocar con un edificio
            Vector3 dirToPoint = (potentialPoint - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, potentialPoint);
            
            if (!Physics.Raycast(transform.position, dirToPoint, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                currentPatrolPoint = potentialPoint;
                validPointFound = true;
            }
        }

        // Si después de 10 intentos no encuentra hueco (ej: está metido en un conducto muy estrecho)
        if (!validPointFound)
        {
            // Vuelve por donde ha venido para desatascarse
            currentPatrolPoint = transform.position - transform.forward * 5f;
        }
    }

    private void AlertNearbyAllies(Transform target)
    {
        // El Dron avisa a los escuadrones de soldados cercanos
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
        // Si chocamos contra lo que sea en modo kamikaze (suelo, pared, persona), explotamos
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

        // Daño en área (Explosión)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hitColliders)
        {
            FactionIdentity hitFaction = hit.GetComponentInParent<FactionIdentity>();
            
            // Fuego amigo: los drones soviéticos no explotan a otros soviéticos
            if (hitFaction != null && myFaction != null && !myFaction.IsEnemy(hitFaction.myFaction)) continue;

            HealthSystem targetHealth = hit.GetComponentInParent<HealthSystem>();
            if (targetHealth != null && !targetHealth.IsDead)
            {
                targetHealth.TakeDamage(explosionDamage);
            }
        }

        // Destruimos el objeto del dron al explotar
        Destroy(gameObject);
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
