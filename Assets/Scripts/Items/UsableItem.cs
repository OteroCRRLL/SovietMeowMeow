using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public abstract class UsableItem : MonoBehaviour
{
    [Header("Item Config")]
    public InputAction useAction;
    public float holdTimeRequired = 0.7f;
    
    protected float currentHoldTime = 0f;
    protected bool isHolding = false;
    protected bool hasBeenUsed = false;

    protected virtual void OnEnable()
    {
        useAction.Enable();
        hasBeenUsed = false;
    }

    protected virtual void OnDisable()
    {
        useAction.Disable();
        isHolding = false;
        currentHoldTime = 0f;
        hasBeenUsed = false;
    }

    protected virtual void Update()
    {
        // No permitir el uso de objetos en la escena Hub
        if (SceneManager.GetActiveScene().name == "Hub") return;

        if (hasBeenUsed) return;

        if (useAction.WasPressedThisFrame() || useAction.IsPressed())
        {
            isHolding = true;
            currentHoldTime += Time.deltaTime;

            if (currentHoldTime >= holdTimeRequired)
            {
                currentHoldTime = 0f;
                isHolding = false;
                hasBeenUsed = true;
                UseItem();
            }
        }
        else
        {
            if (isHolding)
            {
                // Solto el boton antes de tiempo
                isHolding = false;
                currentHoldTime = 0f;
                OnUseCancelled();
            }
        }
    }

    protected abstract void UseItem();
    
    protected virtual void OnUseCancelled() 
    {
        // Opcional: Se puede sobrescribir para cancelar sonidos o animaciones
    }
    
    protected void ConsumeItem()
    {
        PlayerEquipment equipment = FindObjectOfType<PlayerEquipment>();
        if (equipment != null)
        {
            equipment.RemoveCurrentItem();
        }
    }

    // Función útil para objetos reutilizables (con cooldown)
    protected void ResetUsage()
    {
        hasBeenUsed = false;
        isHolding = false;
        currentHoldTime = 0f;
    }
}
