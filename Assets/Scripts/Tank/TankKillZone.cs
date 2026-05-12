using UnityEngine;

public class TankKillZone : MonoBehaviour
{
    private FactionIdentity myFaction;

    private void Start()
    {
        // Al empezar, busca la facción en este objeto o en los padres (el chasis del tanque)
        myFaction = GetComponentInParent<FactionIdentity>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (myFaction == null) return;

        // Obtenemos la facción de lo que hemos tocado
        FactionIdentity otherFaction = other.GetComponent<FactionIdentity>();
        if (otherFaction == null)
        {
            otherFaction = other.GetComponentInParent<FactionIdentity>();
        }

        // Si el objeto tiene facción y es enemigo nuestro
        if (otherFaction != null && myFaction.IsEnemy(otherFaction.myFaction))
        {
            HealthSystem health = other.GetComponent<HealthSystem>();
            if (health == null)
            {
                health = other.GetComponentInParent<HealthSystem>();
            }

            // Si tiene vida y no está muerto, lo atropellamos
            if (health != null && !health.IsDead)
            {
                Debug.Log($"¡El tanque ha atropellado a {other.gameObject.name}! Muerte instantánea.");
                health.TakeDamage(9999f); // Daño letal masivo
            }
        }
    }
}