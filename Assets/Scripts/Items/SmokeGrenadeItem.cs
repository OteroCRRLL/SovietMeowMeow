using UnityEngine;

public class SmokeGrenadeItem : UsableItem
{
    [Header("Smoke Grenade Settings")]
    public GameObject smokeGrenadePrefab;
    public Transform throwPoint;
    public float throwForce = 15f;
    
    protected override void UseItem()
    {
        if (smokeGrenadePrefab != null && throwPoint != null)
        {
            GameObject smoke = Instantiate(smokeGrenadePrefab, throwPoint.position, throwPoint.rotation);
            Rigidbody rb = smoke.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(throwPoint.forward * throwForce, ForceMode.VelocityChange);
            }
            
            ConsumeItem();
        }
    }
}
