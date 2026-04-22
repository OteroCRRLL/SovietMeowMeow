using UnityEngine;
using System.Collections.Generic;

public class SoldierSensor : MonoBehaviour
{
    [Header("Detection settings")]
    public float DetectRange = 40f;
    public Transform visionPoint;
    public LayerMask detectableLayers; // En Unity ahora ignoraremos esto para usar ~0 si queremos chocar con todo

    [Header("Raycasts Configuration (Cone)")]
    [Tooltip("Apertura total del cono en grados.")]
    public float angleDifference = 120f;

    [Header("Optimization")]
    public float scanFrequency = 5f;
    private float nextScanTime = 0f;
    
    private Collider[] collidersBuffer = new Collider[20]; 
    private FactionIdentity myFaction;

    private void Start()
    {
        myFaction = GetComponentInParent<FactionIdentity>();
    }

    /// <summary>
    /// Devuelve el mejor objetivo a la vista.
    /// Prioriza facciones enemigas para combatir. Si no hay facciones pero ve al Player, devuelve al Player.
    /// </summary>
    public Transform GetBestTarget(out bool isPlayer)
    {
        isPlayer = false;
        if (visionPoint == null || myFaction == null) return null;

        if (Time.time < nextScanTime) return null;
        nextScanTime = Time.time + (1f / scanFrequency);

        // Esfera de detección inicial
        int numColliders = Physics.OverlapSphereNonAlloc(visionPoint.position, DetectRange, collidersBuffer);

        Transform bestEnemyTarget = null;
        Transform playerTarget = null;
        float closestEnemyDist = float.MaxValue;

        for (int i = 0; i < numColliders; i++)
        {
            Collider col = collidersBuffer[i];
            
            FactionIdentity otherFaction = col.GetComponentInParent<FactionIdentity>();
            if (otherFaction == null || !myFaction.IsEnemy(otherFaction.myFaction)) continue;

            // Ignorar a los muertos
            HealthSystem targetHealth = col.GetComponentInParent<HealthSystem>();
            if (targetHealth != null && targetHealth.IsDead) continue;

            Vector3 targetPosition = col.bounds.center;
            Vector3 directionToTarget = (targetPosition - visionPoint.position).normalized;

            // Aplanamos vectores
            Vector3 flatForward = visionPoint.forward;
            flatForward.y = 0;
            flatForward.Normalize();

            Vector3 flatDirectionToTarget = directionToTarget;
            flatDirectionToTarget.y = 0;
            flatDirectionToTarget.Normalize();

            float angleToTarget = Vector3.Angle(flatForward, flatDirectionToTarget);
            
            if (angleToTarget <= angleDifference / 2f)
            {
                float distanceToTarget = Vector3.Distance(visionPoint.position, targetPosition);
                
                // Raycast para comprobar paredes
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
                            // Si es de otra facción, calculamos el más cercano
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

        // Behavior Tree: Guerra > Perseguir al civil
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

        // Comprobar que sigue vivo
        HealthSystem targetHealth = target.GetComponentInParent<HealthSystem>();
        if (targetHealth != null && targetHealth.IsDead) return false;

        Collider targetCollider = target.GetComponentInChildren<Collider>();
        Vector3 targetPosition = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up * 0.5f;

        float distanceToTarget = Vector3.Distance(visionPoint.position, targetPosition);
        if (distanceToTarget > DetectRange) return false;

        Vector3 directionToTarget = (targetPosition - visionPoint.position).normalized;

        if (Physics.Raycast(visionPoint.position, directionToTarget, out RaycastHit hit, DetectRange, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.root == target.root)
            {
                return true;
            }
        }
        return false;
    }
}