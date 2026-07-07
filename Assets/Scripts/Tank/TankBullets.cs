using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankBullets : MonoBehaviour
{

    [Header("Explostion Settings")]
    public float explosionRadius = 7f; // Rango de explosión aumentado
    public float lifeTime = 5f; // Changed from 1000f to avoid memory leaks

    public float explosionDamage = 50f; 

    [Header("Audio")]
    public AudioClip explosionImpactClip;

    [Header("Visual Effects")]
    public GameObject explosionPrefab;

    [Header("Camera Shake Settings")]
    public float shakeRadius = 25f;
    public float shakeDuration = 0.5f;
    public float shakeIntensity = 0.3f;

    [Header("Filter")]
    public List<string> collisionTags = new List<string>();
    public List<string> targetTags = new List<string>(); // Lista mantenida por si hace falta explotar barriles u otras cosas sin facción
    
    private FactionIdentity shooterFaction;

    void Start()
    {
        Destroy(gameObject, lifeTime); //Destroy if does not collide
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = 0.0001f; // Evitar empujar objetos al colisionar
            rb.useGravity = false; // Hacer que la bala vaya recta y no caiga al suelo
        }
    }

    public void SetShooterFaction(FactionIdentity faction)
    {
        shooterFaction = faction;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Explotar contra cualquier cosa (suelo, paredes, jugador, otros enemigos)
        Explode();
        Debug.Log("Bullet Collision with " + collision.gameObject.name);
    }

    private void Explode()
    {
        //Detect all colliders in area
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hitColliders)
        {
            FactionIdentity hitFaction = hit.GetComponentInParent<FactionIdentity>();
            
            // Ignora a los aliados (si el impacto es contra la propia facción, el misil no hace daño en área)
            if (hitFaction != null && shooterFaction != null && !shooterFaction.IsEnemy(hitFaction.myFaction))
            {
                continue;
            }

            // Verificar si es un objetivo válido (Enemigo, Player, o un TargetTag manual como "Barril")
            bool isValidTarget = (hitFaction != null && shooterFaction != null && shooterFaction.IsEnemy(hitFaction.myFaction)) 
                                || hit.CompareTag("Player") 
                                || targetTags.Contains(hit.tag);

            if (isValidTarget)
            {
                // Busca el sistema de vida tanto en el objeto como en sus padres (por si el collider está en un hijo)
                HealthSystem health = hit.GetComponent<HealthSystem>();
                if (health == null)
                {
                    health = hit.GetComponentInParent<HealthSystem>();
                }

                if (health != null)
                {
                    health.TakeDamage(explosionDamage);
                    Debug.Log("Daño por explosión a: " + hit.name);
                    
                    if (shooterFaction != null)
                    {
                        SoldierBrain hitBrain = hit.GetComponentInParent<SoldierBrain>();
                        if (hitBrain != null)
                        {
                            hitBrain.ReceiveAlert(shooterFaction.transform);
                            if (hitBrain.squadManager != null)
                            {
                                hitBrain.squadManager.AlertSquad(shooterFaction.transform);
                            }
                        }
                        
                        DroneBrain droneBrain = hit.GetComponentInParent<DroneBrain>();
                        if (droneBrain != null)
                        {
                            droneBrain.ReceiveAlert(shooterFaction.transform);
                        }
                    }
                }
            }
        }
        
        if (explosionImpactClip != null)
        {
            AudioSource.PlayClipAtPoint(explosionImpactClip, transform.position);
        }

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        if (CameraController.instance != null)
        {
            float distToCamera = Vector3.Distance(transform.position, CameraController.instance.transform.position);
            if (distToCamera <= shakeRadius)
            {
                CameraController.instance.ShakeCamera(shakeDuration, shakeIntensity);
            }
        }

        Destroy(gameObject); // Destruye la bala
    }

}