using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestiona la actualización visual de la barra de vida del jugador.
/// Debe ir colocado en un Canvas local o hijo del Prefab del Jugador.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("Configuración UI")]
    [Tooltip("La imagen de la barra de vida. Debe tener el componente Image tipo 'Filled'.")]
    public Image healthBarFill;

    /// <summary>
    /// Función para enlazar al evento OnHealthChanged(float) del HealthSystem.
    /// Recibe el porcentaje de vida entre 0 y 1.
    /// </summary>
    public void UpdateHealthBar(float healthNormalized)
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = healthNormalized;
        }
        else
        {
            Debug.LogWarning("PlayerHealthUI: Falta asignar la imagen de la barra de vida (healthBarFill).");
        }
    }
}
