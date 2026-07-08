using UnityEngine;
using UnityEngine.AI;

public enum SoldierState { Patrol, FollowLeader, Combat, HuntPlayer, Reloading, Dead, Paralyzed }

public class SoldierBrain : MonoBehaviour
{
    [Header("References")]
    public SoldierSensor sensor;
    public SoldierController controller;
    public NavMeshAgent agent;
    public SquadManager squadManager;
    
    [Header("Patrol/Follow Settings")]
    public bool isLeader = false;
    public float patrolRadius = 40f; // Asignado por el spawner
    public Vector3 formationOffset = new Vector3(2f, 0f, -2f); // Offset para seguidores (asignado por SquadManager)
    public float waitTimeAtDestination = 3f; // Tiempo de espera al llegar al destino

    [Header("Combat Settings")]
    public float stopDistance = 25f; // Se detiene para disparar
    public float fireRate = 0.3f;
    public int maxAmmo = 30;
    public float reloadTime = 2.0f;
    public float targetMemoryTime = 4f; // Tiempo que recuerda a un enemigo escondido
    
    [Header("Hunt Player Settings")]
    public float catchDistance = 2.0f; // Distancia para robar la cámara
    public float maxStamina = 5.0f; // Segundos que puede correr persiguiendo
    public float staminaRecoveryTime = 2.0f; // Segundos que tiene que pararse a recuperar
    private float currentStamina;
    private bool isExhausted = false;

    private SoldierState currentState = SoldierState.Patrol;
    public SoldierState CurrentState => currentState;

    private Transform currentTarget;
    public Transform CurrentTarget => currentTarget;
    public bool HasLineOfSight => targetLostTimer == 0f;
    
    private int currentAmmo;
    private HealthSystem health;
    private float nextFireTime = 0f;
    private float reloadTimer = 0f;
    private float targetLostTimer = 0f;
    private float repositionTimer = 0f;
    private float paralyzedTimer = 0f;
    private SoldierState preParalyzedState;

    // Variables de patrulla
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool hasOverrideDestination = false;
    private Vector3 overrideDestination;

    public void ForceOverrideDestination(Vector3 dest)
    {
        hasOverrideDestination = true;
        overrideDestination = dest;
        isWaiting = false; // Cancelar esperas
    }

    public void ClearOverrideDestination()
    {
        if (hasOverrideDestination)
        {
            hasOverrideDestination = false;
            SetRandomPatrolDestination();
        }
    }

    private float walkSpeed = 3.5f;
    private float runSpeed = 7f;
    
    private void Start()
    {
        currentAmmo = maxAmmo;
        currentStamina = maxStamina;

        // Auto-asignar referencias por si quedaron sin arrastrar en el Inspector del Prefab
        if (sensor == null) sensor = GetComponentInChildren<SoldierSensor>();
        if (controller == null) controller = GetComponentInChildren<SoldierController>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            walkSpeed = agent.speed;
            runSpeed = walkSpeed * 2f;
        }

        health = GetComponent<HealthSystem>();
        if (health != null)
        {
            health.onDeath.AddListener(HandleDeath);
        }

