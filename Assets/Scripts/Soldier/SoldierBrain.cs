using UnityEngine;
using UnityEngine.AI;

public enum SoldierState { Patrol, FollowLeader, Combat, HuntPlayer, Reloading }

public class SoldierBrain : MonoBehaviour
{
    [Header("References")]
    public SoldierSensor sensor;
    public SoldierController controller;
    public NavMeshAgent agent;
    public SquadManager squadManager;
    
    [Header("Patrol/Follow Settings")]
    public bool isLeader = false;
    public Transform[] waypoints;
    public Vector3 formationOffset = new Vector3(2f, 0f, -2f); // Offset para seguidores

    [Header("Combat Settings")]
    public float stopDistance = 12f; // Se detiene para disparar
    public float fireRate = 0.3f;
    public int maxAmmo = 30;
    public float reloadTime = 2.0f;
    
    [Header("Hunt Player Settings")]
    public float catchDistance = 2.0f; // Distancia para robar la cámara

    private SoldierState currentState = SoldierState.Patrol;
    public SoldierState CurrentState => currentState;

    private int currentWaypointIndex = 0;
    private Transform currentTarget;
    
    private int currentAmmo;
    private float nextFireTime = 0f;
    private float reloadTimer = 0f;
    
    private void Start()
    {
        currentAmmo = maxAmmo;
        if (isLeader && waypoints != null && waypoints.Length > 0)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
        }
        else
        {
            currentState = SoldierState.FollowLeader;
        }
    }

    private void Update()
    {
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
        Transform visibleTarget = sensor.GetBestTarget(out isPlayer);
        
        // Si ya teníamos un objetivo y lo seguimos viendo, mantenemos el Lock
        if (currentTarget != null && sensor.IsTargetVisible(currentTarget))
        {
            // Mantenemos currentTarget
            // Excepción: Si vemos a una Facción rival y estábamos persiguiendo al Player, cambiamos a la Facción (Guerra > Civil)
            if (currentState == SoldierState.HuntPlayer && visibleTarget != null && !isPlayer)
            {
                currentTarget = visibleTarget;
            }
        }
        else if (visibleTarget != null)
        {
            currentTarget = visibleTarget;
        }
        else
        {
            // Perdimos al objetivo
            currentTarget = null;
            if (currentState == SoldierState.Combat || currentState == SoldierState.HuntPlayer)
            {
                if (currentState == SoldierState.HuntPlayer && squadManager != null)
                {
                    squadManager.ClearHunter(this); // Avisamos que dejamos de perseguir
                }
                currentState = isLeader ? SoldierState.Patrol : SoldierState.FollowLeader;
                if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
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
        controller.SetAnimation("Walk");
        if (waypoints == null || waypoints.Length == 0) return;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
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
                // El seguidor intenta ir a un punto detrás y a un lado del líder
                Vector3 targetPos = squadManager.leader.position + squadManager.leader.TransformDirection(formationOffset);
                agent.SetDestination(targetPos);
            }
        }
    }

    private void UpdateCombat()
    {
        if (currentTarget == null) return;

        controller.RotateTowards(currentTarget);

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distance <= stopDistance)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            controller.SetAnimation("Shoot");
            
            if (Time.time >= nextFireTime)
            {
                controller.Fire(currentTarget);
                currentAmmo--;
                nextFireTime = Time.time + fireRate;
            }
        }
        else
        {
            // Acercarse
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                controller.SetAnimation("Run");
                agent.SetDestination(currentTarget.position);
            }
        }
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
}