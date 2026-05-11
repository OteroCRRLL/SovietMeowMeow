using UnityEngine;

public class JammerItem : UsableItem
{
    [Header("Jammer Settings")]
    public float radius = 15f;
    public float paralyzeDuration = 7f;
    public float cooldown = 30f;

    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;

    protected override void Update()
    {
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                ResetUsage(); // Permite volver a usarlo
                Debug.Log("Jammer recargado y listo para usarse.");
            }
            
            // Opcional: Podrías hacer que el jugador no pueda pulsar el botón mientras recarga
            // o que suene un error si lo intenta.
        }
        else
        {
            // Ejecutar la lógica de mantener pulsado (holdTimeRequired)
            base.Update();
        }
    }

    protected override void UseItem()
    {
        Debug.Log("¡Jammer activado! Desactivando drones cercanos...");

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
        int dronesAffected = 0;

        foreach (Collider hit in hitColliders)
        {
            DroneBrain drone = hit.GetComponentInParent<DroneBrain>();
            if (drone != null)
            {
                drone.Paralyze(paralyzeDuration);
                dronesAffected++;
            }
        }
        
        Debug.Log($"Jammer afectó a {dronesAffected} drones.");

        // Iniciar el cooldown
        isOnCooldown = true;
        cooldownTimer = cooldown;
        
        // ¡IMPORTANTE! No llamamos a ConsumeItem() porque el Jammer es permanente
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
