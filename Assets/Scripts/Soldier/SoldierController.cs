using UnityEngine;

public class SoldierController : MonoBehaviour
{
    [Header("Object References")]
    public Transform body;
    public Transform shootPoint;
    public GameObject bulletPrefab;
    public Animator anim;

    [Header("Settings")]
    public float rotationSpeed = 8f;
    public float bulletForce = 4000f; 
    public float spreadAngle = 1.5f;

    public void RotateTowards(Transform target)
    {
        if (target == null || body == null) return;

        Vector3 direction = (target.position - body.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            body.rotation = Quaternion.Slerp(body.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    public void SetAnimation(string stateName)
    {
        if (anim != null)
        {
            // Aquí puedes configurar tu Animator en Unity
            // Ejemplo: anim.SetBool("isRunning", stateName == "Run");
            // De momento lo dejamos preparado para cuando integres los modelos
        }
    }

    public void Fire(Transform target)
    {
        if (bulletPrefab == null || shootPoint == null) return;

        GameObject bulletObj = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

        // Asignar facción a la bala
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.SetShooterFaction(GetComponentInParent<FactionIdentity>());
        }

        if (bulletObj.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Vector3 direction = shootPoint.forward;
            if (target != null)
            {
                // Apuntar al centro del objetivo en lugar de a sus pies
                Collider targetCollider = target.GetComponentInChildren<Collider>();
                Vector3 targetCenter = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up * 1f;
                direction = (targetCenter - shootPoint.position).normalized;
            }

            float randomX = Random.Range(-spreadAngle, spreadAngle);
            float randomY = Random.Range(-spreadAngle, spreadAngle);
            float randomZ = Random.Range(-spreadAngle, spreadAngle);

            Quaternion spreadRotation = Quaternion.Euler(randomX, randomY, randomZ);
            Vector3 finalDirection = spreadRotation * direction;

            rb.AddForce(finalDirection * bulletForce);
        }
    }
}