using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Eventos al Morir")]
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
        onDeath.Invoke();
    }
}