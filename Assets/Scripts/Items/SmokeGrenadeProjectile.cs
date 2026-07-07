using UnityEngine;

public class SmokeGrenadeProjectile : MonoBehaviour
{
    public float delayBeforeExplosion = 2f;
    public float smokeDuration = 10f;
    public float smokeRadius = 8f;
    
    public AudioClip smokeSound;
    public GameObject smokeVisualEffect;
    
    private bool hasExploded = false;

    private void Start()
    {
        Invoke(nameof(Explode), delayBeforeExplosion);
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (smokeSound != null)
        {
            AudioSource.PlayClipAtPoint(smokeSound, transform.position);
        }

        if (smokeVisualEffect != null)
        {
            GameObject vfx = Instantiate(smokeVisualEffect, transform.position, Quaternion.identity);
            Destroy(vfx, smokeDuration);
        }

        // Create the smoke collider
        GameObject smokeColliderObj = new GameObject("SmokeCloudCollider");
        smokeColliderObj.transform.position = transform.position;
        smokeColliderObj.layer = LayerMask.NameToLayer("Smoke"); // Ensure this layer exists in Unity
        
        SphereCollider col = smokeColliderObj.AddComponent<SphereCollider>();
        col.radius = smokeRadius;
        col.isTrigger = false; // Solid collider; the physics matrix is configured separately so it doesn't block movement

        Destroy(smokeColliderObj, smokeDuration);

        // Audio was triggered via PlayClipAtPoint, so it keeps playing independently of this object being destroyed
        Destroy(gameObject);
    }
}
