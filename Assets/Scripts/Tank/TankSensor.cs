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
    public int numberOfRays = 5;
    [Tooltip("Apertura total del cono en grados.")]
    public float angleDifference = 30f;

    [Header("Tag filter")]
    public List<string> targetTags = new List<string>();

    public Transform GetDetectedEnemy()
    {
        if (visionPoint == null) return null;

        
        float halfAngle = angleDifference / 2f;

        
        int steps = Mathf.Max(2, numberOfRays);
        float spaceBetweenRays = angleDifference / (steps - 1);

     
        for (int i = 0; i < steps; i++)
        {
            float verticalAngle = -halfAngle + (spaceBetweenRays * i);

           
            for (int j = 0; j < steps; j++)
            {
                float horizontalAngle = -halfAngle + (spaceBetweenRays * j);

             
                Quaternion rayRotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);

               
                Vector3 rayDirection = visionPoint.rotation * rayRotation * Vector3.forward;

                RaycastHit hit;
                if (Physics.Raycast(visionPoint.position, rayDirection, out hit, DetectRange, detectableLayers))
                {
                    if (targetTags.Contains(hit.collider.tag))
                    {
                       
                        Debug.DrawRay(visionPoint.position, rayDirection * hit.distance, Color.green);
                        Debug.Log("Enemy detected: " + hit.collider.gameObject.name);

                        return hit.transform;
                    }
                    else
                    {
                        
                        Debug.DrawLine(visionPoint.position, hit.point, Color.yellow);
                    }
                }
                else
                {
                 
                    Debug.DrawRay(visionPoint.position, rayDirection * DetectRange, Color.red);
                }
            }
        }

        return null;
    }
}