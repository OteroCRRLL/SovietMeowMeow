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
        col.isTrigger = false; // Solid collider, but we will configure physics matrix so it doesn't block movement

        Destroy(smokeColliderObj, smokeDuration);

        // Hide the grenade object but don't destroy immediately if there's audio playing from it directly (we used PlayClipAtPoint so it's fine)
        Destroy(gameObject);
    }
}
