using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankSensor : MonoBehaviour
{
    [Header("Detection settings")]
    public float DetectRange = 10f;
    public Transform visionPoint;
    public LayerMask detectableLayers;

    [Header("Raycasts Configuration (Cone)")]
    [Tooltip("Cantidad de rayos por eje. Si pones 5, lanzará 25 rayos en total (5x5).")]
    public int numberOfRays = 5;
    [Tooltip("Apertura total del cono en grados.")]
    public float angleDifference = 30f;

    [Header("Tag filter")]
    public List<string> targetTags = new List<string>();

    public Transform GetDetectedEnemy()
    {
        if (visionPoint == null) return null;

        // Calculamos el inicio del ángulo (mitad hacia un lado, mitad hacia el otro)
        float halfAngle = angleDifference / 2f;

        // Evitamos error de división por cero si numberOfRays es 1
        int steps = Mathf.Max(2, numberOfRays);
        float spaceBetweenRays = angleDifference / (steps - 1);

        // BUCLE VERTICAL (Eje X)
        for (int i = 0; i < steps; i++)
        {
            float verticalAngle = -halfAngle + (spaceBetweenRays * i);

            // BUCLE HORIZONTAL (Eje Y)
            for (int j = 0; j < steps; j++)
            {
                float horizontalAngle = -halfAngle + (spaceBetweenRays * j);

                // Combinamos las rotaciones para crear la dirección del cono
                Quaternion rayRotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);

                // Aplicamos la rotación del visionPoint para que el cono siga hacia donde mira el tanque
                Vector3 rayDirection = visionPoint.rotation * rayRotation * Vector3.forward;

                RaycastHit hit;
                if (Physics.Raycast(visionPoint.position, rayDirection, out hit, DetectRange, detectableLayers))
                {
                    if (targetTags.Contains(hit.collider.tag))
                    {
                        // Dibujamos el rayo hasta el punto de impacto
                        Debug.DrawRay(visionPoint.position, rayDirection * hit.distance, Color.green);
                        Debug.Log("Enemy detected: " + hit.collider.gameObject.name);

                        return hit.transform;
                    }
                    else
                    {
                        // Impacto en algo que no es el objetivo (muros, etc.)
                        Debug.DrawLine(visionPoint.position, hit.point, Color.yellow);
                    }
                }
                else
                {
                    // El rayo no golpea nada dentro del rango
                    Debug.DrawRay(visionPoint.position, rayDirection * DetectRange, Color.red);
                }
            }
        }

        return null;
    }
}