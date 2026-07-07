using UnityEngine;
using System.Collections.Generic;

public class SoldierSensor : MonoBehaviour
{
    [Header("Detection settings")]
    public float DetectRange = 40f;
    public Transform visionPoint;
    public LayerMask detectableLayers; // En Unity, se puede ignorar esto y usar ~0 para colisionar con todo

    [Header("Raycasts Configuration (Cone)")]
    [Tooltip("Apertura total del cono en grados.")]
    public float angleDifference = 120f;

    [Header("Optimization")]
    public float scanFrequency = 5f;
    private float nextScanTime = 0f;

    [Header("Close Range Awareness")]
    [Tooltip("Distancia dentro de la cual se detecta a alguien aunque esté fuera del cono de visión (p. ej., mientras el soldado combate a otro objetivo mirando en otra dirección).")]
    public float closeRangeAlwaysNoticeDistance = 4f;
    
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

            // Vectores aplanados
            Vector3 flatForward = visionPoint.forward;
            flatForward.y = 0;
            flatForward.Normalize();

            Vector3 flatDirectionToTarget = directionToTarget;
            flatDirectionToTarget.y = 0;
            flatDirectionToTarget.Normalize();

            float angleToTarget = Vector3.Angle(flatForward, flatDirectionToTarget);
            float distanceToTarget = Vector3.Distance(visionPoint.position, targetPosition);

            // Fuera del cono de visión se ignora, salvo que esté lo bastante cerca como para
            // notarse de todas formas (p. ej. alguien pasando al lado mientras se combate a otro objetivo).
            bool withinCone = angleToTarget <= angleDifference / 2f;
            bool closeEnoughRegardless = distanceToTarget <= closeRangeAlwaysNoticeDistance;

            if (withinCone || closeEnoughRegardless)
            {
                // Raycast para comprobar paredes (recorre todos los impactos, no solo el primero,
                // para no bloquearse cuando un aliado se cruza en el camino antes del objetivo real)
                if (IsLineClear(visionPoint.position, directionToTarget, distanceToTarget, col.transform.root))
                {
                    if (otherFaction.myFaction == FactionType.Player)
                    {
                        playerTarget = col.transform;
                    }
                    else
                    {
                        // Si es de otra facción, se calcula el más cercano
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

        return IsLineClear(visionPoint.position, directionToTarget, distanceToTarget, target.root);
    }

    /// <summary>
    /// Comprueba si hay línea de visión clara hacia un objetivo, recorriendo TODOS los impactos
    /// del rayo (no solo el primero). Con un único Raycast, cualquier aliado, cadáver u otro
    /// personaje que se cruce en el camino antes de llegar al objetivo hace fallar la detección
    /// aunque el objetivo esté a la vista; por eso se comprueba el orden real de los impactos.
    /// </summary>
    private bool IsLineClear(Vector3 origin, Vector3 direction, float distance, Transform targetRoot)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance + 0.5f, detectableLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.root == transform.root) continue; // Ignora al propio emisor del rayo

            // El primer impacto (que no sea el propio emisor) debe ser el objetivo; si es otra cosa, bloquea
            return hit.transform.root == targetRoot;
        }

        // El rayo no golpeó nada antes del objetivo (p.ej. su collider es un trigger)
        return true;
    }
}