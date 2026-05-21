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

    [Header("Audio")]
    public AudioSource shootAudioSource;
    public AudioClip tankShootClip;

    [Header("Settings")]
    public float rotationSpeed = 5f;
    public float bulletForce = 4500f; 
    [Tooltip("Grados máximos de desviación aleatoria al disparar (Spread).")]
    public float spreadAngle = 3f;

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
        
        TankBullets bulletScript = bullet.GetComponent<TankBullets>();
        if (bulletScript != null)
        {
            bulletScript.SetShooterFaction(GetComponentInParent<FactionIdentity>());
        }

        //Shoot bullet
        if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Vector3 direction;

            if (target != null)
            {
                // Apuntar al centro del objetivo en lugar de a sus pies
                Collider targetCollider = target.GetComponentInChildren<Collider>();
                Vector3 targetCenter = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up * 1f;
                direction = (targetCenter - shootPoint.position).normalized;
            }
            else
            {
                direction = shootPoint.forward;
            }

            // --- Añadir dispersión (Spread) ---
            float randomX = Random.Range(-spreadAngle, spreadAngle);
            float randomY = Random.Range(-spreadAngle, spreadAngle);
            float randomZ = Random.Range(-spreadAngle, spreadAngle);

            Quaternion spreadRotation = Quaternion.Euler(randomX, randomY, randomZ);
            Vector3 finalDirection = spreadRotation * direction;

            rb.AddForce(finalDirection * bulletForce);
        }

        if (shootAudioSource != null && tankShootClip != null)
        {
            shootAudioSource.PlayOneShot(tankShootClip);
        }
        else if (tankShootClip != null)
        {
            AudioSource.PlayClipAtPoint(tankShootClip, shootPoint.position);
        }

        Debug.Log("Tank fired");
    }


}
