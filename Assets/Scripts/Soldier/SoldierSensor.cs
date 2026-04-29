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

    [Header("Sweep Vision")]
    public float sweepSpeed = 60f;
    public float maxSweepAngle = 45f;
    private Quaternion originalVisionRotation;

    private void Start()
    {
        myFaction = GetComponentInParent<FactionIdentity>();
        if (visionPoint != null)
        {
            originalVisionRotation = visionPoint.localRotation;
        }
    }

    public void SweepVision()
    {
        if (visionPoint == null) return;
        float sweepAngle = Mathf.Sin(Time.time * sweepSpeed * Mathf.Deg2Rad) * maxSweepAngle;
        visionPoint.localRotation = originalVisionRotation * Quaternion.Euler(0, sweepAngle, 0);
    }

    public void ResetVision()
    {
        if (visionPoint == null) return;
        visionPoint.localRotation = originalVisionRotation;
    }

    /// <summary>
    /// Devuelve los objetivos a la vista.
    /// </summary>
    public void GetTargets(out Transform bestEnemyTarget, out Transform playerTarget)
    {
        bestEnemyTarget = null;
        playerTarget = null;
        
        if (visionPoint == null || myFaction == null) return;

        if (Time.time < nextScanTime) return;
        nextScanTime = Time.time + (1f / scanFrequency);

        // Esfera de detección inicial
        int numColliders = Physics.OverlapSphereNonAlloc(visionPoint.position, DetectRange, collidersBuffer, detectableLayers);

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
                if (Physics.Raycast(visionPoint.position, directionToTarget, out RaycastHit hit, distanceToTarget, detectableLayers, QueryTriggerInteraction.Ignore))
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

        if (Physics.Raycast(visionPoint.position, directionToTarget, out RaycastHit hit, DetectRange, detectableLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.root == target.root)
            {
                return true;
            }
        }
        return false;
    }
}