using UnityEngine;

public class AdrenalineItem : UsableItem
{
    [Header("Adrenaline Config")]
    public float infiniteStaminaDuration = 7f;

    protected override void UseItem()
    {
        PlayerController playerController = GetComponentInParent<PlayerController>();

        if (playerController == null)
        {
            // Fallback: search globally if not found in parent
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
            }
        }

        if (playerController != null)
        {
            playerController.ApplyInfiniteStamina(infiniteStaminaDuration);
            Debug.Log("Adrenalina inyectada. Stamina infinita por " + infiniteStaminaDuration + " segundos.");

            ConsumeItem(); // Desaparece del inventario
        }
        else
        {
            Debug.LogWarning("No se encontró PlayerController en el jugador para usar la adrenalina.");
        }
    }
}
