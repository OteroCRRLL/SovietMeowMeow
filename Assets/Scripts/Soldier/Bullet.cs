using UnityEngine;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float lifeTime = 3f;
    public float damage = 20f;
    
    [Header("Filter")]
    public List<string> targetTags = new List<string>();

    private FactionIdentity shooterFaction;

    void Start()
    {
        Destroy(gameObject, lifeTime); // Destruir bala si no choca con nada en X segundos
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = 0.0001f; // Evitar que la bala empuje objetos con físicas al colisionar
            rb.useGravity = false; // Hacer que la bala vaya recta y no caiga al suelo
        }
    }
    
    public void SetShooterFaction(FactionIdentity faction)
    {
        shooterFaction = faction;
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject hitObj = collision.gameObject;
        
        // Comprobar facciones para evitar fuego amigo
        FactionIdentity hitFaction = hitObj.GetComponentInParent<FactionIdentity>();
        if (hitFaction != null && shooterFaction != null && !shooterFaction.IsEnemy(hitFaction.myFaction))
        {
            Destroy(gameObject);
            return;
        }

        // Buscar el sistema de vida (HealthSystem)
        HealthSystem health = hitObj.GetComponent<HealthSystem>();
        if (health == null)
        {
            health = hitObj.GetComponentInParent<HealthSystem>();
        }

        if (health != null)
        {
            // Si usamos target tags (como el TankBullet), comprobamos. Si la lista está vacía, dañamos a cualquier enemigo por defecto.
            if (targetTags.Count == 0 || targetTags.Contains(hitObj.tag) || hitObj.CompareTag("Player") || hitFaction != null)
            {
                health.TakeDamage(damage);
                
                // Si la bala impacta a alguien y nosotros tenemos facción, le avisamos de quién le disparó (si es que está vivo y es un soldado o dron)
                if (shooterFaction != null)
                {
                    SoldierBrain hitBrain = hitObj.GetComponentInParent<SoldierBrain>();
                    if (hitBrain != null)
                    {
                        hitBrain.ReceiveAlert(shooterFaction.transform);
                        if (hitBrain.squadManager != null)
                        {
                            hitBrain.squadManager.AlertSquad(shooterFaction.transform);
                        }
                    }

                    DroneBrain hitDrone = hitObj.GetComponentInParent<DroneBrain>();
                    if (hitDrone != null)
                    {
                        hitDrone.ReceiveAlert(shooterFaction.transform);
                    }
                }
            }
        }
        
        // Destruir la bala al chocar contra lo que sea (suelo, paredes, enemigos)
        Destroy(gameObject);
    }
}