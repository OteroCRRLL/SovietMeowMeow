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
    public float lockTime = 1.0f;
    public float targetLossGraceTime = 0.5f; //Time to wait before continue patrolling
    public float shootDuration = 0.5f; // Tiempo que el tanque se queda en estado 'Shoot' tras disparar


    private TankState currentState = TankState.Patrol;
    public TankState CurrentState => currentState;
    private int currentWaypointIndex = 0;
    private float lockTimer = 0f;
    private Transform currentTarget;
    private float lostTargetTimer = 0f;
    private float shootTimer = 0f;
    private HealthSystem health;

    // Start is called before the first frame update
    void Start()
    {
        // Evitar que el tanque salga volando por los aires al recibir disparos físicos
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            rb.mass = 50000f; // Peso hiperrealista
            rb.isKinematic = true; // Como se mueve por NavMesh, bloqueamos las fuerzas físicas externas
        }
        
        Rigidbody parentRb = GetComponentInParent<Rigidbody>();
        if (parentRb != null)
        {
            parentRb.mass = 50000f;
            parentRb.isKinematic = true;
        }

        controller.SetPatrolAnimation(true);

        if (waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position); //Start patrolling routes
        }

        health = GetComponent<HealthSystem>();
        if (health != null)
        {
            health.onDeath.AddListener(HandleDeath);
        }
    }

    // Update is called once per frame
    void Update()
    {
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


    private void UpdatePatrol()
    {
        
        //1. Follow NavMesh route
        if (!agent.pathPending && agent.remainingDistance < 0.5f) //0.5 offset to avoid extreme precision
        { 
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length; //Loop
            agent.SetDestination(waypoints[currentWaypointIndex].position);
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

        // En lugar de usar el cono de visión, verificamos si el objetivo sigue visible directamente
        bool stillVisible = sensor.IsTargetVisible(currentTarget);

        //Still in sight verification
        if (stillVisible)
        {
            lostTargetTimer = 0f;
            lockTimer += Time.deltaTime;

            if (lockTimer >= lockTime)
            {
                shootTimer = 0f; // Reiniciamos el temporizador de disparo
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
        // Disparamos solo en el primer instante del estado Shoot
        if (shootTimer == 0f)
        {
            controller.Fire(currentTarget);
        }

        // Nos quedamos en este estado durante 'shootDuration' segundos
        shootTimer += Time.deltaTime;
        
        if (shootTimer >= shootDuration)
        {
            lockTimer = 0f; // Reiniciamos el tiempo de lock para el próximo disparo
            currentState = TankState.Lock; // Volvemos a fijar al objetivo
        }
    }
    private void ResumePatrol()
    {
        agent.isStopped = false;
        agent.updateRotation = true;

        agent.SetDestination(waypoints[currentWaypointIndex].position);

        controller.SetPatrolAnimation(true);
        currentState = TankState.Patrol;
    }

    private void HandleDeath()
    {
        currentState = TankState.Dead;
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        
        controller.SetPatrolAnimation(false);
        
        if (sensor != null) sensor.enabled = false;
        
        // Destruimos el tanque después de un tiempo para que el cadáver no se quede para siempre
        // También puedes poner aquí instanciar una explosión visual o un modelo de tanque destruido
        Destroy(gameObject, 15f);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.onDeath.RemoveListener(HandleDeath);
        }
    }
}
