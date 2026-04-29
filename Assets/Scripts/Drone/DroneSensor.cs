using UnityEngine;
using System.Collections.Generic;

public class DroneSensor : MonoBehaviour
{
    [Header("Detection Settings")]
    public float DetectRange = 30f;
    public Transform visionPoint;
    
    [Tooltip("El ángulo del cono de visión (más amplio para drones)")]
    public float angleDifference = 140f;
    [Tooltip("El ángulo de visión justo debajo del dron (para que no te puedas esconder bajo él)")]
    public float downwardAngle = 70f;

    [Header("Optimization")]
    public float scanFrequency = 5f;
    private float nextScanTime = 0f;

    private Collider[] collidersBuffer = new Collider[20];
    private FactionIdentity myFaction;

    private void Start()
    {
        myFaction = GetComponentInParent<FactionIdentity>();
    }

    public Transform GetBestTarget(out bool isPlayer)
    {
        isPlayer = false;
        if (visionPoint == null || myFaction == null) return null;

        if (Time.time < nextScanTime) return null;
        nextScanTime = Time.time + (1f / scanFrequency);

        int numColliders = Physics.OverlapSphereNonAlloc(visionPoint.position, DetectRange, collidersBuffer, ~0);

        Transform bestEnemyTarget = null;
        Transform playerTarget = null;
        float closestEnemyDist = float.MaxValue;

        for (int i = 0; i < numColliders; i++)
        {
            Collider col = collidersBuffer[i];

            FactionIdentity otherFaction = col.GetComponentInParent<FactionIdentity>();
            if (otherFaction == null || !myFaction.IsEnemy(otherFaction.myFaction)) continue;

            HealthSystem targetHealth = col.GetComponentInParent<HealthSystem>();
            if (targetHealth != null && targetHealth.IsDead) continue;

            Vector3 targetPosition = col.bounds.center;
            Vector3 directionToTarget = (targetPosition - visionPoint.position).normalized;

            // Como los drones vuelan alto, no aplanamos el vector Y. Hacemos el ángulo en 3D puro.
            float angleToTarget = Vector3.Angle(visionPoint.forward, directionToTarget);
            
            // También comprobamos si el jugador está directamente debajo del dron
            float angleToBottom = Vector3.Angle(Vector3.down, directionToTarget);

            if (angleToTarget <= angleDifference / 2f || angleToBottom <= downwardAngle / 2f)
            {
                float distanceToTarget = Vector3.Distance(visionPoint.position, targetPosition);

                // Raycast para paredes
                if (Physics.Raycast(visionPoint.position, directionToTarget, out RaycastHit hit, distanceToTarget, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform.root == col.transform.root)
                    {
                        if (otherFaction.myFaction == FactionType.Player)
                        {
                            playerTarget = col.transform;
                        }
                        else
                        {
                            if (distanceToTarget < closestEnemyDist)
                            {
                                closestEnemyDist = distanceToTarget;
                                bestEnemyTarget = col.transform;
                            }
                        }
                    }
                }
            }
        }

        if (bestEnemyTarget != null)
        {
            isPlayer = false;
            return bestEnemyTarget;
        }

        if (playerTarget != null)
        {
            isPlayer = true;
            return playerTarget;
        }

        return null;
    }

    public bool IsTargetVisible(Transform target)
    {
        if (target == null || visionPoint == null) return false;

        HealthSystem targetHealth = target.GetComponentInParent<HealthSystem>();
        if (targetHealth != null && targetHealth.IsDead) return false;

        // Encontrar el centro exacto del collider principal para que el raycast no falle
        Vector3 targetPosition = target.position + Vector3.up * 1f;
        Collider[] cols = target.GetComponentsInChildren<Collider>();
        foreach (Collider c in cols)
        {
            if (!c.isTrigger)
            {
                targetPosition = c.bounds.center;
                break;
            }
        }

        float distanceToTarget = Vector3.Distance(visionPoint.position, targetPosition);
        if (distanceToTarget > DetectRange) return false;

        Vector3 directionToTarget = (targetPosition - visionPoint.position).normalized;

        if (Physics.Raycast(visionPoint.position, directionToTarget, out RaycastHit hit, DetectRange, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(target) || hit.transform.root == target.root)
            {
                return true;
            }
        }
        return false;
    }
}