        // La inicialización del estado ahora se hace desde SquadManager
    }

    public void InitializeAsLeader()
    {
        isLeader = true;
        formationOffset = Vector3.zero;
        currentState = SoldierState.Patrol;
        SetRandomPatrolDestination();
    }

    public void InitializeAsFollower(Vector3 offset)
    {
        isLeader = false;
        formationOffset = offset;
        currentState = SoldierState.FollowLeader;
    }

    public void ReceiveAlert(Transform target)
    {
        if (currentState == SoldierState.Dead) return;

        // Si no hay objetivo, se adopta el que pasa el escuadrón
        if (currentTarget == null && target != null)
        {
            currentTarget = target;

            // Gira inmediatamente hacia él
            controller.RotateTowards(currentTarget);
        }
    }

    private void Update()
    {
        if (currentState == SoldierState.Dead) return;

        // === BEHAVIOR TREE (Árbol de Decisión) ===
        
        // 1. Supervivencia: ¿Necesito recargar o estoy paralizado?
        if (currentState == SoldierState.Paralyzed)
        {
            UpdateParalyzed();
            return; // Bloquea todo lo demás hasta terminar
        }
        
        if (currentState == SoldierState.Reloading)
        {
            UpdateReloading();
            return; // Bloquea todo lo demás hasta terminar
        }

        if (currentAmmo <= 0 && (currentState == SoldierState.Combat || currentState == SoldierState.HuntPlayer))
        {
            StartReloading();
            return;
        }

        // 2. Visión y Aggro
        Transform visibleEnemy = null;
        Transform visiblePlayer = null;
        
        if (sensor != null)
        {
            sensor.GetTargets(out visibleEnemy, out visiblePlayer);
        }

        // Barrido de Visión si no está en combate
        if (currentState == SoldierState.Patrol || currentState == SoldierState.FollowLeader)
        {
            if (sensor != null) sensor.SweepVision();
        }
        else
        {
            if (sensor != null) sensor.ResetVision();
        }

        // Avisar al squad manager si el jugador es visible
        if (visiblePlayer != null && squadManager != null)
        {
            squadManager.HandlePlayerSpotted(visiblePlayer);
        }

        bool fightingEnemies = squadManager != null && squadManager.IsFightingEnemies();
        bool amIHunter = squadManager == null || !fightingEnemies || squadManager.IsHunter(this);
        
        if (amIHunter)
        {
            // El Hunter (o todo el escuadrón si no hay otros enemigos) prioriza al jugador
            if (visiblePlayer != null)
            {
                currentTarget = visiblePlayer;
                targetLostTimer = 0f;
            }
            else if (visibleEnemy != null)
            {
                // Si no ve al jugador pero ve a un enemigo, ataca
                currentTarget = visibleEnemy;
                targetLostTimer = 0f;
                if (squadManager != null) squadManager.AlertSquad(currentTarget);
            }
        }
        else
        {
            // El resto del escuadrón (los que no cazan) prioriza enemigos para defender
            if (visibleEnemy != null)
            {
                if (currentTarget != visibleEnemy)
                {
                    currentTarget = visibleEnemy;
                    if (squadManager != null) squadManager.AlertSquad(currentTarget);
                }
                targetLostTimer = 0f;
            }
        }
        
        // Si ya había un objetivo y se le sigue viendo, se mantiene el Lock
        if (currentTarget != null)
        {
            if (sensor != null && sensor.IsTargetVisible(currentTarget))
            {
                targetLostTimer = 0f;
            }
            else
            {
                targetLostTimer += Time.deltaTime;
                if (targetLostTimer >= targetMemoryTime)
                {
                    bool squadStillFighting = false;
                    if (squadManager != null)
                    {
                        squadStillFighting = squadManager.IsTargetEngaged(currentTarget, this);
                    }
                    
                    if (squadStillFighting)
                    {
                        // Si un compañero sigue viendo al objetivo y peleando, se mantiene la alerta
                        // Reset parcial del tiempo para seguir intentando flanquear/acercarse
                        targetLostTimer = targetMemoryTime - 0.5f;
                    }
                    else
                    {
                        // Objetivo perdido definitivamente tras el tiempo de memoria
                        currentTarget = null;
                        if (currentState == SoldierState.HuntPlayer && squadManager != null)
                        {
                            squadManager.ClearHunter(this);
                        }
                    }
                }
            }
        }
        else
        {
            // Objetivo perdido
            if (currentState == SoldierState.Combat || currentState == SoldierState.HuntPlayer)
            {
                if (currentState == SoldierState.HuntPlayer && squadManager != null)
                {
                    squadManager.ClearHunter(this); // Avisa que se dejó de perseguir
                }
                
                // Retomar estado base
                currentState = isLeader ? SoldierState.Patrol : SoldierState.FollowLeader;
                isExhausted = false;
                currentStamina = maxStamina;
                if (agent != null && agent.isOnNavMesh) 
                {
                    agent.isStopped = false;
                    if (isLeader) SetRandomPatrolDestination(); // Forzar nuevo punto si el líder perdió aggro
                }
            }
        }

        // 3. Evaluar Estado según Objetivo
        if (currentTarget != null)
        {
            FactionIdentity targetFaction = currentTarget.GetComponentInParent<FactionIdentity>();
            if (targetFaction != null && targetFaction.myFaction == FactionType.Player)
            {
                if (amIHunter || squadManager == null)
                {
                    currentState = SoldierState.HuntPlayer;
                }
                else
                {
                    // Si es un seguidor, el hunter ya se está ocupando de él, así que patrulla
                    currentTarget = null;
                    currentState = isLeader ? SoldierState.Patrol : SoldierState.FollowLeader;
                }
            }
            else
            {
                // Es un enemigo (Tanque, Dron u otro soldado)
                if (currentState == SoldierState.HuntPlayer && squadManager != null)
                {
                    squadManager.ClearHunter(this); 
                }
                currentState = SoldierState.Combat;
            }
        }

        // 4. Ejecutar la acción del estado
        switch (currentState)
        {
            case SoldierState.Patrol:
                UpdatePatrol();
                break;
            case SoldierState.FollowLeader:
                UpdateFollow();
                break;
            case SoldierState.Combat:
                UpdateCombat();
                break;
            case SoldierState.HuntPlayer:
                UpdateHuntPlayer();
                break;
            case SoldierState.Paralyzed:
                UpdateParalyzed();
                break;
        }
    }

    public void Paralyze(float duration)
    {
        if (currentState == SoldierState.Dead) return;

        if (currentState != SoldierState.Paralyzed)
        {
            preParalyzedState = currentState;
            currentState = SoldierState.Paralyzed;
            
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            
            controller.SetAnimation("Idle"); // Quedarse quieto
        }
        
        paralyzedTimer = duration;
    }

    private void UpdateParalyzed()
    {
        paralyzedTimer -= Time.deltaTime;
        if (paralyzedTimer <= 0f)
        {
            currentState = preParalyzedState;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }
    }

    private void StartReloading()
    {
        currentState = SoldierState.Reloading;
        reloadTimer = 0f;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        controller.SetAnimation("Reload");
    }

    private void UpdateReloading()
    {
        reloadTimer += Time.deltaTime;
        if (reloadTimer >= reloadTime)
        {
            currentAmmo = maxAmmo;
            currentState = isLeader ? SoldierState.Patrol : SoldierState.FollowLeader;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        }
    }

    private void UpdatePatrol()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            if (isWaiting && !hasOverrideDestination)
            {
                controller.SetAnimation("Idle"); // Opcional: una animación de estar quieto vigilando
                agent.isStopped = true;
                
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitTimeAtDestination)
                {
                    isWaiting = false;
                    SetRandomPatrolDestination();
                }
            }
            else
            {
                controller.SetAnimation("Walk");
                if (agent != null) agent.speed = walkSpeed;
                agent.isStopped = false;
                
                if (hasOverrideDestination)
                {
                    agent.SetDestination(overrideDestination);
                    return;
                }
                
                // Esperar a que tenga una ruta válida antes de comprobar la distancia
                if (!agent.pathPending)
                {
                    // Evita el bug de Unity donde remainingDistance es 0 temporalmente
                    Vector3 currentPos = transform.position;
                    currentPos.y = 0;
                    Vector3 destPos = agent.destination;
                    destPos.y = 0;

                    if (agent.remainingDistance <= 0.5f && Vector3.Distance(currentPos, destPos) <= 1.5f)
                    {
                        // Destino alcanzado, empieza la espera
                        isWaiting = true;
                        waitTimer = 0f;
                    }
                    else if (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.pathStatus == NavMeshPathStatus.PathPartial)
                    {
                        // Si la ruta es inválida (se atascó), fuerza un nuevo destino INMEDIATAMENTE
                        isWaiting = true;
                        waitTimer = waitTimeAtDestination; // Fuerza el reloj para que busque otro destino en el siguiente frame
                    }
                }
            }
        }
    }

    private void SetRandomPatrolDestination()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        if (hasOverrideDestination) return;

        // Elegir un punto aleatorio entre 10 y el radio máximo para evitar que elija un punto en sus propios pies
        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(10f, patrolRadius);
        Vector3 randomDirection = new Vector3(randomCircle.x, 0, randomCircle.y) + transform.position;
        
        // "Gravedad" hacia el centro del mapa para un movimiento más orgánico hacia el interior
        if (DayDirector.instance != null && DayDirector.instance.mapCenterPoint != null)
        {
            Vector3 centerDir = (DayDirector.instance.mapCenterPoint.position - transform.position).normalized;
            centerDir.y = 0;
            // Empuje constante hacia el centro equivalente a un 40% del radio
            randomDirection += centerDir * (patrolRadius * 0.4f);
        }
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Si por algún motivo falla, se reintenta en el próximo frame
            isWaiting = true;
            waitTimer = waitTimeAtDestination; 
        }
    }

    private void UpdateFollow()
    {
        if (agent != null) agent.speed = walkSpeed;
        if (squadManager != null && squadManager.leader != null)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;

                // Calcular la dirección hacia adelante y a la derecha del líder de forma plana
                Vector3 leaderForward = squadManager.leader.forward;
                leaderForward.y = 0;
                if (leaderForward.sqrMagnitude < 0.01f) leaderForward = Vector3.forward;
                leaderForward.Normalize();

                Vector3 leaderRight = Vector3.Cross(Vector3.up, leaderForward);

                // Aplicar el offset matemáticamente en base a hacia dónde mira el líder
                Vector3 targetPos = squadManager.leader.position
                                    + (leaderRight * formationOffset.x)
                                    + (leaderForward * formationOffset.z);

                // IMPORTANTE: el destino solo se actualiza si se ha alejado lo suficiente.
                // Llamar a SetDestination() cada frame haría que el NavMeshAgent se buguee calculando rutas y se quede quieto.
                Vector3 currentDest = agent.destination;
                currentDest.y = 0;
                Vector3 targetDest = targetPos;
                targetDest.y = 0;

                if (Vector3.Distance(currentDest, targetDest) > 1f)
                {
                    agent.SetDestination(targetPos);
                }
            }
        }

        // Solo se anima como "Walk" mientras realmente se desplaza; si ya alcanzó su
        // posición en la formación y está esperando al líder, se queda en Idle.
        bool isMoving = agent != null && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.05f;
        controller.SetAnimation(isMoving ? "Walk" : "Idle");
    }

    private void UpdateCombat()
    {
        if (currentTarget == null) return;

        controller.RotateTowards(currentTarget);

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        
        // Solo dispara si hay línea de visión (no está escondido tras el tiempo de memoria)
        bool hasLineOfSight = (targetLostTimer == 0f);

        // A distancia de tiro, el comportamiento es táctico (no corre a lo loco hacia él)
        if (distance <= stopDistance)
        {
            // Comprueba si está en movimiento
            bool isRepositioning = false;
            if (agent != null && agent.isOnNavMesh)
            {
                if (agent.pathPending)
                {
                    isRepositioning = true;
                }
                else
                {
                    Vector3 currentPos = transform.position;
                    currentPos.y = 0;
                    Vector3 destPos = agent.destination;
                    destPos.y = 0;
                    
                    if (agent.remainingDistance > 0.5f || Vector3.Distance(currentPos, destPos) > 1.5f)
                    {
                        isRepositioning = true;
                    }
                }
            }

            // Sistema de movimiento en combate (para buscar ángulo si no hay visión, o no ser blanco fácil)
            repositionTimer -= Time.deltaTime;

            bool tooClose = distance < 15f;
            bool needsToMove = (!hasLineOfSight || tooClose) && !isRepositioning;

            // Forzar reposicionamiento inmediato si no hay visión, está muy cerca o toca moverse por tiempo
            if (repositionTimer <= 0f || needsToMove)
            {
                Vector3 repoPos = transform.position;
                if (tooClose)
                {
                    // El enemigo está demasiado cerca, intentar retroceder
                    Vector3 dirAway = (transform.position - currentTarget.position).normalized;
                    repoPos += dirAway * 6f + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                    repositionTimer = Random.Range(1.5f, 3f); // Muy cerca: la posición se evalúa más a menudo
                }
                else
                {
                    // Movimiento lateral/aleatorio normal (mucho menos frecuente para evitar que den tantas vueltas)
                    Vector2 rand = Random.insideUnitCircle * 5f;
                    repoPos += new Vector3(rand.x, 0, rand.y);
                    repositionTimer = Random.Range(8f, 14f); // Se quedan quietos disparando durante mucho más tiempo
                }
                
                if (NavMesh.SamplePosition(repoPos, out NavMeshHit hit, 6f, NavMesh.AllAreas))
                {
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.SetDestination(hit.position);
                        agent.isStopped = false;
                        isRepositioning = true; // Orden de moverse recién emitida
                    }
                }
            }

            if (!isRepositioning)
            {
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

                if (hasLineOfSight)
                {
                    controller.SetAnimation("Shoot");

                    // Solo dispara si está quieto
                    if (Time.time >= nextFireTime)
                    {
                        if (HasClearShot(currentTarget))
                        {
                            controller.Fire(currentTarget);
                            currentAmmo--;
                            nextFireTime = Time.time + fireRate;
                        }
                        else
                        {
                            // Un aliado bloquea la línea de tiro o hay una pared, forzar reposicionamiento inmediato
                            repositionTimer = 0f;
                            nextFireTime = Time.time + (fireRate / 2f); // Retraso pequeño antes de reintentar
                        }
                    }
                }
                else
                {
                    // A buena distancia pero sin visión (escondido tras un muro o aliado).
                    // Se queda quieto esperando a que el timer fuerce el movimiento.
                    controller.SetAnimation("Idle");
                }
            }
            else
            {
                if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
                controller.SetAnimation("Walk"); // Caminar tácticamente, no correr
                if (agent != null) agent.speed = walkSpeed;
            }
        }
        else
        {
            // Muy lejos, Acercarse corriendo a su posición
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                controller.SetAnimation("Run");
                if (agent != null) agent.speed = runSpeed;
                
                Vector3 targetPos = currentTarget.position;

                // Despliegue táctico al avanzar si es un seguidor y el líder vive
                if (!isLeader && squadManager != null && squadManager.leader != null)
                {
                    // Vector desde el líder hacia el enemigo
                    Vector3 leaderToEnemy = (currentTarget.position - squadManager.leader.position).normalized;
                    leaderToEnemy.y = 0;
                    if (leaderToEnemy.sqrMagnitude < 0.01f) leaderToEnemy = Vector3.forward;

                    Vector3 rightEnemy = Vector3.Cross(Vector3.up, leaderToEnemy);

                    // Posición avanzando hacia el enemigo pero respetando el offset de formación desplegada
                    targetPos = currentTarget.position
                                - (leaderToEnemy * stopDistance * 0.8f) // Se mantiene la línea un poco por detrás del rango de disparo
                                + (rightEnemy * formationOffset.x)
                                + (leaderToEnemy * formationOffset.z);

                    // El punto debe caer en el NavMesh
                    if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                    {
                        targetPos = hit.position;
                    }
                    else
                    {
                        targetPos = currentTarget.position; // Fallback al objetivo
                    }
                }
                
                agent.SetDestination(targetPos);
            }
        }
    }

    private bool HasClearShot(Transform target)
    {
        if (controller.shootPoint == null) return true;

        // Mismo punto de puntería que usa SoldierController.Fire() (centro del collider, no los pies/pivote),
        // para que esta comprobación y el disparo real apunten exactamente al mismo sitio.
        Collider targetCollider = target.GetComponentInChildren<Collider>();
        Vector3 targetPosition = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up * 1f;

        Vector3 direction = (targetPosition - controller.shootPoint.position).normalized;
        float dist = Vector3.Distance(controller.shootPoint.position, targetPosition);

        // Recorre todos los impactos del rayo (no solo el primero), igual que SoldierSensor.IsLineClear,
        // para que el primer impacto real (ignorando al propio tirador) determine si hay tiro limpio.
        RaycastHit[] hits = Physics.RaycastAll(controller.shootPoint.position, direction, dist + 0.5f, sensor != null ? sensor.detectableLayers : (LayerMask)~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.root == transform.root) continue; // Ignora al propio tirador
            return hit.transform.root == target.root; // El primer impacto ajeno debe ser el objetivo
        }

        return true;
    }

    private void UpdateHuntPlayer()
    {
        if (currentTarget == null) return;

        if (isExhausted)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            controller.SetAnimation("Idle"); // Tomando aliento
            currentStamina += Time.deltaTime;
            if (currentStamina >= staminaRecoveryTime)
            {
                isExhausted = false;
                currentStamina = maxStamina;
            }
            return;
        }

        currentStamina -= Time.deltaTime;
        if (currentStamina <= 0f)
        {
            isExhausted = true;
            currentStamina = 0f;
            return;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            controller.SetAnimation("Run");
            if (agent != null) agent.speed = runSpeed;
            agent.SetDestination(currentTarget.position);
        }
        controller.RotateTowards(currentTarget);

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance <= catchDistance)
        {
            // Robar cámara = Fin del día
            if (GameManager.instance != null)
            {
                if (DeathScreenManager.instance != null)
                {
                    DeathScreenManager.instance.ShowDeathScreen("CAUGHT\n\nThey smashed your camera.");
                }
                else
                {
                    GameManager.instance.FailDay();
                }
            }
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            currentState = SoldierState.Patrol; 
        }
    }

    private void HandleDeath()
    {
        currentState = SoldierState.Dead;
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.enabled = false; // Desactivar agente para que no mueva al cadáver
        }
        
        controller.TriggerDeath();
        
        if (sensor != null) sensor.enabled = false;
        
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false; // El cadáver no bloquea balas/movimiento
        
        if (squadManager != null)
        {
            squadManager.RemoveMember(this);
        }
        
        // En lugar de destruir, se desactiva el cadáver para no perder el script y que el replay manager guarde el estado
        StartCoroutine(DeactivateAfterDelay(20f));
    }

    private System.Collections.IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.onDeath.RemoveListener(HandleDeath);
        }
    }
}