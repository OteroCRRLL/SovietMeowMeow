using UnityEngine;

public class EMPGrenadeProjectile : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float timeToExplode = 3.0f;
    public float explosionRadius = 15f;
    public float paralyzeDuration = 8f;
    public GameObject explosionEffectPrefab; // Opcional, para particulas azules

    private float timer = 0f;
    private bool hasExploded = false;

    private void Update()
    {
        if (hasExploded) return;

        timer += Time.deltaTime;
        if (timer >= timeToExplode)
        {
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;

        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hitColliders)
        {
            // Intentamos paralizar drones
            DroneBrain drone = hit.GetComponentInParent<DroneBrain>();
            if (drone != null)
            {
                drone.Paralyze(paralyzeDuration);
            }

            // Intentamos paralizar soldados
            SoldierBrain soldier = hit.GetComponentInParent<SoldierBrain>();
            if (soldier != null)
            {
                soldier.Paralyze(paralyzeDuration);
            }
        }

        // Podrías añadir un sonido de explosión aquí antes de destruir
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
