using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankBullets : MonoBehaviour
{

    [Header("Explostion Settings")]
    public float explosionRadius = 3f;
    public float lifeTime = 5f; // Changed from 1000f to avoid memory leaks

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
        // Explotar si colisiona con algo que está en la lista de collisionTags, o si colisiona directamente con el Player
        if (collisionTags.Contains(collision.gameObject.tag) || collision.gameObject.CompareTag("Player"))
        {
            Explode();
            Debug.Log("Bullet Collision with " + collision.gameObject.name);
        }
    }

    private void Explode()
    {
        //Detect all colliders in area
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hitColliders)
        {
            //Verify if object is desired tag, or if it's the Player
            if (targetTags.Contains(hit.tag) || hit.CompareTag("Player"))
            {
                // Buscamos el sistema de vida tanto en el objeto como en sus padres (por si el collider está en un hijo)
                HealthSystem health = hit.GetComponent<HealthSystem>();
                if (health == null)
                {
                    health = hit.GetComponentInParent<HealthSystem>();
                }

                if (health != null)
                {
                    health.TakeDamage(explosionDamage);
                    Debug.Log("Daño por explosión a: " + hit.name);
                }
            }
        }
        Destroy(gameObject); // Destruye la bala
    }

}