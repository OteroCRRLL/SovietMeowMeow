using UnityEngine;

public class MedkitItem : UsableItem
{
    [Header("Medkit Config")]
    public float healAmount = 50f;

    protected override void UseItem()
    {
        HealthSystem playerHealth = GetComponentInParent<HealthSystem>();
        
        if (playerHealth == null)
        {
            // Fallback: search globally if not found in parent
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<HealthSystem>();
            }
        }

        if (playerHealth != null)
        {
            // Solo curar si le falta vida
            if (playerHealth.CurrentHealth < playerHealth.maxHealth)
            {
                playerHealth.Heal(healAmount);
                Debug.Log("Botiquín usado. Vida curada: " + healAmount);
                
                // Aquí podrías reproducir un sonido
                
                ConsumeItem(); // Desaparece del inventario
            }
            else
            {
                Debug.Log("La vida ya está al máximo. No se gasta el botiquín.");
            }
        }
        else
        {
            Debug.LogWarning("No se encontró HealthSystem en el jugador para usar el botiquín.");
        }
    }
}
