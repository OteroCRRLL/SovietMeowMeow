using UnityEngine;
using UnityEngine.AI;

public enum SoldierState { Patrol, FollowLeader, Combat, HuntPlayer, Reloading, Dead }

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

    // Variables de patrulla
    private float waitTimer = 0f;
    private bool isWaiting = false;
    
    private void Start()
    {
        currentAmmo = maxAmmo;

        // Auto-asignar referencias por si se te olvidó arrastrarlas en el Inspector del Prefab
        if (sensor == null) sensor = GetComponentInChildren<SoldierSensor>();
        if (controller == null) controller = GetComponentInChildren<SoldierController>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();

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

        // Si no tenemos objetivo, adoptamos el que nos pasa el escuadrón
        if (currentTarget == null && target != null)
        {
            currentTarget = target;
            
            // Inmediatamente miramos hacia él
            controller.RotateTowards(currentTarget);
        }
    }

    private void Update()
    {
        if (currentState == SoldierState.Dead) return;

        // === BEHAVIOR TREE (Árbol de Decisión) ===
        
        // 1. Supervivencia: ¿Necesito recargar?
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
        bool isPlayer = false;
        Transform visibleTarget = null;
        
        if (sensor != null)
        {
            visibleTarget = sensor.GetBestTarget(out isPlayer);
        }
        
        // Si ya teníamos un objetivo y lo seguimos viendo, mantenemos el Lock
        if (currentTarget != null)
        {
            if (sensor != null && sensor.IsTargetVisible(currentTarget))
            {
                targetLostTimer = 0f;
                // Excepción: Si vemos a una Facción rival y estábamos persiguiendo al Player, cambiamos a la Facción (Guerra > Civil)
                if (currentState == SoldierState.HuntPlayer && visibleTarget != null && !isPlayer)
                {
                    currentTarget = visibleTarget;
                }
            }
            else
            {
                targetLostTimer += Time.deltaTime;
                if (visibleTarget != null)
                {
                    // Vemos a otro mientras el nuestro está escondido, cambiamos de objetivo!
                    currentTarget = visibleTarget;
                    targetLostTimer = 0f;
                    if (squadManager != null) squadManager.AlertSquad(currentTarget);
                }
                else if (targetLostTimer >= targetMemoryTime)
                {
                    bool squadStillFighting = false;
                    if (squadManager != null)
                    {
                        squadStillFighting = squadManager.IsTargetEngaged(currentTarget, this);
                    }
                    
                    if (squadStillFighting)
                    {
                        // Si un compañero sigue viendo al objetivo y peleando, nos mantenemos alerta
                        // Reseteamos un poco el tiempo para seguir intentando flanquear/acercarnos
                        targetLostTimer = targetMemoryTime - 0.5f;
                    }
                    else
                    {
                        // Perdimos al objetivo definitivamente tras el tiempo de memoria
                        currentTarget = null;
                    }
                }
            }
        }
        else if (visibleTarget != null)
        {
            currentTarget = visibleTarget;
            targetLostTimer = 0f;
            
            // Avisar al resto del escuadrón
            if (squadManager != null)
            {
                squadManager.AlertSquad(currentTarget);
            }
        }
        
        if (currentTarget == null)
        {
            if (currentState == SoldierState.Combat || currentState == SoldierState.HuntPlayer)
            {
                if (currentState == SoldierState.HuntPlayer && squadManager != null)
                {
                    squadManager.ClearHunter(this); // Avisamos que dejamos de perseguir
                }
                
                // Retomar estado base
                currentState = isLeader ? SoldierState.Patrol : SoldierState.FollowLeader;
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
                // Es el Jugador: Preguntar al SquadManager si podemos cazarlo
                if (squadManager == null || squadManager.RequestHuntPlayer(this))
                {
                    currentState = SoldierState.HuntPlayer;
                }
                else
                {
                    // Otro soldado ya lo persigue, le ignoramos
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
            if (isWaiting)
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
                agent.isStopped = false;
                
                // Esperar a que tenga una ruta válida antes de comprobar la distancia
                if (!agent.pathPending)
                {
                    // Evitamos el bug de Unity donde remainingDistance es 0 temporalmente
                    Vector3 currentPos = transform.position;
                    currentPos.y = 0;
                    Vector3 destPos = agent.destination;
                    destPos.y = 0;

                    if (agent.remainingDistance <= 0.5f && Vector3.Distance(currentPos, destPos) <= 1.5f)
                    {
                        // Llegamos al destino, empezamos a esperar
                        isWaiting = true;
                        waitTimer = 0f;
                    }
                    else if (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.pathStatus == NavMeshPathStatus.PathPartial)
                    {
                        // Si la ruta es inválida (se atascó), forzamos un nuevo destino INMEDIATAMENTE
                        isWaiting = true;
                        waitTimer = waitTimeAtDestination; // Forzamos el reloj para que busque otro destino en el siguiente frame
                    }
                }
            }
        }
    }

    private void SetRandomPatrolDestination()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Elegir un punto aleatorio entre 10 y el radio máximo para evitar que elija un punto en sus propios pies
        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(10f, patrolRadius);
        Vector3 randomDirection = new Vector3(randomCircle.x, 0, randomCircle.y) + transform.position;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Si por algún motivo falla, volvemos a intentarlo en el próximo frame
            isWaiting = true;
            waitTimer = waitTimeAtDestination; 
        }
    }

    private void UpdateFollow()
    {
        controller.SetAnimation("Walk");
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

                // Aplicar el offset matemáticamente basándonos en hacia dónde mira el líder
                Vector3 targetPos = squadManager.leader.position 
                                    + (leaderRight * formationOffset.x) 
                                    + (leaderForward * formationOffset.z);
                
                // IMPORTANTÍSIMO: Solo actualizamos el destino si se ha alejado lo suficiente. 
                // Si llamamos a SetDestination() cada frame, el NavMeshAgent se buguea calculando rutas y se queda quieto.
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
    }

    private void UpdateCombat()
    {
        if (currentTarget == null) return;

        controller.RotateTowards(currentTarget);

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        
        // Solo disparamos si tenemos línea de visión (no está escondido tras el tiempo de memoria)
        bool hasLineOfSight = (targetLostTimer == 0f);

        // Si estamos a distancia de tiro, nos comportamos tácticamente (no corremos a lo loco hacia él)
        if (distance <= stopDistance)
        {
            // Verificamos si estamos en movimiento
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

            // Sistema de movimiento en combate (para buscar ángulo si no le vemos o no ser blanco fácil)
            repositionTimer -= Time.deltaTime;
            
            bool tooClose = distance < 15f;
            bool needsToMove = (!hasLineOfSight || tooClose) && !isRepositioning;

            // Forzar reposicionamiento inmediato si no tenemos visión, estamos muy cerca o toca moverse por tiempo
            if (repositionTimer <= 0f || needsToMove)
            {
                Vector3 repoPos = transform.position;
                if (tooClose)
                {
                    // El enemigo está demasiado cerca, intentar retroceder
                    Vector3 dirAway = (transform.position - currentTarget.position).normalized;
                    repoPos += dirAway * 6f + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                    repositionTimer = Random.Range(1.5f, 3f); // Si estamos muy cerca, evaluamos la posición más a menudo
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
                        isRepositioning = true; // Acabamos de darle una orden de moverse
                    }
                }
            }

            if (!isRepositioning)
            {
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                
                if (hasLineOfSight)
                {
                    controller.SetAnimation("Shoot");
                    
                    // Solo disparamos si estamos quietos
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
                            // Un aliado nos bloquea o hay una pared, forzar reposicionamiento inmediato
                            repositionTimer = 0f; 
                            nextFireTime = Time.time + (fireRate / 2f); // Retraso pequeño antes de volver a intentarlo
                        }
                    }
                }
                else
                {
                    // Estamos a buena distancia pero no le vemos (escondido tras un muro o aliado). 
                    // Apuntamos y esperamos a que el timer nos haga movernos.
                    controller.SetAnimation("Walk"); // O Idle de combate
                }
            }
            else
            {
                if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
                controller.SetAnimation("Walk"); // Caminar tácticamente, no correr
            }
        }
        else
        {
            // Muy lejos, Acercarse corriendo a su posición
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                controller.SetAnimation("Run");
                
                Vector3 targetPos = currentTarget.position;
                
                // Despliegue táctico al avanzar si somos seguidores y el líder vive
                if (!isLeader && squadManager != null && squadManager.leader != null)
                {
                    // Vector desde el líder hacia el enemigo
                    Vector3 leaderToEnemy = (currentTarget.position - squadManager.leader.position).normalized;
                    leaderToEnemy.y = 0;
                    if (leaderToEnemy.sqrMagnitude < 0.01f) leaderToEnemy = Vector3.forward;
                    
                    Vector3 rightEnemy = Vector3.Cross(Vector3.up, leaderToEnemy);
                    
                    // Calculamos una posición avanzando hacia el enemigo pero respetando nuestro offset de formación desplegada
                    targetPos = currentTarget.position 
                                - (leaderToEnemy * stopDistance * 0.8f) // Nos acercamos manteniendo la línea un poco por detrás del rango de disparo
                                + (rightEnemy * formationOffset.x) 
                                + (leaderToEnemy * formationOffset.z);
                                
                    // Asegurarnos de que el punto cae en el NavMesh
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
        
        Vector3 direction = (target.position - controller.shootPoint.position).normalized;
        float dist = Vector3.Distance(controller.shootPoint.position, target.position);
        
        // Raycast para ver si hay un aliado en medio o una pared (usando LayerMask default ~0)
        if (Physics.Raycast(controller.shootPoint.position, direction, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            FactionIdentity hitFaction = hit.collider.GetComponentInParent<FactionIdentity>();
            FactionIdentity myFaction = GetComponent<FactionIdentity>();
            
            // Si golpeamos algo que tiene facción y NO es nuestro enemigo (es un aliado)
            if (hitFaction != null && myFaction != null && !myFaction.IsEnemy(hitFaction.myFaction))
            {
                // Verificamos que no seamos nosotros mismos por si el shootPoint detecta nuestro propio cuerpo
                if (hit.transform.root != transform.root)
                {
                    return false; // Aliado en la línea de fuego!
                }
            }
            
            // Podrías añadir que retorne false si golpea una pared en vez del objetivo, 
            // pero el sensor ya suele gestionar la visibilidad. Por si acaso, lo dejamos simple.
        }
        return true;
    }

    private void UpdateHuntPlayer()
    {
        if (currentTarget == null) return;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            controller.SetAnimation("Run");
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
        
        controller.SetAnimation("Death");
        
        if (sensor != null) sensor.enabled = false;
        
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false; // El cadáver no bloquea balas/movimiento
        
        if (squadManager != null)
        {
            squadManager.RemoveMember(this);
        }
        
        // Destruimos el cadáver pasados 10 segundos para limpiar la escena
        Destroy(gameObject, 10f);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.onDeath.RemoveListener(HandleDeath);
        }
    }
}