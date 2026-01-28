using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankController : MonoBehaviour
{
    [Header("Object References")]
    public Transform tankCabin;
    public Transform shootPoint;
    public GameObject bulletPrefab;
    public Animator anim;

    [Header("Settings")]
    public float rotationSpeed = 5f;
    public float bulletForce = 1500f; 

    public void RotateTowards(Transform target)
    {
        if (target == null || tankCabin == null) return;

        //1. Calculate direction to target
        Vector3 direction = (target.position - tankCabin.position).normalized;

        //Block y.axis
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            //Create rotation
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            lookRotation *= Quaternion.Euler(0, -90, 0);
            //Smooth rotation
            tankCabin.rotation = Quaternion.Slerp(tankCabin.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
        

    }

    public void SetPatrolAnimation(bool active)
    {
        if (anim != null)
        {
            anim.enabled = active;
            

        }
    }

    public void Fire(Transform target)
    {
        //Spawn bullet on firepoint
        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

        //Shoot bullet
        if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Vector3 direction;

            if (target != null)
            {
 
                direction = (target.position - shootPoint.position).normalized;

            }

            else
            {
                direction = shootPoint.forward;
            }

            rb.AddForce(direction * bulletForce);
        }

        Debug.Log("Tank fired");
    }

}
