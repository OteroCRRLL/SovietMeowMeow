using UnityEngine;

public class DecoyItem : UsableItem
{
    [Header("Decoy Settings")]
    public GameObject decoyPrefab;
    public Transform throwPoint;
    public float throwForce = 15f;
    
    protected override void UseItem()
    {
        if (decoyPrefab != null && throwPoint != null)
        {
            GameObject decoy = Instantiate(decoyPrefab, throwPoint.position, throwPoint.rotation);
            Rigidbody rb = decoy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(throwPoint.forward * throwForce, ForceMode.VelocityChange);
            }
            
            ConsumeItem();
        }
    }
}
