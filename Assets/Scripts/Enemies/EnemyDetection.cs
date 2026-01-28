using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    public float DetectRange = 10f;
    public Transform visionPoint;
    public LayerMask detectableLayers;

    [Header("Raycasts Configuration")]
    public int numberOfRays = 5;
    public float angleDifference = 10f;

    [Header("Tag filter")]
    public List<string> targetTags = new List<string>();


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        DetectEnemy();

    }

    

    private void DetectEnemy()
    {
        float initialAngle = -angleDifference / 2f; //Half the angle downwards
        float spaceBetweenRays = angleDifference / (numberOfRays - 1);

        for (int i = 0; i < numberOfRays; i++)
        {
            float actualAngle = initialAngle + (spaceBetweenRays * i);

            Quaternion rayRotation = Quaternion.Euler(actualAngle, 0, 0);
            Vector3 rayDirection = visionPoint.rotation * rayRotation * Vector3.forward;

            RaycastHit hit;
            if (Physics.Raycast(visionPoint.position, rayDirection, out hit, DetectRange, detectableLayers))
            {
                if (targetTags.Contains(hit.collider.tag))
                {
                    Debug.DrawRay(visionPoint.position, rayDirection * DetectRange, Color.green);
                    Debug.Log("Enemy detected" + hit.collider.gameObject.name);
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
}
