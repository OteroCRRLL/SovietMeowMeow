using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifeTime = 3f;
    public float damage = 20f;
    private FactionIdentity shooterFaction;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
    
    public void SetShooterFaction(FactionIdentity faction)
    {
        shooterFaction = faction;
    }

    private void OnCollisionEnter(Collision collision)
    {
        FactionIdentity hitFaction = collision.gameObject.GetComponentInParent<FactionIdentity>();
        
        // No hacernos daño a nosotros mismos ni a nuestra facción
        if (hitFaction != null && shooterFaction != null && !shooterFaction.IsEnemy(hitFaction.myFaction))
        {
            Destroy(gameObject);
            return;
        }

        HealthSystem health = collision.gameObject.GetComponent<HealthSystem>();
        if (health == null)
        {
            health = collision.gameObject.GetComponentInParent<HealthSystem>();
        }

        if (health != null)
        {
            health.TakeDamage(damage);
        }
        
        // Efecto de impacto podría ir aquí
        Destroy(gameObject);
    }
}