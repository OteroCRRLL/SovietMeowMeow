using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Eventos al Recibir Daño")]
    public UnityEvent<float> onHealthChanged; // Pasamos el porcentaje de vida (0 a 1)

    [Header("Eventos al Morir")]
    public UnityEvent onDeath;

    void Start()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        
        // Evitamos que la vida baje de 0
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log(gameObject.name + " recibi dao. Vida restante: " + currentHealth);

        // Disparamos el evento de cambio de vida con el valor normalizado (0 a 1)
        onHealthChanged?.Invoke(currentHealth / maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " ha muerto.");
        onDeath.Invoke();
        
        if (gameObject.CompareTag("Player") && DeathScreenManager.instance != null)
        {
            DeathScreenManager.instance.ShowDeathScreen();
        }
    }
}