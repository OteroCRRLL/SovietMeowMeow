using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    public bool IsDead => isDead;
    public float CurrentHealth => currentHealth;

    [Header("Eventos al Recibir Daño")]
    public UnityEvent<float> onHealthChanged; // Porcentaje de vida (0 a 1)

    [Header("Eventos al Morir")]
    public UnityEvent onDeath;

    void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        
        currentHealth -= amount;

        if (currentHealth < 0) currentHealth = 0;

        Debug.Log(gameObject.name + " recibi dao. Vida restante: " + currentHealth);

        onHealthChanged?.Invoke(currentHealth / maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;

        if (currentHealth > maxHealth) currentHealth = maxHealth;

        Debug.Log(gameObject.name + " se ha curado. Vida restante: " + currentHealth);

        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        
        Debug.Log(gameObject.name + " ha muerto.");
        onDeath.Invoke();
        
        if (gameObject.CompareTag("Player") && DeathScreenManager.instance != null)
        {
            DeathScreenManager.instance.ShowDeathScreen();
        }
    }
}