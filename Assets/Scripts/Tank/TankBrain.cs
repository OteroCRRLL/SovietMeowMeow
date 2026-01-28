using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum TankState { Patrol, Lock, Shoot }
public class TankBrain : MonoBehaviour
{

    [Header("References")]
    public TankSensor sensor;
    public TankController controller;
    public NavMeshAgent agent;
    public Transform[] waypoints;

    [Header("Configuration")]
    public float lockTime = 2.0f;
    public float targetLossGraceTime = 1f; //Time to wait before continue patrolling


    private TankState currentState = TankState.Patrol;
    private int currentWaypointIndex = 0;
    private float lockTimer = 0f;
    private Transform currentTarget;
    private float lostTargetTimer = 0f;

    // Start is called before the first frame update
    void Start()
    {
        controller.SetPatrolAnimation(true);

        if (waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position); //Start patrolling routes
        }
    }

    // Update is called once per frame
    void Update()
    {
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

        Transform detected = sensor.GetDetectedEnemy();

        //Still in sight verification
        if (detected != null)
        {
            lostTargetTimer = 0f;
            currentTarget = detected;
            lockTimer += Time.deltaTime;

            if (lockTimer >= lockTime)
            {
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
        controller.Fire(currentTarget);

        lockTimer = 0f;
        currentState = TankState.Lock;
        
    }
    private void ResumePatrol()
    {
        agent.isStopped = false;
        agent.updateRotation = true;

        agent.SetDestination(waypoints[currentWaypointIndex].position);

        controller.SetPatrolAnimation(true);
        currentState = TankState.Patrol;
    }
}
