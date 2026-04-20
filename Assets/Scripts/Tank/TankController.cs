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

            // --- Añadir dispersión (Spread) ---
            float randomX = Random.Range(-spreadAngle, spreadAngle);
            float randomY = Random.Range(-spreadAngle, spreadAngle);
            float randomZ = Random.Range(-spreadAngle, spreadAngle);

            Quaternion spreadRotation = Quaternion.Euler(randomX, randomY, randomZ);
            Vector3 finalDirection = spreadRotation * direction;

            rb.AddForce(finalDirection * bulletForce);
        }

        Debug.Log("Tank fired");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Si atropellamos al jugador, muere instantáneamente
        if (collision.gameObject.CompareTag("Player"))
        {
            HealthSystem health = collision.gameObject.GetComponent<HealthSystem>();
            if (health == null)
            {
                health = collision.gameObject.GetComponentInParent<HealthSystem>();
            }

            if (health != null)
            {
                Debug.Log("¡El tanque ha atropellado al jugador! Muerte instantánea.");
                health.TakeDamage(9999f); // Daño letal masivo
            }
        }
    }
}
