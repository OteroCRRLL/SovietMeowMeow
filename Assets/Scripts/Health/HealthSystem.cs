using UnityEngine;
using UnityEngine.Events; // Esto es la magia para que sea modular

public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Eventos al Morir")]
    [Tooltip("Añade aquí lo que pasará al morir (Ej: GameLoopManager -> ExtractPlayer)")]
    public UnityEvent onDeath;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log(gameObject.name + " recibió daño. Vida restante: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " ha muerto.");
        onDeath.Invoke(); // Ejecuta todo lo que le pongas en el Inspector
    }
}