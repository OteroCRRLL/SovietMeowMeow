using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankBullets : MonoBehaviour
{

    [Header("Explostion Settings")]
    public float explosionRadius = 3f;
    public float lifeTime = 1000f;

    public float explosionDamage = 50f; 

    //public GameObject explosionEffect;

    [Header("Filter")]
    public List<string> collisionTags = new List<string>();
    public List<string> targetTags = new List<string>();

    void Start()
    {
        Destroy(gameObject, lifeTime); //Destroy if does not collide
    }

    private void OnCollisionEnter(Collision collision)
    {

        
        if (collisionTags.Contains(collision.gameObject.tag))
        {
            Explode();
            Debug.Log("Bullet Collision");
        }
        
    }

    private void Explode()
    {
        //Detect all colliders in area
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hitColliders)
        {
            //Verify if object is desired tag
            if (targetTags.Contains(hit.tag))
            {
                // Si el objeto tiene sistema de vida, le hace daño
                if (hit.TryGetComponent<HealthSystem>(out HealthSystem health))
                {
                    health.TakeDamage(explosionDamage);
                    Debug.Log("Daño por explosión a: " + hit.name);
                }
            }
        }
        Destroy(gameObject); // Destruye la bala
    }

}
