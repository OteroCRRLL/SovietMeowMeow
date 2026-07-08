using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public abstract class UsableItem : MonoBehaviour
{
    [Header("Item Config")]
    public InputAction useAction;

    protected bool hasBeenUsed = false;

    protected virtual void OnEnable()
    {
        useAction.Enable();
        hasBeenUsed = false;
    }

    protected virtual void OnDisable()
    {
        useAction.Disable();
        hasBeenUsed = false;
    }

    protected virtual void Update()
    {
        // No permitir el uso de objetos en la escena Hub
        if (SceneManager.GetActiveScene().name == "Hub") return;

        if (hasBeenUsed) return;

        if (useAction.WasPressedThisFrame())
        {
            hasBeenUsed = true;
            UseItem();
        }
    }

    protected abstract void UseItem();

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
    }
}
