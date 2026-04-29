using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankSensor : MonoBehaviour
{
    [Header("Detection settings")]
    public float DetectRange = 15f;
    public Transform visionPoint;
    public LayerMask detectableLayers;

    [Header("Raycasts Configuration (Cone)")]
    [Tooltip("Apertura total del cono en grados.")]
    public float angleDifference = 110f;

    [Header("Tag filter")]
    public List<string> targetTags = new List<string>();

    [Header("Optimization")]
    [Tooltip("Veces por segundo que el sensor escanea el área (reduce el lag drásticamente).")]
    public float scanFrequency = 5f;

    private float nextScanTime = 0f;
    
    // Usamos un array pre-asignado para OverlapSphereNonAlloc (cero Garbage Collection)
    private Collider[] collidersBuffer = new Collider[20]; 
    private FactionIdentity myFaction;

    private void Start()
    {
        myFaction = GetComponentInParent<FactionIdentity>();
    }

    public Transform GetDetectedEnemy()
    {
        if (visionPoint == null || myFaction == null) return null;

        // Limitar la frecuencia de escaneo para ahorrar muchísimo rendimiento
        if (Time.time < nextScanTime)
        {
            return null; // Aún no toca escanear
        }

        nextScanTime = Time.time + (1f / scanFrequency);

        // 1. Detección espacial rápida (Esfera invisible, sin coste de raycasts múltiples)
        int numColliders = Physics.OverlapSphereNonAlloc(visionPoint.position, DetectRange, collidersBuffer, detectableLayers);

        for (int i = 0; i < numColliders; i++)
        {
            Collider col = collidersBuffer[i];
            
            // Usar Facciones en lugar de Tags
            FactionIdentity otherFaction = col.GetComponentInParent<FactionIdentity>();
            if (otherFaction == null || !myFaction.IsEnemy(otherFaction.myFaction)) continue;

            // Ignorar a los muertos
            HealthSystem targetHealth = col.GetComponentInParent<HealthSystem>();
            if (targetHealth != null && targetHealth.IsDead) continue;

            // Apuntar al centro del collider en lugar de a los pies (transform.position)
            Vector3 targetPosition = col.bounds.center;
            Vector3 directionToTarget = (targetPosition - visionPoint.position).normalized;

            // Aplanamos los vectores para que la diferencia de altura (jugador más bajito o muy cerca) no nos saque del cono de visión
            Vector3 flatForward = visionPoint.forward;
            flatForward.y = 0;
            flatForward.Normalize();

            Vector3 flatDirectionToTarget = directionToTarget;
            flatDirectionToTarget.y = 0;
            flatDirectionToTarget.Normalize();

            // 2. Comprobar matemáticamente si está dentro del cono de visión del tanque (solo horizontal)
            float angleToTarget = Vector3.Angle(flatForward, flatDirectionToTarget);
            
            if (angleToTarget <= angleDifference / 2f)
            {
                // 3. Lanzar Raycast hacia el objetivo para ver si hay paredes bloqueando
                float distanceToTarget = Vector3.Distance(visionPoint.position, targetPosition);
                
                RaycastHit[] hits = Physics.RaycastAll(visionPoint.position, directionToTarget, distanceToTarget + 0.5f, detectableLayers, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                
                bool hitWall = false;
                foreach (RaycastHit hit in hits)
                {
                    // Ignorar a nosotros mismos
                    if (hit.collider.transform.root == transform.root) continue;
                    
                    FactionIdentity hitFaction = hit.collider.GetComponentInParent<FactionIdentity>();
                    if (hit.collider.transform.root == col.transform.root || (hitFaction != null && myFaction.IsEnemy(hitFaction.myFaction)))
                    {
                        Debug.DrawRay(visionPoint.position, directionToTarget * distanceToTarget, Color.green, 1f / scanFrequency);
                        return col.transform; // ¡Enemigo detectado!
                    }
                    else
                    {
                        // Si chocamos con algo que no somos nosotros ni el objetivo/enemigo, asumimos que es un muro
                        hitWall = true;
                        break; 
                    }
                }
                
                // Si el raycast no chocó con ningún muro, consideramos que lo vemos (útil si el raycast pasa el collider por poco)
                if (!hitWall)
                {
                    Debug.DrawRay(visionPoint.position, directionToTarget * distanceToTarget, Color.green, 1f / scanFrequency);
                    return col.transform;
                }
            }
        }

        return null;
    }

    public bool IsTargetVisible(Transform target)
    {
        if (target == null || visionPoint == null) return false;

        Collider targetCollider = target.GetComponentInChildren<Collider>();
        Vector3 targetPosition = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up * 0.5f;

        float distanceToTarget = Vector3.Distance(visionPoint.position, targetPosition);
        
        // Si se ha alejado demasiado, ya no lo vemos
        if (distanceToTarget > DetectRange) return false;

        Vector3 directionToTarget = (targetPosition - visionPoint.position).normalized;

        // Lanzamos un raycast directo al objetivo para mantener el "lock"
        RaycastHit[] hits = Physics.RaycastAll(visionPoint.position, directionToTarget, DetectRange, detectableLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            // Ignorar el propio tanque
            if (hit.collider.transform.root == transform.root) continue;

            FactionIdentity hitFaction = hit.collider.GetComponentInParent<FactionIdentity>();
            if (hit.transform.root == target.root || (hitFaction != null && myFaction != null && myFaction.IsEnemy(hitFaction.myFaction)))
            {
                return true;
            }
            
            // Chocó contra un muro u otro obstáculo
            return false;
        }
        
        return false;
    }

    // Método extra para ver el rango de visión en el editor fácilmente
    private void OnDrawGizmosSelected()
    {
        if (visionPoint != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f); // Esfera amarilla semi-transparente
            Gizmos.DrawWireSphere(visionPoint.position, DetectRange);
        }
    }
}
