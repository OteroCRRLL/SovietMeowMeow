using UnityEngine;

public class DecoyProjectile : MonoBehaviour
{
    public float attractionRadius = 30f;
    public float lifetime = 10f;
    public AudioClip decoySound;
    public GameObject distractionEffect;

    private void Start()
    {
        // Emit sound
        if (decoySound != null)
        {
            AudioSource.PlayClipAtPoint(decoySound, transform.position);
        }

        if (distractionEffect != null)
        {
            Instantiate(distractionEffect, transform.position, Quaternion.identity);
        }

        // Alert enemies
        Collider[] colliders = Physics.OverlapSphere(transform.position, attractionRadius);
        foreach (Collider col in colliders)
        {
            SoldierBrain soldier = col.GetComponentInParent<SoldierBrain>();
            if (soldier != null)
            {
                // Force patrol destination to the decoy, or alert them
                soldier.ForceOverrideDestination(transform.position);
                continue;
            }

            DroneBrain drone = col.GetComponentInParent<DroneBrain>();
            if (drone != null)
            {
                drone.ReceiveAlert(transform);
                continue;
            }
        }

        Destroy(gameObject, lifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attractionRadius);
    }
}
